using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// Shows the <see cref="Selected"/> view model through its DataTemplate, like a ContentControl, but
/// keeps the view it built for each one: switching back to a tab shows its view again rather than
/// building its whole card tree anew. A view is built the first time its view model is shown, so
/// nothing is built up front.
/// </summary>
/// <remarks>
/// <para>
/// <c>&lt;ctl:KeptContentHost Selected="{Binding SelectedTab}" Items="{Binding Tabs}" /&gt;</c>.
/// The views not shown are collapsed, so they take no room and no input; their bindings stay live.
/// A view whose view model has left <see cref="Items"/> is dropped at the next switch.
/// </para>
/// <para>
/// Each view's presenter has its view model as its DataContext and takes <see cref="PresenterStyle"/>,
/// for what a page sets once for everything on it, e.g. <c>ctl:Card.OutputsOn</c>.
/// </para>
/// </remarks>
public sealed class KeptContentHost : Grid
{
	/// <summary>Identifies the <see cref="Selected"/> dependency property.</summary>
	public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
		nameof(Selected), typeof(object), typeof(KeptContentHost), new PropertyMetadata(null, (d, _) => ((KeptContentHost)d).ShowSelected()));

	/// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
	public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
		nameof(Items), typeof(IEnumerable), typeof(KeptContentHost), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="PresenterStyle"/> dependency property.</summary>
	public static readonly DependencyProperty PresenterStyleProperty = DependencyProperty.Register(
		nameof(PresenterStyle), typeof(Style), typeof(KeptContentHost), new PropertyMetadata(null));

	private readonly Dictionary<object, ContentPresenter> _views = new(ReferenceEqualityComparer.Instance);

	/// <summary>The view model to show, e.g. the selected tab; <see langword="null"/> shows nothing.</summary>
	public object? Selected
	{
		get => GetValue(SelectedProperty);
		set => SetValue(SelectedProperty, value);
	}

	/// <summary>Every view model that can be shown, e.g. the tabs; a kept view whose view model isn't among them is dropped.</summary>
	public IEnumerable? Items
	{
		get => (IEnumerable?)GetValue(ItemsProperty);
		set => SetValue(ItemsProperty, value);
	}

	/// <summary>The style each view's presenter takes, its view model as its DataContext.</summary>
	public Style? PresenterStyle
	{
		get => (Style?)GetValue(PresenterStyleProperty);
		set => SetValue(PresenterStyleProperty, value);
	}

	private void ShowSelected()
	{
		object? selected = Selected;

		DropGone(selected);

		if (selected is not null && !_views.ContainsKey(selected))
		{
			ContentPresenter view = new() { Content = selected, DataContext = selected, Style = PresenterStyle };
			_views[selected] = view;
			Children.Add(view);
		}

		foreach ((object item, ContentPresenter view) in _views)
		{
			view.Visibility = ReferenceEquals(item, selected) ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	/// <summary>Drops the views of view models no longer in <see cref="Items"/>, all but the one being shown.</summary>
	private void DropGone(object? selected)
	{
		if (Items is not { } items)
		{
			return;
		}

		HashSet<object> current = new(items.Cast<object>(), ReferenceEqualityComparer.Instance);

		foreach (object gone in _views.Keys.Where(item => !current.Contains(item) && !ReferenceEquals(item, selected)).ToList())
		{
			Children.Remove(_views[gone]);
			_views.Remove(gone);
		}
	}
}
