using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// Keeps a custom-chrome window's maximized bounds inside the monitor's work area.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="WindowStyle.None"/> window maximizes to the whole monitor, so the taskbar covers
/// its bottom rows (the shell's status bar) and the resize frame overshoots every edge. The work
/// area is read from the monitor the window is on at the time, so any monitor size, DPI or
/// taskbar position is handled.
/// </para>
/// <para>
/// Two messages are answered. <c>WM_GETMINMAXINFO</c> sets the maximized size and position.
/// Windows rescales those values when the window maximizes on a secondary monitor of a
/// different size, so <c>WM_WINDOWPOSCHANGING</c> also pins the final bounds of a maximized
/// window to the work area. Only the maximized size, position and the largest tracking size are
/// changed, so WPF's own MinWidth/MinHeight handling still applies.
/// </para>
/// <para>
/// When the taskbar auto-hides, the work area is the whole monitor; the window then stops 1px
/// short of the taskbar's edge, or Windows treats it as full-screen and the taskbar slides out
/// behind it instead of over it.
/// </para>
/// </remarks>
public static class MaximizeToWorkArea
{
	private const int WM_GETMINMAXINFO = 0x0024;
	private const int WM_WINDOWPOSCHANGING = 0x0046;
	private const uint MONITOR_DEFAULTTONEAREST = 2;
	private const uint SWP_NOSIZE = 0x0001;
	private const uint SWP_NOMOVE = 0x0002;
	private const uint ABM_GETSTATE = 0x0004;
	private const uint ABM_GETTASKBARPOS = 0x0005;
	private const int ABS_AUTOHIDE = 0x0001;
	private const uint ABE_LEFT = 0, ABE_TOP = 1, ABE_RIGHT = 2, ABE_BOTTOM = 3;

	/// <summary>Hooks <paramref name="window"/> once its native handle exists.</summary>
	/// <param name="window">A window with <see cref="WindowStyle.None"/>.</param>
	public static void Attach(Window window) =>
		window.SourceInitialized += (_, _) =>
			HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook(WndProc);

	private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		switch (msg)
		{
			case WM_GETMINMAXINFO:
				OnGetMinMaxInfo(hwnd, lParam);
				break;
			case WM_WINDOWPOSCHANGING:
				OnWindowPosChanging(hwnd, lParam);
				break;
		}
		return IntPtr.Zero;
	}

	private static void OnGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
	{
		IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
		if (!TryGetMaximizedBounds(monitor, out RECT bounds, out RECT area)) return;

		var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

		// ptMaxPosition is relative to the monitor's top-left, not the virtual screen.
		mmi.ptMaxPosition.X = bounds.Left - area.Left;
		mmi.ptMaxPosition.Y = bounds.Top - area.Top;
		mmi.ptMaxSize.X = bounds.Right - bounds.Left;
		mmi.ptMaxSize.Y = bounds.Bottom - bounds.Top;

		// Windows clamps the maximized size to this, and fills it in from the primary monitor,
		// so a larger secondary monitor would otherwise maximize short.
		mmi.ptMaxTrackSize.X = Math.Max(mmi.ptMaxTrackSize.X, mmi.ptMaxSize.X);
		mmi.ptMaxTrackSize.Y = Math.Max(mmi.ptMaxTrackSize.Y, mmi.ptMaxSize.Y);

		Marshal.StructureToPtr(mmi, lParam, fDeleteOld: false);
	}

	private static void OnWindowPosChanging(IntPtr hwnd, IntPtr lParam)
	{
		if (!IsZoomed(hwnd)) return;

		var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
		if ((pos.flags & (SWP_NOSIZE | SWP_NOMOVE)) != 0) return;

		// The monitor the window is about to land on, which differs from its current one when a
		// maximized window is sent to another monitor (Win+Shift+Arrow).
		var target = new RECT { Left = pos.x, Top = pos.y, Right = pos.x + pos.cx, Bottom = pos.y + pos.cy };
		IntPtr monitor = MonitorFromRect(ref target, MONITOR_DEFAULTTONEAREST);
		if (!TryGetMaximizedBounds(monitor, out RECT bounds, out _)) return;

		pos.x = bounds.Left;
		pos.y = bounds.Top;
		pos.cx = bounds.Right - bounds.Left;
		pos.cy = bounds.Bottom - bounds.Top;
		Marshal.StructureToPtr(pos, lParam, fDeleteOld: false);
	}

	/// <summary>The monitor's work area, less 1px on the edge an auto-hide taskbar slides in from.</summary>
	private static bool TryGetMaximizedBounds(IntPtr monitor, out RECT bounds, out RECT area)
	{
		var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
		if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
		{
			bounds = area = default;
			return false;
		}

		bounds = info.rcWork;
		area = info.rcMonitor;

		// The per-monitor query (ABM_GETAUTOHIDEBAREX) is unreliable for secondary taskbars - it
		// can report none after auto-hide is toggled - so use the global auto-hide state and the
		// taskbar's edge, which Windows keeps the same on every monitor. Only an edge the work
		// area does not already stop short of is trimmed.
		if (IsAutoHideOn() && TryGetTaskbarEdge(out uint edge))
		{
			if (edge == ABE_LEFT && bounds.Left == area.Left) bounds.Left += 1;
			if (edge == ABE_TOP && bounds.Top == area.Top) bounds.Top += 1;
			if (edge == ABE_RIGHT && bounds.Right == area.Right) bounds.Right -= 1;
			if (edge == ABE_BOTTOM && bounds.Bottom == area.Bottom) bounds.Bottom -= 1;
		}
		return true;
	}

	private static bool IsAutoHideOn()
	{
		var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
		return ((int)SHAppBarMessage(ABM_GETSTATE, ref data) & ABS_AUTOHIDE) != 0;
	}

	private static bool TryGetTaskbarEdge(out uint edge)
	{
		var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
		bool found = SHAppBarMessage(ABM_GETTASKBARPOS, ref data) != IntPtr.Zero;
		edge = data.uEdge;
		return found;
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

	[StructLayout(LayoutKind.Sequential)]
	private struct WINDOWPOS
	{
		public IntPtr hwnd;
		public IntPtr hwndInsertAfter;
		public int x;
		public int y;
		public int cx;
		public int cy;
		public uint flags;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct APPBARDATA
	{
		public int cbSize;
		public IntPtr hWnd;
		public uint uCallbackMessage;
		public uint uEdge;
		public RECT rc;
		public IntPtr lParam;
	}

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);

	[DllImport("user32.dll")]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

	[DllImport("user32.dll")]
	private static extern bool IsZoomed(IntPtr hwnd);

	[DllImport("shell32.dll")]
	private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
}
