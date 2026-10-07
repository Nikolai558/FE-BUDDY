using System.Windows.Media;

using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the map's file rows (<see cref="MapFileItem"/>) for the Your Files card (issue #312): the
/// Show all files box's state, and the right-click "Deselect all files except this one", which only
/// rows that offer it have.
/// </summary>
public sealed class MapFileItemTests
{
	private static MapFileItem File(string name, Action<MapFileItem>? showOnly = null) =>
		new(name, @"C:\maps\" + name, Brushes.Red, () => { }, _ => { }, _ => { }, showOnly);

	[Fact]
	public void show_all_is_ticked_with_every_file_shown_clear_with_none_and_filled_with_some()
	{
		MapFileItem a = File("A.geojson");
		MapFileItem b = File("B.geojson");

		Assert.True(MapFileItem.AllShown([a, b]));
		Assert.True(MapFileItem.AllShown([]));

		b.IsVisible = false;
		Assert.Null(MapFileItem.AllShown([a, b]));

		a.IsVisible = false;
		Assert.False(MapFileItem.AllShown([a, b]));
	}

	[Fact]
	public void deselect_all_but_this_one_is_offered_only_where_the_list_handles_it()
	{
		MapFileItem? shown = null;
		MapFileItem offered = File("A.geojson", item => shown = item);

		Assert.Null(File("Output.geojson").ShowOnlyCommand);

		offered.ShowOnlyCommand!.Execute(null);

		Assert.Same(offered, shown);
	}
}
