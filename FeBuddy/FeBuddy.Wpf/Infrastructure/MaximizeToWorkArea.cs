using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// Keeps a custom-chrome window's maximized bounds inside the monitor's work area.
/// </summary>
/// <remarks>
/// A <see cref="WindowStyle.None"/> window maximizes to the whole monitor, so the taskbar covers
/// its bottom rows (the shell's status bar) and the resize frame overshoots every edge. Answering
/// <c>WM_GETMINMAXINFO</c> with the work area of the monitor the window is on fixes both, on any
/// monitor, DPI and taskbar position. Only the maximized size, position and the largest tracking
/// size are changed, so WPF's own MinWidth/MinHeight handling of the same message still applies.
/// </remarks>
public static class MaximizeToWorkArea
{
    private const int WM_GETMINMAXINFO = 0x0024;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    /// <summary>Hooks <paramref name="window"/> once its native handle exists.</summary>
    public static void Attach(Window window) =>
        window.SourceInitialized += (_, _) =>
            HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook(WndProc);

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_GETMINMAXINFO) return IntPtr.Zero;

        IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return IntPtr.Zero;

        RECT work = info.rcWork;
        RECT area = info.rcMonitor;
        var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

        // ptMaxPosition is relative to the monitor's top-left, not the virtual screen.
        mmi.ptMaxPosition.X = work.Left - area.Left;
        mmi.ptMaxPosition.Y = work.Top - area.Top;
        mmi.ptMaxSize.X = work.Right - work.Left;
        mmi.ptMaxSize.Y = work.Bottom - work.Top;

        // Windows clamps the maximized size to this, and fills it in from the primary monitor,
        // so a larger secondary monitor would otherwise maximize short.
        mmi.ptMaxTrackSize.X = Math.Max(mmi.ptMaxTrackSize.X, mmi.ptMaxSize.X);
        mmi.ptMaxTrackSize.Y = Math.Max(mmi.ptMaxTrackSize.Y, mmi.ptMaxSize.Y);

        Marshal.StructureToPtr(mmi, lParam, fDeleteOld: false);
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
}
