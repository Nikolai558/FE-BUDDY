using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// Remembers FE-Buddy's main window - its size, its position and whether it was maximized - from one
/// launch to the next (Feature #364), and puts it back only where it can be seen and reached on the
/// screens connected now (<see cref="WindowPlacementRules"/>). Only the main window does; dialogs open
/// over it.
/// </summary>
/// <remarks>
/// <para>
/// Both settings are this PC's (<see cref="UserConfigKeys.RememberWindowPlacement"/>,
/// <see cref="UserConfigKeys.MainWindowPlacement"/>): kept in <c>Shared.json</c> for every profile,
/// never exported or imported, and deleted with the settings by a reset. They are saved as they
/// change (<see cref="UserConfigFile.SaveShared"/>), not with the Settings page's Save.
/// </para>
/// <para>
/// Windows' own placement functions save and restore it, in physical screen pixels, the unit the
/// rules check in. They take the normal bounds in workspace coordinates, which this turns to and
/// from screen coordinates (<see cref="WindowPlacementRules.ToWorkspace"/>). The window is placed
/// while still hidden, before it is first shown, so it never shows at the default first.
/// </para>
/// <para>
/// Nothing here ever stops FE-Buddy opening or closing: a failure is logged, and the window opens at
/// its default.
/// </para>
/// </remarks>
public static class MainWindowPlacement
{
	private const string LogSource = "Window";

	private const int SW_HIDE = 0;
	private const int SW_SHOWNORMAL = 1;

	/// <summary>
	/// Whether the main window reopens where it was closed: yes unless turned off. Read from
	/// <c>Shared.json</c>, as the window opens before the launch has read the settings.
	/// </summary>
	public static bool IsRemembered => UserConfigFile.ReadSharedValue(UserConfigKeys.RememberWindowPlacement) != "N";

	/// <summary>
	/// Where the main window was when it closed, or <see langword="null"/> when nothing is saved, what
	/// is saved can't be read, or <see cref="IsRemembered"/> is off. Read from <c>Shared.json</c>.
	/// </summary>
	public static SavedWindowPlacement? Saved =>
		IsRemembered ? SavedWindowPlacement.Parse(UserConfigFile.ReadSharedValue(UserConfigKeys.MainWindowPlacement)) : null;

	/// <summary>Turns remembering on or off, and saves it at once. Off also forgets where the window was.</summary>
	/// <param name="remember">Whether to remember.</param>
	public static void SetRemembered(bool remember)
	{
		UserConfigFile.TrySetValue(UserConfigKeys.RememberWindowPlacement, remember ? "Y" : "N");

		if (!remember)
		{
			UserConfigFile.RemoveValues(UserConfigKeys.MainWindowPlacement);
		}

		SaveShared(UserConfigKeys.RememberWindowPlacement, UserConfigKeys.MainWindowPlacement);
	}

	/// <summary>Keeps <paramref name="placement"/> as where the window was, and saves it - only while <see cref="IsRemembered"/> is on.</summary>
	/// <param name="placement">Where the window is.</param>
	public static void Remember(SavedWindowPlacement placement)
	{
		ArgumentNullException.ThrowIfNull(placement);

		if (!IsRemembered)
		{
			return;
		}

		UserConfigFile.TrySetValue(UserConfigKeys.MainWindowPlacement, placement.Format());
		SaveShared(UserConfigKeys.MainWindowPlacement);
	}

	/// <summary>Forgets where the window was, and saves that.</summary>
	public static void Forget()
	{
		UserConfigFile.RemoveValues(UserConfigKeys.MainWindowPlacement);
		SaveShared(UserConfigKeys.MainWindowPlacement);
	}

	/// <summary>
	/// Puts the main window where it was when it closed, or at its default, before it is first shown.
	/// Call it from <see cref="Window.SourceInitialized"/>, after <c>MaximizeToWorkArea</c> is attached.
	/// </summary>
	/// <param name="window">The main window.</param>
	public static void Restore(Window window)
	{
		ArgumentNullException.ThrowIfNull(window);

		try
		{
			IntPtr hwnd = new WindowInteropHelper(window).Handle;
			IReadOnlyList<MonitorArea> monitors = NativeMonitors.All(out MonitorArea? main);

			if (main is null)
			{
				AppLog.Warning(LogSource, "Could not read the screens, so FE-Buddy opens in the middle of the main screen.");
				CenterOnMainScreen(window);
				return;
			}

			SavedWindowPlacement? saved = Saved;
			WindowPlacementPlan plan = WindowPlacementRules.Restore(saved, monitors, main, WindowSizes.Shell);

			Place(hwnd, plan.Normal, monitors, SW_HIDE);

			if (plan.Maximized)
			{
				// Shown maximized, on the monitor its normal bounds are on; MaximizeToWorkArea keeps it off the taskbar.
				window.WindowState = WindowState.Maximized;
			}

			AppLog.Debug(LogSource, saved is null
				? $"Opening at the default: {Describe(plan)}."
				: $"Saved {saved.Format()}; opening at {Describe(plan)}.");
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			AppLog.Warning(LogSource, $"Could not put the window back where it was: {ex.Message}");
		}
	}

	/// <summary>
	/// Keeps where the main window is as it closes: its normal bounds and whether it was maximized
	/// (or minimized from maximized). Does nothing while <see cref="IsRemembered"/> is off.
	/// </summary>
	/// <param name="window">The main window, before its handle is gone.</param>
	public static void Save(Window window)
	{
		ArgumentNullException.ThrowIfNull(window);

		if (!IsRemembered)
		{
			return;
		}

		try
		{
			IntPtr hwnd = new WindowInteropHelper(window).Handle;
			WINDOWPLACEMENT placement = new() { length = Marshal.SizeOf<WINDOWPLACEMENT>() };

			if (hwnd == IntPtr.Zero || !GetWindowPlacement(hwnd, ref placement))
			{
				return;
			}

			IReadOnlyList<MonitorArea> monitors = NativeMonitors.All(out _);
			PixelRect normal = WindowPlacementRules.FromWorkspace(NativeMonitors.ToPixelRect(placement.rcNormalPosition), monitors);
			int dpi = WindowPlacementRules.MonitorOf(normal, monitors)?.Dpi ?? (int)GetDpiForWindow(hwnd);

			Remember(WindowPlacementRules.Capture(placement.showCmd, placement.flags, normal, dpi));
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			AppLog.Warning(LogSource, $"Could not keep where the window was: {ex.Message}");
		}
	}

	/// <summary>Moves the main window to its default at once: its default size, in the middle of the main screen, not maximized.</summary>
	/// <param name="window">The main window.</param>
	public static void MoveToDefault(Window window)
	{
		ArgumentNullException.ThrowIfNull(window);

		try
		{
			window.WindowState = WindowState.Normal;

			IReadOnlyList<MonitorArea> monitors = NativeMonitors.All(out MonitorArea? main);

			if (main is null)
			{
				CenterOnMainScreen(window);
				return;
			}

			Place(new WindowInteropHelper(window).Handle, WindowPlacementRules.Default(main, WindowSizes.Shell).Normal, monitors, SW_SHOWNORMAL);
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			AppLog.Warning(LogSource, $"Could not move the window to its default: {ex.Message}");
		}
	}

	/// <summary>Sets the window's normal bounds, in screen pixels, and shows it (or keeps it hidden) with <paramref name="showCmd"/>.</summary>
	private static void Place(IntPtr hwnd, PixelRect bounds, IReadOnlyList<MonitorArea> monitors, int showCmd)
	{
		WINDOWPLACEMENT placement = new()
		{
			length = Marshal.SizeOf<WINDOWPLACEMENT>(),
			showCmd = showCmd,
			ptMinPosition = new POINT { X = -1, Y = -1 },
			ptMaxPosition = new POINT { X = -1, Y = -1 },
			rcNormalPosition = NativeMonitors.ToRect(WindowPlacementRules.ToWorkspace(bounds, monitors)),
		};

		// Twice: the first can take the window onto a monitor with other scaling, which makes Windows
		// rescale it (WM_DPICHANGED); the second puts the exact bounds back.
		SetWindowPlacement(hwnd, ref placement);
		SetWindowPlacement(hwnd, ref placement);
	}

	/// <summary>The default when the screens can't be read: as WPF would center it, on the main screen.</summary>
	private static void CenterOnMainScreen(Window window)
	{
		Rect area = SystemParameters.WorkArea;
		double width = Math.Min(WindowSizes.Shell.DefaultWidth, area.Width);
		double height = Math.Min(WindowSizes.Shell.DefaultHeight, area.Height);

		window.Width = width;
		window.Height = height;
		window.Left = area.Left + ((area.Width - width) / 2);
		window.Top = area.Top + ((area.Height - height) / 2);
	}

	private static string Describe(WindowPlacementPlan plan) =>
		$"{plan.Normal.Left},{plan.Normal.Top} {plan.Normal.Width}x{plan.Normal.Height}{(plan.Maximized ? ", maximized" : string.Empty)}";

	/// <summary>Saves shared settings, logging rather than throwing when the file can't be written.</summary>
	private static void SaveShared(params string[] keys)
	{
		try
		{
			UserConfigFile.SaveShared(keys);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Could not save the window's settings: {ex.Message}");
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct WINDOWPLACEMENT
	{
		public int length;
		public int flags;
		public int showCmd;
		public POINT ptMinPosition;
		public POINT ptMaxPosition;
		public NativeMonitors.RECT rcNormalPosition;
	}

	[DllImport("user32.dll")]
	private static extern bool GetWindowPlacement(IntPtr hwnd, ref WINDOWPLACEMENT placement);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPlacement(IntPtr hwnd, ref WINDOWPLACEMENT placement);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(IntPtr hwnd);
}
