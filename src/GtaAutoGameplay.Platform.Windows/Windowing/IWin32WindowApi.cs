namespace GtaAutoGameplay.Platform.Windows.Windowing;

internal interface IWin32WindowApi
{
    NativeWindowEnumerationResult EnumerateTopLevelWindows(CancellationToken cancellationToken);

    bool IsWindow(NativeWindowReference window);

    bool IsWindowVisible(NativeWindowReference window);

    bool IsWindowEnabled(NativeWindowReference window);

    bool IsToolWindow(NativeWindowReference window);

    bool TryGetWindowTitle(NativeWindowReference window, out string title);

    bool TryGetWindowProcessId(NativeWindowReference window, out int processId);

    bool TryGetWindowClassName(NativeWindowReference window, out string className);

    bool TryGetProcessMetadata(
        int processId,
        out NativeProcessMetadata? metadata,
        out NativeProcessQueryFailure failure);
}
