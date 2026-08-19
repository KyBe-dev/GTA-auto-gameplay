using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GtaAutoGameplay.Core.Targeting;

namespace GtaAutoGameplay.Platform.Windows.Windowing;

internal sealed class Win32WindowApi : IWin32WindowApi
{
    private const int ExtendedWindowStyleIndex = -20;
    private const long ToolWindowStyle = 0x00000080L;
    private const int MaxWindowTitleLength = 2048;
    private const int MaxClassNameLength = 256;

    public NativeWindowEnumerationResult EnumerateTopLevelWindows(
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return NativeWindowEnumerationResult.Failed(WindowDiscoveryFailure.Cancelled);
        }

        List<NativeWindowReference> windows = [];
        bool completed = EnumWindows(
            (window, _) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (window != nint.Zero)
                {
                    windows.Add(new NativeWindowReference(window));
                }

                return true;
            },
            nint.Zero);

        if (cancellationToken.IsCancellationRequested)
        {
            return NativeWindowEnumerationResult.Failed(WindowDiscoveryFailure.Cancelled);
        }

        return completed
            ? NativeWindowEnumerationResult.Succeeded(windows)
            : NativeWindowEnumerationResult.Failed(WindowDiscoveryFailure.EnumerationFailed);
    }

    public bool IsWindow(NativeWindowReference window) =>
        IsWindowNative(window.Value);

    public bool IsWindowVisible(NativeWindowReference window) =>
        IsWindowVisibleNative(window.Value);

    public bool IsWindowEnabled(NativeWindowReference window) =>
        IsWindowEnabledNative(window.Value);

    public bool IsToolWindow(NativeWindowReference window) =>
        (GetExtendedWindowStyle(window.Value) & ToolWindowStyle) != 0;

    public bool TryGetWindowTitle(NativeWindowReference window, out string title)
    {
        title = string.Empty;
        if (!IsWindow(window))
        {
            return false;
        }

        int reportedLength = GetWindowTextLength(window.Value);
        if (reportedLength <= 0)
        {
            return false;
        }

        int capacity = Math.Min(reportedLength, MaxWindowTitleLength) + 1;
        StringBuilder buffer = new(capacity);
        int copiedLength = GetWindowText(window.Value, buffer, capacity);
        if (copiedLength <= 0)
        {
            return false;
        }

        title = buffer.ToString();
        return !string.IsNullOrWhiteSpace(title);
    }

    public bool TryGetWindowProcessId(NativeWindowReference window, out int processId)
    {
        processId = 0;
        _ = GetWindowThreadProcessId(window.Value, out uint nativeProcessId);
        if (nativeProcessId == 0 || nativeProcessId > int.MaxValue)
        {
            return false;
        }

        processId = (int)nativeProcessId;
        return true;
    }

    public bool TryGetWindowClassName(
        NativeWindowReference window,
        out string className)
    {
        StringBuilder buffer = new(MaxClassNameLength + 1);
        int copiedLength = GetClassName(window.Value, buffer, buffer.Capacity);
        className = copiedLength > 0 ? buffer.ToString() : string.Empty;
        return !string.IsNullOrWhiteSpace(className);
    }

    public bool TryGetProcessMetadata(
        int processId,
        out NativeProcessMetadata? metadata,
        out NativeProcessQueryFailure failure)
    {
        metadata = null;
        failure = NativeProcessQueryFailure.None;

        try
        {
            using Process process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                failure = NativeProcessQueryFailure.ProcessUnavailable;
                return false;
            }

            DateTimeOffset startedAtUtc = new(
                process.StartTime.ToUniversalTime(),
                TimeSpan.Zero);
            string? executablePath = process.MainModule?.FileName;
            string? executableName = Path.GetFileName(executablePath);
            if (string.IsNullOrWhiteSpace(executablePath) ||
                string.IsNullOrWhiteSpace(executableName))
            {
                failure = NativeProcessQueryFailure.IncompleteMetadata;
                return false;
            }

            metadata = new NativeProcessMetadata(
                startedAtUtc,
                executableName,
                executablePath);
            return true;
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 5)
        {
            failure = NativeProcessQueryFailure.AccessDenied;
            return false;
        }
        catch (ArgumentException)
        {
            failure = NativeProcessQueryFailure.ProcessUnavailable;
            return false;
        }
        catch (InvalidOperationException)
        {
            failure = NativeProcessQueryFailure.ProcessUnavailable;
            return false;
        }
        catch (Win32Exception)
        {
            failure = NativeProcessQueryFailure.IncompleteMetadata;
            return false;
        }
        catch (NotSupportedException)
        {
            failure = NativeProcessQueryFailure.IncompleteMetadata;
            return false;
        }
    }

    private static long GetExtendedWindowStyle(nint window)
    {
        nint style = nint.Size == sizeof(long)
            ? GetWindowLongPtr64(window, ExtendedWindowStyleIndex)
            : new nint(GetWindowLong32(window, ExtendedWindowStyleIndex));
        return style.ToInt64();
    }

    private delegate bool EnumWindowsCallback(nint window, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(
        EnumWindowsCallback callback,
        nint parameter);

    [DllImport("user32.dll", EntryPoint = "IsWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowNative(nint window);

    [DllImport("user32.dll", EntryPoint = "IsWindowVisible")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisibleNative(nint window);

    [DllImport("user32.dll", EntryPoint = "IsWindowEnabled")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabledNative(nint window);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthW", SetLastError = true)]
    private static extern int GetWindowTextLength(nint window);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(
        nint window,
        StringBuilder text,
        int maxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(
        nint window,
        out uint processId);

    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassName(
        nint window,
        StringBuilder className,
        int maxCount);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr64(nint window, int index);
}
