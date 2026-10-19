using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FeBuddy.Wpf.Controls;

namespace FeBuddy.UnitTests.Wpf.Controls;

/// <summary>
/// Covers <see cref="KeptContentHost"/>: each view model's view is built the first time it is shown and
/// kept, only the selected one is visible, and a view whose view model has gone is dropped at the next
/// switch. Each test runs on a WPF thread of its own.
/// </summary>
public sealed class KeptContentHostTests
{
	/// <summary>A tab shown again gets the view it had, not a new one; nothing is built before it is shown.</summary>
	[Fact]
	public void a_view_is_built_once_and_kept() => StaThread.Run(() =>
	{
		string first = "first";
		string second = "second";
		Style style = new(typeof(ContentPresenter));
		KeptContentHost host = new() { Items = new[] { first, second }, PresenterStyle = style };

		Assert.Empty(host.Children);

		host.Selected = first;
		ContentPresenter firstView = Assert.IsType<ContentPresenter>(Assert.Single(host.Children));
		Assert.Same(first, firstView.Content);
		Assert.Same(first, firstView.DataContext);
		Assert.Same(style, firstView.Style);

		host.Selected = second;
		host.Selected = first;

		Assert.Equal(2, host.Children.Count);
		Assert.Same(firstView, host.Children[0]);
		Assert.Equal(Visibility.Visible, firstView.Visibility);
		Assert.Equal(Visibility.Collapsed, host.Children[1].Visibility);

		host.Selected = null;
		Assert.All(host.Children.Cast<UIElement>(), view => Assert.Equal(Visibility.Collapsed, view.Visibility));
	});

	/// <summary>A tab that leaves the rail takes its view with it, at the next switch.</summary>
	[Fact]
	public void the_view_of_a_tab_that_has_gone_is_dropped() => StaThread.Run(() =>
	{
		ObservableCollection<string> tabs = ["first", "second", "third"];
		KeptContentHost host = new() { Items = tabs, Selected = "first" };
		host.Selected = "second";

		tabs.Remove("first");
		host.Selected = "third";

		Assert.Equal(["second", "third"], host.Children.Cast<ContentPresenter>().Select(view => view.Content));

		host.Items = null;
		host.Selected = "fourth";
		Assert.Equal(3, host.Children.Count);
	});
}
