using System.Security.Cryptography;
using System.Text;
using GtaAutoGameplay.Core.Targeting;

namespace GtaAutoGameplay.Platform.Windows.Windowing;

public sealed class WindowsWindowDiscovery : IWindowDiscovery
{
    public static readonly TimeSpan DefaultCandidateLifetime = TimeSpan.FromSeconds(15);

    private readonly IWin32WindowApi _windowApi;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _candidateLifetime;
    private readonly int _currentProcessId;
    private readonly string _identitySalt;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly object _stateGate = new();
    private readonly Dictionary<CandidateId, CandidateMapping> _candidateMappings = [];
    private readonly Dictionary<SelectionId, NativeWindowReference> _selectionMappings = [];
    private WindowSelection? _currentSelection;

    public WindowsWindowDiscovery()
        : this(
            new Win32WindowApi(),
            TimeProvider.System,
            DefaultCandidateLifetime,
            Environment.ProcessId,
            Guid.NewGuid().ToString("N"))
    {
    }

    internal WindowsWindowDiscovery(
        IWin32WindowApi windowApi,
        TimeProvider timeProvider,
        TimeSpan candidateLifetime,
        int currentProcessId,
        string identitySalt)
    {
        _windowApi = windowApi ?? throw new ArgumentNullException(nameof(windowApi));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        if (candidateLifetime <= TimeSpan.Zero || candidateLifetime > TimeSpan.FromMinutes(1))
        {
            throw new ArgumentOutOfRangeException(nameof(candidateLifetime));
        }

        if (currentProcessId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentProcessId));
        }

        if (string.IsNullOrWhiteSpace(identitySalt))
        {
            throw new ArgumentException("Identity salt is required.", nameof(identitySalt));
        }

        _candidateLifetime = candidateLifetime;
        _currentProcessId = currentProcessId;
        _identitySalt = identitySalt;
    }

    public WindowSelection? CurrentSelection
    {
        get
        {
            lock (_stateGate)
            {
                return _currentSelection;
            }
        }
    }

    internal int CandidateMappingCount
    {
        get
        {
            lock (_stateGate)
            {
                return _candidateMappings.Count;
            }
        }
    }

    internal int SelectionMappingCount
    {
        get
        {
            lock (_stateGate)
            {
                return _selectionMappings.Count;
            }
        }
    }

    public async ValueTask<WindowDiscoveryResult> DiscoverAsync(
        CancellationToken cancellationToken)
    {
        bool entered = false;
        try
        {
            await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            entered = true;
            return await Task.Run(
                    () => DiscoverCore(cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            ClearAllMappings();
            return WindowDiscoveryResult.Failed(WindowDiscoveryFailure.Cancelled);
        }
        catch (Exception exception) when (IsRecoverablePlatformException(exception))
        {
            ClearAllMappings();
            return WindowDiscoveryResult.Failed(WindowDiscoveryFailure.Unavailable);
        }
        finally
        {
            if (entered)
            {
                _operationGate.Release();
            }
        }
    }

    public async ValueTask<WindowSelectionResult> SelectCandidateAsync(
        CandidateId candidateId,
        DateTimeOffset selectedAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidateId);

        bool entered = false;
        try
        {
            await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            entered = true;
            return await Task.Run(
                    () => SelectCore(candidateId, selectedAtUtc, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            CancelSelection();
            return WindowSelectionResult.Failed(WindowSelectionFailure.Cancelled);
        }
        catch (Exception exception) when (IsRecoverablePlatformException(exception))
        {
            CancelSelection();
            return WindowSelectionResult.Failed(WindowSelectionFailure.CandidateUnavailable);
        }
        finally
        {
            if (entered)
            {
                _operationGate.Release();
            }
        }
    }

    public void CancelSelection()
    {
        lock (_stateGate)
        {
            _selectionMappings.Clear();
            _currentSelection = null;
        }
    }

    internal bool TryResolveSelection(
        SelectionId selectionId,
        out NativeWindowReference nativeWindow)
    {
        ArgumentNullException.ThrowIfNull(selectionId);
        lock (_stateGate)
        {
            return _selectionMappings.TryGetValue(selectionId, out nativeWindow);
        }
    }

    private WindowDiscoveryResult DiscoverCore(CancellationToken cancellationToken)
    {
        ClearAllMappings();
        cancellationToken.ThrowIfCancellationRequested();

        NativeWindowEnumerationResult nativeResult =
            _windowApi.EnumerateTopLevelWindows(cancellationToken);
        if (!nativeResult.IsSuccess)
        {
            return WindowDiscoveryResult.Failed(nativeResult.Failure!.Value);
        }

        DateTimeOffset capturedAtUtc = _timeProvider.GetUtcNow();
        Dictionary<CandidateId, CandidateMapping> discovered = [];
        HashSet<NativeWindowReference> seenWindows = [];
        bool sawAccessDenied = false;
        bool sawIncompleteMetadata = false;

        foreach (NativeWindowReference nativeWindow in nativeResult.Windows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!seenWindows.Add(nativeWindow))
            {
                continue;
            }

            CandidateBuildResult buildResult = TryBuildCandidate(
                nativeWindow,
                capturedAtUtc,
                out WindowCandidate? candidate);
            if (buildResult == CandidateBuildResult.Included)
            {
                discovered.Add(candidate!.CandidateId, new CandidateMapping(candidate, nativeWindow));
            }
            else if (buildResult == CandidateBuildResult.AccessDenied)
            {
                sawAccessDenied = true;
            }
            else if (buildResult == CandidateBuildResult.IncompleteMetadata)
            {
                sawIncompleteMetadata = true;
            }
        }

        if (discovered.Count == 0)
        {
            if (sawAccessDenied)
            {
                return WindowDiscoveryResult.Failed(WindowDiscoveryFailure.AccessDenied);
            }

            if (sawIncompleteMetadata)
            {
                return WindowDiscoveryResult.Failed(WindowDiscoveryFailure.IncompleteMetadata);
            }
        }

        lock (_stateGate)
        {
            foreach ((CandidateId id, CandidateMapping mapping) in discovered)
            {
                _candidateMappings.Add(id, mapping);
            }
        }

        return WindowDiscoveryResult.Succeeded(
            discovered.Values.Select(mapping => mapping.Candidate));
    }

    private CandidateBuildResult TryBuildCandidate(
        NativeWindowReference nativeWindow,
        DateTimeOffset capturedAtUtc,
        out WindowCandidate? candidate)
    {
        candidate = null;
        if (!_windowApi.IsWindow(nativeWindow) ||
            !_windowApi.IsWindowVisible(nativeWindow) ||
            !_windowApi.IsWindowEnabled(nativeWindow) ||
            _windowApi.IsToolWindow(nativeWindow))
        {
            return CandidateBuildResult.Excluded;
        }

        if (!_windowApi.TryGetWindowTitle(nativeWindow, out string title) ||
            string.IsNullOrWhiteSpace(title))
        {
            return CandidateBuildResult.Excluded;
        }

        if (!_windowApi.TryGetWindowProcessId(nativeWindow, out int processId))
        {
            return CandidateBuildResult.Excluded;
        }

        if (processId == _currentProcessId)
        {
            return CandidateBuildResult.Excluded;
        }

        if (!_windowApi.TryGetWindowClassName(nativeWindow, out string className) ||
            string.IsNullOrWhiteSpace(className))
        {
            return CandidateBuildResult.IncompleteMetadata;
        }

        if (!_windowApi.TryGetProcessMetadata(
                processId,
                out NativeProcessMetadata? processMetadata,
                out NativeProcessQueryFailure processFailure))
        {
            return processFailure == NativeProcessQueryFailure.AccessDenied
                ? CandidateBuildResult.AccessDenied
                : processFailure == NativeProcessQueryFailure.ProcessUnavailable
                    ? CandidateBuildResult.Excluded
                    : CandidateBuildResult.IncompleteMetadata;
        }

        if (processMetadata!.StartedAtUtc > capturedAtUtc)
        {
            return CandidateBuildResult.IncompleteMetadata;
        }

        string executableIdentity = CreateOpaqueIdentity(
            "executable",
            processMetadata.ExecutablePath.ToUpperInvariant());
        string processInstanceId = CreateOpaqueIdentity(
            "process",
            processId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            processMetadata.StartedAtUtc.UtcTicks.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            executableIdentity);
        string windowInstanceId = CreateOpaqueIdentity(
            "window",
            nativeWindow.Value.ToInt64().ToString(
                "X",
                System.Globalization.CultureInfo.InvariantCulture),
            processId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            processMetadata.StartedAtUtc.UtcTicks.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            className,
            executableIdentity);

        WindowIdentitySnapshot identity = new(
            windowInstanceId,
            processId,
            processInstanceId,
            processMetadata.StartedAtUtc,
            className,
            processMetadata.ExecutableName,
            executableIdentity,
            capturedAtUtc,
            capturedAtUtc.Add(_candidateLifetime));
        candidate = new WindowCandidate(
            new CandidateId(Guid.NewGuid()),
            title,
            identity);
        return CandidateBuildResult.Included;
    }

    private WindowSelectionResult SelectCore(
        CandidateId candidateId,
        DateTimeOffset selectedAtUtc,
        CancellationToken cancellationToken)
    {
        CandidateMapping? mapping;
        lock (_stateGate)
        {
            _selectionMappings.Clear();
            _currentSelection = null;
            _candidateMappings.TryGetValue(candidateId, out mapping);
        }

        if (mapping is null)
        {
            return WindowSelectionResult.Failed(WindowSelectionFailure.CandidateNotFound);
        }

        if (mapping.Candidate.IsExpiredAt(selectedAtUtc))
        {
            RemoveCandidate(candidateId);
            return WindowSelectionResult.Failed(WindowSelectionFailure.CandidateExpired);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!MatchesInitialIdentity(mapping))
        {
            RemoveCandidate(candidateId);
            return WindowSelectionResult.Failed(WindowSelectionFailure.CandidateUnavailable);
        }

        cancellationToken.ThrowIfCancellationRequested();
        WindowSelection selection = new(
            new SelectionId(Guid.NewGuid()),
            candidateId,
            mapping.Candidate.Identity,
            selectedAtUtc);
        lock (_stateGate)
        {
            _selectionMappings.Add(selection.SelectionId, mapping.NativeWindow);
            _currentSelection = selection;
        }

        return WindowSelectionResult.Succeeded(selection);
    }

    private bool MatchesInitialIdentity(CandidateMapping mapping)
    {
        NativeWindowReference nativeWindow = mapping.NativeWindow;
        WindowIdentitySnapshot expected = mapping.Candidate.Identity;
        if (!_windowApi.IsWindow(nativeWindow) ||
            !_windowApi.IsWindowVisible(nativeWindow) ||
            !_windowApi.IsWindowEnabled(nativeWindow) ||
            _windowApi.IsToolWindow(nativeWindow) ||
            !_windowApi.TryGetWindowProcessId(nativeWindow, out int processId) ||
            processId != expected.ProcessId ||
            !_windowApi.TryGetWindowClassName(nativeWindow, out string className) ||
            !string.Equals(className, expected.WindowClassName, StringComparison.Ordinal) ||
            !_windowApi.TryGetProcessMetadata(
                processId,
                out NativeProcessMetadata? processMetadata,
                out _))
        {
            return false;
        }

        string executableIdentity = CreateOpaqueIdentity(
            "executable",
            processMetadata!.ExecutablePath.ToUpperInvariant());
        string processInstanceId = CreateOpaqueIdentity(
            "process",
            processId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            processMetadata.StartedAtUtc.UtcTicks.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            executableIdentity);
        string windowInstanceId = CreateOpaqueIdentity(
            "window",
            nativeWindow.Value.ToInt64().ToString(
                "X",
                System.Globalization.CultureInfo.InvariantCulture),
            processId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            processMetadata.StartedAtUtc.UtcTicks.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            className,
            executableIdentity);

        return processMetadata.StartedAtUtc == expected.ProcessStartedAtUtc &&
            string.Equals(executableIdentity, expected.ExecutableIdentity, StringComparison.Ordinal) &&
            string.Equals(processInstanceId, expected.ProcessInstanceId, StringComparison.Ordinal) &&
            string.Equals(windowInstanceId, expected.WindowInstanceId, StringComparison.Ordinal);
    }

    private string CreateOpaqueIdentity(string kind, params string[] values)
    {
        string material = string.Join('\u001f', [_identitySalt, kind, .. values]);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private void RemoveCandidate(CandidateId candidateId)
    {
        lock (_stateGate)
        {
            _candidateMappings.Remove(candidateId);
        }
    }

    private void ClearAllMappings()
    {
        lock (_stateGate)
        {
            _candidateMappings.Clear();
            _selectionMappings.Clear();
            _currentSelection = null;
        }
    }

    private static bool IsRecoverablePlatformException(Exception exception) =>
        exception is InvalidOperationException or
            ArgumentException or
            System.ComponentModel.Win32Exception or
            NotSupportedException;

    private sealed record CandidateMapping(
        WindowCandidate Candidate,
        NativeWindowReference NativeWindow);

    private enum CandidateBuildResult
    {
        Excluded = 0,
        Included,
        AccessDenied,
        IncompleteMetadata,
    }
}
