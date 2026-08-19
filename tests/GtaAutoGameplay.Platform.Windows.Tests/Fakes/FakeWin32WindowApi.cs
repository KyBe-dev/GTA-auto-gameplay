using GtaAutoGameplay.Core.Targeting;
using GtaAutoGameplay.Platform.Windows.Windowing;

namespace GtaAutoGameplay.Platform.Windows.Tests.Fakes;

internal sealed class FakeWin32WindowApi : IWin32WindowApi
{
    private readonly List<NativeWindowReference> _enumeratedWindows = [];
    private readonly Dictionary<NativeWindowReference, WindowData> _windows = [];

    public WindowDiscoveryFailure? EnumerationFailure { get; set; }

    public NativeWindowReference AddWindow(
        long handle,
        int processId,
        string title = "Controlled Test Window",
        string className = "ControlledWindowClass",
        string executableName = "controlled-window.exe",
        string executablePath = "C:\\Controlled\\controlled-window.exe",
        bool isWindow = true,
        bool isVisible = true,
        bool isEnabled = true,
        bool isToolWindow = false,
        bool titleReadSucceeds = true,
        bool processIdReadSucceeds = true,
        bool classNameReadSucceeds = true,
        NativeProcessQueryFailure processFailure = NativeProcessQueryFailure.None)
    {
        NativeWindowReference reference = new(new nint(handle));
        _enumeratedWindows.Add(reference);
        _windows[reference] = new WindowData
        {
            ProcessId = processId,
            Title = title,
            ClassName = className,
            ExecutableName = executableName,
            ExecutablePath = executablePath,
            IsWindow = isWindow,
            IsVisible = isVisible,
            IsEnabled = isEnabled,
            IsToolWindow = isToolWindow,
            TitleReadSucceeds = titleReadSucceeds,
            ProcessIdReadSucceeds = processIdReadSucceeds,
            ClassNameReadSucceeds = classNameReadSucceeds,
            ProcessFailure = processFailure,
        };
        return reference;
    }

    public void AddDuplicate(NativeWindowReference window) =>
        _enumeratedWindows.Add(window);

    public void ReplaceEnumeration(params NativeWindowReference[] windows)
    {
        _enumeratedWindows.Clear();
        _enumeratedWindows.AddRange(windows);
    }

    public void CloseWindow(NativeWindowReference window) =>
        GetData(window).IsWindow = false;

    public void ReuseWindowHandle(
        NativeWindowReference window,
        int processId,
        DateTimeOffset processStartedAtUtc,
        string executableName,
        string executablePath)
    {
        WindowData data = GetData(window);
        data.ProcessId = processId;
        data.ProcessStartedAtUtc = processStartedAtUtc;
        data.ExecutableName = executableName;
        data.ExecutablePath = executablePath;
    }

    public NativeWindowEnumerationResult EnumerateTopLevelWindows(
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return NativeWindowEnumerationResult.Failed(WindowDiscoveryFailure.Cancelled);
        }

        return EnumerationFailure is null
            ? NativeWindowEnumerationResult.Succeeded(_enumeratedWindows)
            : NativeWindowEnumerationResult.Failed(EnumerationFailure.Value);
    }

    public bool IsWindow(NativeWindowReference window) => GetData(window).IsWindow;

    public bool IsWindowVisible(NativeWindowReference window) => GetData(window).IsVisible;

    public bool IsWindowEnabled(NativeWindowReference window) => GetData(window).IsEnabled;

    public bool IsToolWindow(NativeWindowReference window) => GetData(window).IsToolWindow;

    public bool TryGetWindowTitle(NativeWindowReference window, out string title)
    {
        WindowData data = GetData(window);
        title = data.Title;
        return data.TitleReadSucceeds;
    }

    public bool TryGetWindowProcessId(NativeWindowReference window, out int processId)
    {
        WindowData data = GetData(window);
        processId = data.ProcessId;
        return data.ProcessIdReadSucceeds;
    }

    public bool TryGetWindowClassName(
        NativeWindowReference window,
        out string className)
    {
        WindowData data = GetData(window);
        className = data.ClassName;
        return data.ClassNameReadSucceeds;
    }

    public bool TryGetProcessMetadata(
        int processId,
        out NativeProcessMetadata? metadata,
        out NativeProcessQueryFailure failure)
    {
        WindowData? data = _windows.Values.FirstOrDefault(item => item.ProcessId == processId);
        if (data is null)
        {
            metadata = null;
            failure = NativeProcessQueryFailure.ProcessUnavailable;
            return false;
        }

        if (data.ProcessFailure != NativeProcessQueryFailure.None)
        {
            metadata = null;
            failure = data.ProcessFailure;
            return false;
        }

        metadata = new NativeProcessMetadata(
            data.ProcessStartedAtUtc,
            data.ExecutableName,
            data.ExecutablePath);
        failure = NativeProcessQueryFailure.None;
        return true;
    }

    private WindowData GetData(NativeWindowReference window) =>
        _windows.TryGetValue(window, out WindowData? data)
            ? data
            : throw new InvalidOperationException("Unknown fake window reference.");

    private sealed class WindowData
    {
        public int ProcessId { get; set; }

        public string Title { get; init; } = string.Empty;

        public string ClassName { get; init; } = string.Empty;

        public string ExecutableName { get; set; } = string.Empty;

        public string ExecutablePath { get; set; } = string.Empty;

        public DateTimeOffset ProcessStartedAtUtc { get; set; } =
            new(2026, 8, 19, 8, 0, 0, TimeSpan.Zero);

        public bool IsWindow { get; set; }

        public bool IsVisible { get; init; }

        public bool IsEnabled { get; init; }

        public bool IsToolWindow { get; init; }

        public bool TitleReadSucceeds { get; init; }

        public bool ProcessIdReadSucceeds { get; init; }

        public bool ClassNameReadSucceeds { get; init; }

        public NativeProcessQueryFailure ProcessFailure { get; init; }
    }
}
