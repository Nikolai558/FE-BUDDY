using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using FeBuddy.Wpf.Controls;

namespace FeBuddy.UnitTests.Wpf.Controls;

/// <summary>
/// Covers <see cref="BesideOrBelow"/>: the second child beside the first when the row has room for
/// both (the first's minimum, the gap and the side's width), under it when not, and the first alone
/// across the row when the second has nothing to show. Each test runs on a WPF thread of its own.
/// </summary>
public sealed class BesideOrBelowTests
{
	/// <summary>The File Names row's sizes: 300 for the file, 16 apart, 440 for the box - 756 in all.</summary>
	private static (BesideOrBelow Panel, Border Main, Border Side) Row(double sideHeight = 34)
	{
		Border main = new() { Height = 20 };
		Border side = new() { Height = sideHeight };
		BesideOrBelow panel = new() { SideWidth = 440, MainMinWidth = 300, Children = { main, side } };
		return (panel, main, side);
	}

	private static void Layout(BesideOrBelow panel, double width)
	{
		panel.Measure(new Size(width, double.PositiveInfinity));
		panel.Arrange(new Rect(0, 0, width, panel.DesiredSize.Height));
	}

	[Fact]
	public void with_room_for_both_the_second_sits_against_the_right_edge() =>
		StaThread.Run(() =>
		{
			(BesideOrBelow panel, Border main, Border side) = Row();

			Layout(panel, 800);

			Assert.True(panel.IsBeside);
			Assert.Equal(34, panel.DesiredSize.Height);
			Assert.Equal(new Rect(0, 0, 344, 20), LayoutInformation.GetLayoutSlot(main));
			Assert.Equal(new Rect(360, 0, 440, 34), LayoutInformation.GetLayoutSlot(side));
		});

	[Theory]
	[InlineData(755)]
	[InlineData(double.PositiveInfinity)]
	public void without_room_the_second_goes_under_the_first(double width) =>
		StaThread.Run(() =>
		{
			(BesideOrBelow panel, Border main, Border side) = Row();

			panel.Measure(new Size(width, double.PositiveInfinity));
			panel.Arrange(new Rect(0, 0, 755, panel.DesiredSize.Height));

			Assert.False(panel.IsBeside);
			Assert.Equal(20 + 6 + 34, panel.DesiredSize.Height);
			Assert.Equal(new Rect(0, 0, 755, 20), LayoutInformation.GetLayoutSlot(main));
			Assert.Equal(new Rect(0, 26, 755, 34), LayoutInformation.GetLayoutSlot(side));
		});

	[Fact]
	public void exactly_enough_room_is_room() =>
		StaThread.Run(() =>
		{
			(BesideOrBelow panel, _, _) = Row();

			Layout(panel, 756);

			Assert.True(panel.IsBeside);
		});

	/// <summary>A file that can't be renamed has an empty box column: its caption gets the whole row.</summary>
	[Fact]
	public void a_second_child_with_nothing_to_show_leaves_the_first_the_whole_row() =>
		StaThread.Run(() =>
		{
			(BesideOrBelow empty, Border emptyMain, _) = Row(sideHeight: 0);
			(BesideOrBelow collapsed, Border collapsedMain, Border collapsedSide) = Row();
			collapsedSide.Visibility = Visibility.Collapsed;

			Layout(empty, 800);
			Layout(collapsed, 800);

			Assert.False(empty.IsBeside);
			Assert.Equal(20, empty.DesiredSize.Height);
			Assert.Equal(new Rect(0, 0, 800, 20), LayoutInformation.GetLayoutSlot(emptyMain));
			Assert.False(collapsed.IsBeside);
			Assert.Equal(20, collapsed.DesiredSize.Height);
			Assert.Equal(new Rect(0, 0, 800, 20), LayoutInformation.GetLayoutSlot(collapsedMain));
		});

	[Fact]
	public void a_third_child_is_not_laid_out() =>
		StaThread.Run(() =>
		{
			(BesideOrBelow panel, _, _) = Row();
			Border extra = new() { Width = 50, Height = 50 };
			panel.Children.Add(extra);

			Layout(panel, 800);

			Assert.Equal(34, panel.DesiredSize.Height);
			Assert.Equal(new Rect(0, 0, 0, 0), LayoutInformation.GetLayoutSlot(extra));
		});
}
