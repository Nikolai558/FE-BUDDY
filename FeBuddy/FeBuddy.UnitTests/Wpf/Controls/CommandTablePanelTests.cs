using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using FeBuddy.Wpf.Controls;

namespace FeBuddy.UnitTests.Wpf.Controls;

/// <summary>
/// Covers <see cref="CommandTablePanel"/>: columns when the middle one keeps its minimum - the first
/// and last as wide as their widest cell, the middle the rest, a rule between rows - and each row
/// stacked, its headings hidden, when it would not. Each test runs on a WPF thread of its own.
/// </summary>
public sealed class CommandTablePanelTests
{
	/// <summary>
	/// Headings, then two rows. The first column is at most 100 wide and the last 150, so with 24
	/// between columns and 240 for the middle, columns need 538.
	/// </summary>
	private static (CommandTablePanel Panel, Border[] Cells) Table(double secondDescriptionHeight = 20)
	{
		Border[] cells =
		[
			new() { Width = 60, Height = 16 }, new() { Height = 16 }, new() { Width = 50, Height = 16 },
			new() { Width = 100, Height = 40 }, new() { Height = secondDescriptionHeight }, new() { Width = 150, Height = 30 },
			new() { Width = 80, Height = 20 }, new() { Height = 50 }, new() { Width = 120, Height = 60 },
		];

		CommandTablePanel panel = new() { MiddleMinWidth = 240, ColumnSpacing = 24, RowPadding = 12, StackSpacing = 8 };

		foreach (Border cell in cells)
		{
			panel.Children.Add(cell);
		}

		return (panel, cells);
	}

	private static void Layout(CommandTablePanel panel, double width)
	{
		panel.Measure(new Size(width, double.PositiveInfinity));
		panel.Arrange(new Rect(0, 0, width, panel.DesiredSize.Height));
	}

	private static Rect Slot(UIElement cell) => LayoutInformation.GetLayoutSlot((FrameworkElement)cell);

	[Fact]
	public void with_room_the_cells_are_columns_the_outer_ones_as_wide_as_their_widest_cell() =>
		StaThread.Run(() =>
		{
			(CommandTablePanel panel, Border[] cells) = Table();

			Layout(panel, 800);

			// Rows of 16, 40 and 60, each pair 12 + 1 + 12 apart.
			Assert.True(panel.IsColumns);
			Assert.Equal(new Size(800, 166), panel.DesiredSize);
			Assert.Equal(new Rect(0, 0, 100, 16), Slot(cells[0]));
			Assert.Equal(new Rect(0, 41, 100, 40), Slot(cells[3]));
			Assert.Equal(new Rect(124, 41, 502, 40), Slot(cells[4]));
			Assert.Equal(new Rect(650, 41, 150, 40), Slot(cells[5]));
			Assert.Equal(new Rect(650, 106, 150, 60), Slot(cells[8]));
			Assert.Equal([28.0, 93.0], panel.Rules);
		});

	[Fact]
	public void exactly_enough_room_for_the_middle_is_room() =>
		StaThread.Run(() =>
		{
			(CommandTablePanel panel, _) = Table();

			Layout(panel, 538);

			Assert.True(panel.IsColumns);
		});

	[Theory]
	[InlineData(537)]
	[InlineData(double.PositiveInfinity)]
	public void without_room_each_row_stacks_and_the_headings_are_hidden(double width) =>
		StaThread.Run(() =>
		{
			(CommandTablePanel panel, Border[] cells) = Table();

			panel.Measure(new Size(width, double.PositiveInfinity));
			panel.Arrange(new Rect(0, 0, 537, panel.DesiredSize.Height));

			// 40 + 8 + 20 + 8 + 30, a rule 12 below it, then 20 + 8 + 50 + 8 + 60 another 12 below that.
			Assert.False(panel.IsColumns);
			Assert.Equal(277, panel.DesiredSize.Height);
			Assert.Equal(double.IsInfinity(width) ? 150 : width, panel.DesiredSize.Width);
			Assert.All(cells[..3], heading => Assert.Equal(new Rect(), Slot(heading)));
			Assert.Equal(new Rect(0, 0, 537, 40), Slot(cells[3]));
			Assert.Equal(new Rect(0, 48, 537, 20), Slot(cells[4]));
			Assert.Equal(new Rect(0, 76, 537, 30), Slot(cells[5]));
			Assert.Equal(new Rect(0, 131, 537, 20), Slot(cells[6]));
			Assert.Equal(new Rect(0, 217, 537, 60), Slot(cells[8]));
			Assert.Equal([118.0], panel.Rules);
		});

	[Fact]
	public void a_stacked_cell_with_nothing_to_show_takes_no_room_and_no_gap() =>
		StaThread.Run(() =>
		{
			(CommandTablePanel panel, Border[] cells) = Table(secondDescriptionHeight: 0);

			Layout(panel, 400);

			Assert.Equal(new Rect(0, 48, 400, 30), Slot(cells[5]));
			Assert.Equal(277 - 28, panel.DesiredSize.Height);
		});

	[Fact]
	public void children_past_the_last_whole_row_are_not_laid_out() =>
		StaThread.Run(() =>
		{
			(CommandTablePanel panel, _) = Table();
			Border extra = new() { Width = 50, Height = 50 };
			panel.Children.Add(extra);

			Layout(panel, 800);

			Assert.Equal(166, panel.DesiredSize.Height);
			Assert.Equal(new Rect(), Slot(extra));
		});
}
