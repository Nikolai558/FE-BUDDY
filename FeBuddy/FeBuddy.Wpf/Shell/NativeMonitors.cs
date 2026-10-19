using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using FeBuddy.Wpf.Shell.Models;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// The monitors connected now, as Windows reports them: each one's bounds and work area in physical
/// pixels, and its scaling. For <see cref="MainWindowPlacement"/> and the dialogs that size themselves
/// to the monitor they open on.
/// </summary>
internal static class NativeMonitors
{
	private const uint MONITOR_DEFAULTTONEAREST = 2;
	private const uint MONITORINFOF_PRIMARY = 1;
	private const int MDT_EFFECTIVE_DPI = 0;

	/// <summary>Every connected monitor.</summary>
	/// <param name="main">The main monitor, or <see langword="null"/> when Windows reports none.</param>
	/// <returns>The monitors; empty when they could not be read.</returns>
	public static IReadOnlyList<MonitorArea> All(out MonitorArea? main)
	{
		List<MonitorArea> monitors = [];
		MonitorArea? primary = null;

		bool OnMonitor(IntPtr monitor, IntPtr deviceContext, IntPtr bounds, IntPtr data)
		{
			if (TryRead(monitor, out MonitorArea? area, out bool isPrimary))
			{
				monitors.Add(area);
				primary ??= isPrimary ? area : null;
			}

			return true;
		}

		MonitorEnumProc callback = OnMonitor;
		EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
		GC.KeepAlive(callback);

		main = primary ?? monitors.FirstOrDefault();
		return monitors;
	}

	/// <summary>The monitor a window is on (the nearest, when it is on none), or <see langword="null"/> when it can't be read.</summary>
	/// <param name="hwnd">The window's handle.</param>
	/// <returns>The monitor, or <see langword="null"/>.</returns>
	public static MonitorArea? Of(IntPtr hwnd) =>
		hwnd != IntPtr.Zero && TryRead(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), out MonitorArea? area, out _) ? area : null;

	/// <summary>A Win32 rectangle as a <see cref="PixelRect"/>.</summary>
	/// <param name="rect">The rectangle.</param>
	/// <returns>The same rectangle.</returns>
	internal static PixelRect ToPixelRect(RECT rect) => new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

	/// <summary>A <see cref="PixelRect"/> as a Win32 rectangle.</summary>
	/// <param name="rect">The rectangle.</param>
	/// <returns>The same rectangle.</returns>
	internal static RECT ToRect(PixelRect rect) => new() { Left = rect.Left, Top = rect.Top, Right = rect.Right, Bottom = rect.Bottom };

	private static bool TryRead(IntPtr monitor, [NotNullWhen(true)] out MonitorArea? area, out bool isPrimary)
	{
		MONITORINFO info = new() { cbSize = Marshal.SizeOf<MONITORINFO>() };

		if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
		{
			area = null;
			isPrimary = false;
			return false;
		}

		// Its effective scaling, which per-monitor DPI awareness (app.manifest) reports per monitor.
		int dpi = GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 && dpiX > 0
			? (int)dpiX
			: WindowPlacementRules.StandardDpi;

		area = new MonitorArea(ToPixelRect(info.rcMonitor), ToPixelRect(info.rcWork), dpi);
		isPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;
		return true;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MONITORINFO
	{
		public int cbSize;
		public RECT rcMonitor;
		public RECT rcWork;
		public uint dwFlags;
	}

	private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr deviceContext, IntPtr bounds, IntPtr data);

	[DllImport("user32.dll")]
	private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

	[DllImport("user32.dll")]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

	[DllImport("shcore.dll")]
	private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
}
