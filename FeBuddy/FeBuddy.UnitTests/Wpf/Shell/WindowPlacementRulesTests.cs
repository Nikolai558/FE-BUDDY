using System.Windows;

using FeBuddy.Wpf.Shell;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers <see cref="WindowPlacementRules"/> and <see cref="SavedWindowPlacement"/> (Feature #364):
/// the main window opens where it was closed when that is on a screen connected now, and otherwise
/// at its default - never with its title bar out of reach, never bigger than the screen, never
/// minimized - with every size at the scaling of the monitor it is measured on.
/// </summary>
public sealed class WindowPlacementRulesTests
{
	/// <summary>A 1920 x 1080 main screen at 100%, its taskbar 40 high along the bottom.</summary>
	private static readonly MonitorArea Main = new(new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 96);

	/// <summary>A 2560 x 1440 screen at 150%, to the right of <see cref="Main"/>, its taskbar 40 high.</summary>
	private static readonly MonitorArea Second = new(new PixelRect(1920, 0, 2560, 1440), new PixelRect(1920, 0, 2560, 1400), 144);

	private static readonly MonitorArea[] Both = [Main, Second];

	private static readonly WindowSizes Sizes = WindowSizes.Shell;

	/// <summary>1200 x 780 in the middle of <see cref="Main"/>'s work area.</summary>
	private static readonly PixelRect MainDefault = new(360, 130, 1200, 780);

	private static WindowPlacementPlan Restore(SavedWindowPlacement? saved, params MonitorArea[] monitors) =>
		WindowPlacementRules.Restore(saved, monitors, monitors[0], Sizes);

	private static SavedWindowPlacement Saved(int left, int top, int width, int height, int dpi = 96, bool maximized = false) =>
		new(new PixelRect(left, top, width, height), dpi, maximized);

	// ---- where it was ----

	[Fact]
	public void a_placement_fully_on_a_connected_monitor_is_used_as_it_is()
	{
		WindowPlacementPlan plan = Restore(Saved(2100, 100, 1800, 1170, dpi: 144), Both);

		Assert.Equal(new WindowPlacementPlan(new PixelRect(2100, 100, 1800, 1170), Maximized: false), plan);
	}

	[Fact]
	public void a_placement_on_a_monitor_that_is_gone_opens_at_the_default_on_the_main_screen()
	{
		WindowPlacementPlan plan = Restore(Saved(2100, 100, 1800, 1170, dpi: 144), Main);

		Assert.Equal(new WindowPlacementPlan(MainDefault, Maximized: false), plan);
	}

	[Fact]
	public void nothing_saved_opens_at_the_default_on_the_main_screen()
	{
		Assert.Equal(new WindowPlacementPlan(MainDefault, Maximized: false), Restore(null, Both));
	}

	[Fact]
	public void the_default_is_the_default_size_at_the_main_screens_scaling_in_the_middle_of_its_work_area()
	{
		WindowPlacementPlan plan = WindowPlacementRules.Restore(null, Both, Second, Sizes);

		// 1200 x 780 at 150% is 1800 x 1170, in the middle of 2560 x 1400 from x 1920.
		Assert.Equal(new WindowPlacementPlan(new PixelRect(2300, 115, 1800, 1170), Maximized: false), plan);
	}

	[Fact]
	public void the_default_shrinks_to_fit_a_small_main_screen()
	{
		MonitorArea small = new(new PixelRect(0, 0, 1024, 768), new PixelRect(0, 0, 1024, 728), 96);

		WindowPlacementPlan plan = WindowPlacementRules.Default(small, Sizes);

		Assert.Equal(new PixelRect(0, 0, 1024, 728), plan.Normal);
	}

	// ---- the resolution dropped ----

	[Fact]
	public void a_window_bigger_than_the_work_area_now_shrinks_to_fit_and_moves_fully_on_screen()
	{
		MonitorArea lowered = new(new PixelRect(0, 0, 1280, 720), new PixelRect(0, 0, 1280, 680), 96);

		WindowPlacementPlan plan = Restore(Saved(100, 50, 1800, 1000), lowered);

		Assert.Equal(new WindowPlacementPlan(new PixelRect(0, 0, 1280, 680), Maximized: false), plan);
	}

	[Fact]
	public void a_window_partly_off_the_side_moves_fully_on_screen()
	{
		WindowPlacementPlan plan = Restore(Saved(1500, 200, 1200, 780), Main);

		Assert.Equal(new PixelRect(720, 200, 1200, 780), plan.Normal);
	}

	[Fact]
	public void a_window_smaller_than_its_minimum_grows_to_it()
	{
		WindowPlacementPlan plan = Restore(Saved(100, 100, 500, 300), Main);

		Assert.Equal(new PixelRect(100, 100, 980, 640), plan.Normal);
	}

	[Fact]
	public void a_work_area_smaller_than_the_minimum_size_is_filled()
	{
		MonitorArea tiny = new(new PixelRect(0, 0, 800, 600), new PixelRect(0, 0, 800, 560), 96);

		WindowPlacementPlan plan = Restore(Saved(0, 0, 1200, 780), tiny);

		Assert.Equal(new PixelRect(0, 0, 800, 560), plan.Normal);
	}

	// ---- the title bar ----

	[Theory]
	[InlineData(-1)] // its top above the screen
	[InlineData(995)] // 45 of its 46 rows above the taskbar
	public void a_title_bar_not_fully_inside_the_work_area_opens_at_the_default(int top)
	{
		Assert.Equal(MainDefault, Restore(Saved(300, top, 1200, 780), Main).Normal);
	}

	[Fact]
	public void a_title_bar_whose_whole_height_shows_is_kept()
	{
		// Its top row at 994 leaves all 46 rows of the title bar above the taskbar at 1040.
		Assert.NotEqual(MainDefault, Restore(Saved(300, 994, 1200, 780), Main).Normal);
	}

	[Theory]
	[InlineData(1820, false)] // 100 of its width on screen, past the right edge
	[InlineData(1821, true)] // 99
	[InlineData(-1100, false)] // 100, past the left edge
	[InlineData(-1101, true)] // 99
	public void a_title_bar_needs_100_of_its_width_on_screen(int left, bool opensAtDefault)
	{
		WindowPlacementPlan plan = Restore(Saved(left, 100, 1200, 780), Main);

		Assert.Equal(opensAtDefault, plan.Normal == MainDefault);
		Assert.InRange(plan.Normal.Left, 0, 1920 - plan.Normal.Width);
	}

	[Fact]
	public void a_title_bar_across_two_monitors_is_kept_on_the_one_it_is_most_on()
	{
		// 150 of 1800 on Main, the rest on Second; both have enough of it at full height.
		WindowPlacementPlan plan = Restore(Saved(1770, 100, 1800, 1170, dpi: 144), Both);

		Assert.Equal(new PixelRect(1920, 100, 1800, 1170), plan.Normal);
	}

	// ---- maximized ----

	[Fact]
	public void saved_maximized_on_a_connected_monitor_maximizes_on_it()
	{
		WindowPlacementPlan plan = Restore(Saved(2100, 100, 1800, 1170, dpi: 144, maximized: true), Both);

		Assert.Equal(new WindowPlacementPlan(new PixelRect(2100, 100, 1800, 1170), Maximized: true), plan);
	}

	[Fact]
	public void saved_maximized_on_a_monitor_that_is_gone_maximizes_on_the_main_screen()
	{
		WindowPlacementPlan plan = Restore(Saved(2100, 100, 1800, 1170, dpi: 144, maximized: true), Main);

		Assert.Equal(new WindowPlacementPlan(MainDefault, Maximized: true), plan);
	}

	[Fact]
	public void saved_maximized_with_its_title_bar_out_of_reach_maximizes_on_its_monitor_and_restores_to_its_middle()
	{
		WindowPlacementPlan plan = Restore(Saved(2100, -50, 1800, 1170, dpi: 144, maximized: true), Both);

		Assert.Equal(new WindowPlacementPlan(new PixelRect(2300, 115, 1800, 1170), Maximized: true), plan);
	}

	// ---- minimized ----

	[Theory]
	[InlineData(1, 0, false)] // SW_SHOWNORMAL
	[InlineData(3, 0, true)] // SW_SHOWMAXIMIZED
	[InlineData(2, 0, false)] // SW_SHOWMINIMIZED, from normal
	[InlineData(2, 2, true)] // SW_SHOWMINIMIZED, from maximized (WPF_RESTORETOMAXIMIZED)
	[InlineData(6, 2, true)] // SW_MINIMIZE, from maximized
	[InlineData(7, 0, false)] // SW_SHOWMINNOACTIVE, from normal
	[InlineData(1, 2, false)] // the flag means nothing on a window that isn't minimized
	public void a_window_is_never_kept_minimized_but_as_it_was_before(int showCmd, int flags, bool maximized)
	{
		PixelRect normal = new(100, 100, 1200, 780);

		SavedWindowPlacement saved = WindowPlacementRules.Capture(showCmd, flags, normal, 96);

		Assert.Equal(new SavedWindowPlacement(normal, 96, maximized), saved);
	}

	[Fact]
	public void a_window_closed_minimized_from_maximized_reopens_maximized()
	{
		SavedWindowPlacement saved = WindowPlacementRules.Capture(2, 2, new PixelRect(2100, 100, 1800, 1170), 144);

		WindowPlacementPlan plan = Restore(SavedWindowPlacement.Parse(saved.Format()), Both);

		Assert.True(plan.Maximized);
		Assert.Equal(new PixelRect(2100, 100, 1800, 1170), plan.Normal);
	}

	// ---- scaling ----

	[Fact]
	public void a_monitor_whose_scaling_changed_keeps_the_window_the_same_size_to_the_eye()
	{
		MonitorArea scaledUp = Main with { Bounds = new PixelRect(0, 0, 2560, 1440), WorkArea = new PixelRect(0, 0, 2560, 1400), Dpi = 144 };

		WindowPlacementPlan plan = Restore(Saved(100, 100, 1200, 780, dpi: 96), scaledUp);

		Assert.Equal(new PixelRect(100, 100, 1800, 1170), plan.Normal);
	}

	[Theory]
	[InlineData(96, false)] // 150 showing is more than 100 at 100%
	[InlineData(192, true)] // but less than 200, which is 100 at 200%
	public void the_title_bar_check_is_at_the_scaling_of_the_monitor_it_is_on(int dpi, bool opensAtDefault)
	{
		MonitorArea monitor = new(new PixelRect(0, 0, 3840, 2160), new PixelRect(0, 0, 3840, 2120), dpi);

		WindowPlacementPlan plan = Restore(Saved(3840 - 150, 100, 2400, 1560, dpi), monitor);

		PixelRect atDefault = WindowPlacementRules.Default(monitor, Sizes).Normal;
		Assert.Equal(opensAtDefault, plan.Normal == atDefault);
	}

	[Fact]
	public void the_minimum_size_is_at_the_scaling_of_the_monitor_it_is_on()
	{
		WindowPlacementPlan plan = Restore(Saved(2000, 100, 1000, 700, dpi: 144), Both);

		// 980 x 640 at 150%.
		Assert.Equal(new PixelRect(2000, 100, 1470, 960), plan.Normal);
	}

	// ---- workspace coordinates ----

	[Fact]
	public void workspace_coordinates_are_shifted_by_a_taskbar_on_the_left_or_top()
	{
		MonitorArea leftTaskbar = new(new PixelRect(0, 0, 1920, 1080), new PixelRect(48, 0, 1872, 1080), 96);
		MonitorArea topTaskbar = new(new PixelRect(1920, 0, 1920, 1080), new PixelRect(1920, 40, 1920, 1040), 96);
		MonitorArea[] monitors = [leftTaskbar, topTaskbar];

		Assert.Equal(new PixelRect(52, 100, 800, 600), WindowPlacementRules.ToWorkspace(new PixelRect(100, 100, 800, 600), monitors));
		Assert.Equal(new PixelRect(2000, 60, 800, 600), WindowPlacementRules.ToWorkspace(new PixelRect(2000, 100, 800, 600), monitors));
		Assert.Equal(new PixelRect(100, 100, 800, 600), WindowPlacementRules.FromWorkspace(new PixelRect(52, 100, 800, 600), monitors));
		Assert.Equal(new PixelRect(2000, 100, 800, 600), WindowPlacementRules.FromWorkspace(new PixelRect(2000, 60, 800, 600), monitors));
	}

	[Fact]
	public void workspace_coordinates_are_screen_coordinates_with_the_taskbar_at_the_bottom_or_off_every_screen()
	{
		PixelRect onMain = new(100, 100, 800, 600);
		PixelRect offScreen = new(-5000, -5000, 800, 600);

		Assert.Equal(onMain, WindowPlacementRules.ToWorkspace(onMain, Both));
		Assert.Equal(onMain, WindowPlacementRules.FromWorkspace(onMain, Both));
		Assert.Equal(offScreen, WindowPlacementRules.ToWorkspace(offScreen, Both));
		Assert.Equal(offScreen, WindowPlacementRules.FromWorkspace(offScreen, Both));
	}

	// ---- the monitor it is on, and a dialog's ----

	[Fact]
	public void a_rectangle_is_on_the_monitor_it_covers_most_of()
	{
		Assert.Same(Second, WindowPlacementRules.MonitorOf(new PixelRect(1800, 0, 400, 400), Both));
		Assert.Same(Main, WindowPlacementRules.MonitorOf(new PixelRect(1700, 0, 400, 400), Both));
		Assert.Null(WindowPlacementRules.MonitorOf(new PixelRect(-500, 0, 400, 400), Both));
	}

	[Theory]
	[InlineData(144, 1280, 0, 2560 / 1.5, 1400 / 1.5)]
	[InlineData(96, 1920, 0, 2560, 1400)]
	[InlineData(0, 1920, 0, 2560, 1400)] // unknown scaling is taken as 100%
	public void a_dialog_sizes_itself_to_its_owners_screen_in_wpf_units(int dpi, double left, double top, double width, double height)
	{
		Rect area = WindowPlacementRules.ToDips(Second.WorkArea, dpi);

		Assert.Equal(left, area.Left, 6);
		Assert.Equal(top, area.Top, 6);
		Assert.Equal(width, area.Width, 6);
		Assert.Equal(height, area.Height, 6);
	}

	[Fact]
	public void rectangles_that_only_touch_share_nothing()
	{
		Assert.Equal(0, new PixelRect(0, 0, 100, 100).OverlapArea(new PixelRect(100, 0, 100, 100)));
		Assert.Equal(2_500, new PixelRect(0, 0, 100, 100).OverlapArea(new PixelRect(50, 50, 100, 100)));
	}

	// ---- the saved value ----

	[Fact]
	public void the_saved_value_reads_back_as_it_was_written()
	{
		SavedWindowPlacement saved = Saved(-1800, -20, 1800, 1170, dpi: 144, maximized: true);

		Assert.Equal("-1800,-20,1800,1170,144,Maximized", saved.Format());
		Assert.Equal(saved, SavedWindowPlacement.Parse(saved.Format()));
		Assert.Equal(Saved(100, 50, 1200, 780), SavedWindowPlacement.Parse(" 100, 50 ,1200,780,96, Normal "));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("garbage")]
	[InlineData("100,50,1200,780,96")]
	[InlineData("100,50,1200,780,96,Normal,extra")]
	[InlineData("left,50,1200,780,96,Normal")]
	[InlineData("100.5,50,1200,780,96,Normal")]
	[InlineData("100,50,0,780,96,Normal")]
	[InlineData("100,50,1200,-780,96,Normal")]
	[InlineData("100,50,1200,780,0,Normal")]
	[InlineData("100,50,1200,780,96,Minimized")]
	[InlineData("100,50,1200,780,96,normal")]
	[InlineData("99999999999,50,1200,780,96,Normal")]
	[InlineData("-2147483648,50,1200,780,96,Normal")]
	[InlineData("1000001,50,1200,780,96,Normal")]
	public void a_missing_or_corrupt_saved_value_reads_as_none_and_means_the_default(string? text)
	{
		Assert.Null(SavedWindowPlacement.Parse(text));
		Assert.Equal(new WindowPlacementPlan(MainDefault, Maximized: false), Restore(SavedWindowPlacement.Parse(text), Both));
	}

	[Fact]
	public void restore_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => WindowPlacementRules.Restore(null, null!, Main, Sizes));
		Assert.Throws<ArgumentNullException>(() => WindowPlacementRules.Restore(null, Both, null!, Sizes));
		Assert.Throws<ArgumentNullException>(() => WindowPlacementRules.Restore(null, Both, Main, null!));
		Assert.Throws<ArgumentNullException>(() => WindowPlacementRules.Default(null!, Sizes));
		Assert.Throws<ArgumentNullException>(() => WindowPlacementRules.Default(Main, null!));
		Assert.Throws<ArgumentNullException>(() => WindowPlacementRules.MonitorOf(default, null!));
	}
}
