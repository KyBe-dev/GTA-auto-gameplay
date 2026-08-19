using GtaAutoGameplay.Core.Targeting;
using GtaAutoGameplay.Platform.Windows.Tests.Fakes;
using GtaAutoGameplay.Platform.Windows.Windowing;

namespace GtaAutoGameplay.Platform.Windows.Tests;

[TestClass]
public sealed class WindowsWindowDiscoveryTests
{
    private static readonly DateTimeOffset DiscoveryTime =
        new(2026, 8, 19, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task NativeWindow_IsConvertedToPlatformNeutralCandidate()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(0x1234, 200, title: "Safe Test Window");
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Candidates);
        WindowCandidate candidate = result.Candidates[0];
        Assert.AreEqual("Safe Test Window", candidate.Title);
        Assert.AreEqual("controlled-window.exe", candidate.ProcessName);
        Assert.AreEqual(200, candidate.ProcessId);
        Assert.AreEqual(DiscoveryTime, candidate.DiscoveredAtUtc);
        Assert.AreEqual(
            DiscoveryTime.Add(WindowsWindowDiscovery.DefaultCandidateLifetime),
            candidate.ValidUntilUtc);
        Assert.HasCount(64, candidate.Identity.WindowInstanceId);
        Assert.HasCount(64, candidate.Identity.ProcessInstanceId);
        Assert.HasCount(64, candidate.Identity.ExecutableIdentity);
    }

    [TestMethod]
    public async Task DuplicateNativeReferences_AreReturnedOnce()
    {
        FakeWin32WindowApi api = new();
        NativeWindowReference window = api.AddWindow(0x1111, 201);
        api.AddDuplicate(window);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Candidates);
        Assert.AreEqual(1, discovery.CandidateMappingCount);
    }

    [TestMethod]
    public async Task WindowsWithSameTitle_RemainDistinctCandidates()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(0x1111, 201, title: "Same Title");
        _ = api.AddWindow(
            0x2222,
            202,
            title: "Same Title",
            executablePath: "C:\\Controlled\\second-window.exe",
            executableName: "second-window.exe");
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.HasCount(2, result.Candidates);
        Assert.AreNotEqual(
            result.Candidates[0].CandidateId,
            result.Candidates[1].CandidateId);
        Assert.AreNotEqual(
            result.Candidates[0].Identity.WindowInstanceId,
            result.Candidates[1].Identity.WindowInstanceId);
    }

    [TestMethod]
    public async Task CurrentProcessWindows_AreExcluded()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(0x1111, 100);
        WindowsWindowDiscovery discovery = CreateDiscovery(api, currentProcessId: 100);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Candidates);
    }

    [TestMethod]
    [DataRow(false, true, true, false)]
    [DataRow(true, false, true, false)]
    [DataRow(true, true, false, false)]
    [DataRow(true, true, true, true)]
    public async Task HiddenInvalidDisabledOrToolWindows_AreExcluded(
        bool isWindow,
        bool isVisible,
        bool isEnabled,
        bool isToolWindow)
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(
            0x1111,
            201,
            isWindow: isWindow,
            isVisible: isVisible,
            isEnabled: isEnabled,
            isToolWindow: isToolWindow);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Candidates);
    }

    [TestMethod]
    [DataRow("", true)]
    [DataRow("Ignored", false)]
    public async Task EmptyOrUnreadableTitle_IsSkippedWithoutCrash(
        string title,
        bool titleReadSucceeds)
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(
            0x1111,
            201,
            title: title,
            titleReadSucceeds: titleReadSucceeds);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Candidates);
    }

    [TestMethod]
    public async Task ExitedProcess_IsSkippedWithoutCrash()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(
            0x1111,
            201,
            processFailure: NativeProcessQueryFailure.ProcessUnavailable);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Candidates);
    }

    [TestMethod]
    public async Task MissingProcessId_IsSkippedWithoutCrash()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(0x1111, 201, processIdReadSucceeds: false);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Candidates);
    }

    [TestMethod]
    public async Task MissingClassName_ReturnsIncompleteMetadataFailure()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(0x1111, 201, classNameReadSucceeds: false);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.AreEqual(WindowDiscoveryFailure.IncompleteMetadata, result.Failure);
        Assert.IsEmpty(result.Candidates);
    }

    [TestMethod]
    [DataRow(WindowDiscoveryFailure.EnumerationFailed)]
    [DataRow(WindowDiscoveryFailure.Unavailable)]
    public async Task NativeEnumerationFailure_IsReturnedAsTypedFailure(
        WindowDiscoveryFailure failure)
    {
        FakeWin32WindowApi api = new()
        {
            EnumerationFailure = failure,
        };
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.AreEqual(failure, result.Failure);
        Assert.IsEmpty(result.Candidates);
    }

    [TestMethod]
    public async Task AccessDeniedWithoutOtherCandidates_ReturnsTypedFailure()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(
            0x1111,
            201,
            processFailure: NativeProcessQueryFailure.AccessDenied);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(WindowDiscoveryFailure.AccessDenied, result.Failure);
        Assert.IsEmpty(result.Candidates);
    }

    [TestMethod]
    public async Task RefreshInvalidatesOldCandidateAndSelectionMappings()
    {
        FakeWin32WindowApi api = new();
        NativeWindowReference firstWindow = api.AddWindow(0x1111, 201);
        NativeWindowReference secondWindow = api.AddWindow(
            0x2222,
            202,
            executablePath: "C:\\Controlled\\second-window.exe",
            executableName: "second-window.exe");
        api.ReplaceEnumeration(firstWindow);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);
        WindowDiscoveryResult firstScan = await discovery.DiscoverAsync(CancellationToken.None);
        WindowCandidate oldCandidate = firstScan.Candidates[0];
        WindowSelectionResult selected = await discovery.SelectCandidateAsync(
            oldCandidate.CandidateId,
            DiscoveryTime.AddSeconds(1),
            CancellationToken.None);
        Assert.IsTrue(selected.IsSuccess);

        api.ReplaceEnumeration(secondWindow);
        WindowDiscoveryResult refreshed = await discovery.DiscoverAsync(CancellationToken.None);
        WindowSelectionResult oldSelectionAttempt = await discovery.SelectCandidateAsync(
            oldCandidate.CandidateId,
            DiscoveryTime.AddSeconds(2),
            CancellationToken.None);

        Assert.HasCount(1, refreshed.Candidates);
        Assert.AreEqual(202, refreshed.Candidates[0].ProcessId);
        Assert.AreEqual(WindowSelectionFailure.CandidateNotFound, oldSelectionAttempt.Failure);
        Assert.IsNull(discovery.CurrentSelection);
        Assert.AreEqual(0, discovery.SelectionMappingCount);
    }

    [TestMethod]
    public async Task ExplicitSelection_CreatesNewSelectionAndNativeMapping()
    {
        FakeWin32WindowApi api = new();
        NativeWindowReference nativeWindow = api.AddWindow(0x1111, 201);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);
        WindowCandidate candidate = (await discovery.DiscoverAsync(CancellationToken.None))
            .Candidates[0];

        WindowSelectionResult first = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            DiscoveryTime.AddSeconds(1),
            CancellationToken.None);
        WindowSelectionResult second = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            DiscoveryTime.AddSeconds(2),
            CancellationToken.None);

        Assert.IsTrue(first.IsSuccess);
        Assert.IsTrue(second.IsSuccess);
        Assert.AreNotEqual(first.Selection!.SelectionId, second.Selection!.SelectionId);
        Assert.AreEqual(1, discovery.SelectionMappingCount);
        Assert.IsTrue(discovery.TryResolveSelection(second.Selection.SelectionId, out NativeWindowReference resolved));
        Assert.AreEqual(nativeWindow, resolved);
        Assert.IsFalse(discovery.TryResolveSelection(first.Selection.SelectionId, out _));
    }

    [TestMethod]
    public async Task SingleCandidate_DoesNotCreateSelectionAutomatically()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(0x1111, 201);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);

        WindowDiscoveryResult result = await discovery.DiscoverAsync(CancellationToken.None);

        Assert.HasCount(1, result.Candidates);
        Assert.IsNull(discovery.CurrentSelection);
        Assert.AreEqual(0, discovery.SelectionMappingCount);
    }

    [TestMethod]
    public async Task CancelSelection_ClearsSelectionAndNativeMapping()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(0x1111, 201);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);
        WindowCandidate candidate = (await discovery.DiscoverAsync(CancellationToken.None))
            .Candidates[0];
        WindowSelectionResult selected = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            DiscoveryTime.AddSeconds(1),
            CancellationToken.None);
        Assert.IsTrue(selected.IsSuccess);

        discovery.CancelSelection();

        Assert.IsNull(discovery.CurrentSelection);
        Assert.AreEqual(0, discovery.SelectionMappingCount);
        Assert.IsFalse(discovery.TryResolveSelection(selected.Selection!.SelectionId, out _));
    }

    [TestMethod]
    public async Task WindowClosedBeforeSelection_ReturnsUnavailableAndRemovesCandidate()
    {
        FakeWin32WindowApi api = new();
        NativeWindowReference nativeWindow = api.AddWindow(0x1111, 201);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);
        WindowCandidate candidate = (await discovery.DiscoverAsync(CancellationToken.None))
            .Candidates[0];
        api.CloseWindow(nativeWindow);

        WindowSelectionResult result = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            DiscoveryTime.AddSeconds(1),
            CancellationToken.None);

        Assert.AreEqual(WindowSelectionFailure.CandidateUnavailable, result.Failure);
        Assert.AreEqual(0, discovery.CandidateMappingCount);
        Assert.IsNull(discovery.CurrentSelection);
    }

    [TestMethod]
    public async Task ReusedWindowHandle_CannotInheritOldCandidateIdentity()
    {
        FakeWin32WindowApi api = new();
        NativeWindowReference nativeWindow = api.AddWindow(0x1111, 201);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);
        WindowCandidate candidate = (await discovery.DiscoverAsync(CancellationToken.None))
            .Candidates[0];
        api.ReuseWindowHandle(
            nativeWindow,
            processId: 301,
            processStartedAtUtc: DiscoveryTime.AddMinutes(-1),
            executableName: "replacement-window.exe",
            executablePath: "C:\\Controlled\\replacement-window.exe");

        WindowSelectionResult result = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            DiscoveryTime.AddSeconds(1),
            CancellationToken.None);

        Assert.AreEqual(WindowSelectionFailure.CandidateUnavailable, result.Failure);
        Assert.IsNull(discovery.CurrentSelection);
        Assert.AreEqual(0, discovery.CandidateMappingCount);
    }

    [TestMethod]
    public async Task ExpiredCandidate_CannotBeSelected()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(0x1111, 201);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);
        WindowCandidate candidate = (await discovery.DiscoverAsync(CancellationToken.None))
            .Candidates[0];

        WindowSelectionResult result = await discovery.SelectCandidateAsync(
            candidate.CandidateId,
            candidate.ValidUntilUtc,
            CancellationToken.None);

        Assert.AreEqual(WindowSelectionFailure.CandidateExpired, result.Failure);
        Assert.AreEqual(0, discovery.CandidateMappingCount);
    }

    [TestMethod]
    public async Task PlatformNeutralModels_DoNotExposeNativeWindowReference()
    {
        const long RawValue = 0x1234;
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(RawValue, 201);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);
        WindowCandidate candidate = (await discovery.DiscoverAsync(CancellationToken.None))
            .Candidates[0];

        Assert.AreNotEqual(RawValue.ToString(), candidate.Identity.WindowInstanceId);
        Assert.AreNotEqual(RawValue.ToString("X"), candidate.Identity.WindowInstanceId);
        Assert.IsFalse(
            typeof(WindowCandidate).GetProperties().Any(property =>
                property.PropertyType == typeof(nint)));
        Assert.IsFalse(
            typeof(WindowSelection).GetProperties().Any(property =>
                property.PropertyType == typeof(nint)));
    }

    [TestMethod]
    public void Discovery_HasNoLoggingCaptureSafetyOrInputDependencies()
    {
        string[] forbiddenNamespaceParts =
        [
            ".Logging",
            ".Input",
            ".Safety",
            ".Capture",
            ".AI",
            ".Credentials",
        ];

        Type discoveryType = typeof(WindowsWindowDiscovery);
        IEnumerable<Type> exposedTypes = discoveryType
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Concat(discoveryType.GetProperties().Select(property => property.PropertyType));

        foreach (Type type in exposedTypes)
        {
            Assert.IsFalse(forbiddenNamespaceParts.Any(part =>
                type.FullName?.Contains(part, StringComparison.Ordinal) == true));
        }
    }

    [TestMethod]
    public async Task CancelledRefresh_ReturnsTypedFailureAndClearsMappings()
    {
        FakeWin32WindowApi api = new();
        _ = api.AddWindow(0x1111, 201);
        WindowsWindowDiscovery discovery = CreateDiscovery(api);
        _ = await discovery.DiscoverAsync(CancellationToken.None);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        WindowDiscoveryResult result = await discovery.DiscoverAsync(cancellation.Token);

        Assert.AreEqual(WindowDiscoveryFailure.Cancelled, result.Failure);
        Assert.AreEqual(0, discovery.CandidateMappingCount);
        Assert.AreEqual(0, discovery.SelectionMappingCount);
    }

    private static WindowsWindowDiscovery CreateDiscovery(
        FakeWin32WindowApi api,
        int currentProcessId = 100) =>
        new(
            api,
            new ManualTimeProvider(DiscoveryTime),
            WindowsWindowDiscovery.DefaultCandidateLifetime,
            currentProcessId,
            "fixed-test-salt");

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
