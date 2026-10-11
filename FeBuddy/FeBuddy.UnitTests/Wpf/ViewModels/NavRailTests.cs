using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="ShellViewModel.RailFor"/> (issue #345): opening Map collapses the nav rail, and
/// leaving Map expands it again - unless the user set the rail themselves in between.
/// </summary>
public sealed class NavRailTests
{
	[Theory]
	// Opening Map collapses an expanded rail, and remembers it did - also when the user expanded it
	// on Map (which forgets Map's doing) and opens Map again.
	[InlineData(true, false, false, true, true)]
	// Opening Map again while it holds the rail collapsed keeps it so.
	[InlineData(true, true, true, true, true)]
	// A rail the user collapsed stays theirs: leaving Map won't expand it.
	[InlineData(true, true, false, true, false)]
	// Leaving Map expands the rail Map collapsed.
	[InlineData(false, true, true, false, false)]
	// Without Map's doing, another section leaves the rail as it is.
	[InlineData(false, true, false, true, false)]
	[InlineData(false, false, false, false, false)]
	public void map_collapses_the_rail_and_leaving_it_restores_it(
		bool openingMap, bool navCollapsed, bool collapsedForMap, bool expectedCollapsed, bool expectedForMap)
	{
		Assert.Equal((expectedCollapsed, expectedForMap), ShellViewModel.RailFor(openingMap, navCollapsed, collapsedForMap));
	}
}
