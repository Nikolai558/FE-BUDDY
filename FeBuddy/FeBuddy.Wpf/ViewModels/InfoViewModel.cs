using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// SYSTEM ▸ Info: the What's New in v3.0? and Alias Command Guide pages, and links to the manual, the
/// change log and the issue tracker. Discord is on the Dashboard instead.
/// </summary>
/// <remarks>
/// A page opens in place of the cards (<see cref="Page"/>). What's New links to the Alias Command
/// Guide; the guide's back arrow then returns to What's New rather than the cards.
/// </remarks>
public sealed class InfoViewModel : ObservableObject
{
	private object? _page;

	/// <summary>One Info card.</summary>
	/// <param name="Title">The card's title.</param>
	/// <param name="Blurb">A one-line description.</param>
	/// <param name="Open">Opens the page or link.</param>
	/// <param name="IsFeatured"><see langword="true"/> for a card set apart in amber: one of FE-Buddy's own pages.</param>
	public sealed record Resource(string Title, string Blurb, ICommand Open, bool IsFeatured = false);

	/// <summary>Creates the view-model.</summary>
	public InfoViewModel()
	{
		Resources =
		[
			new("What's New in v3.0?", "What changed since FE-Buddy 2.x, and the new chart recall commands.",
				new RelayCommand(ShowWhatsNew), IsFeatured: true),
			new("Alias Command Guide", "Every FE-Buddy alias command, explained for your controllers. Export it for your facility's website.",
				new RelayCommand(() => ShowGuide(back: ShowCards)), IsFeatured: true),
			Link("User Guide", "How each tool works, field by field.", Links.UserGuide),
			Link("FAQ / Troubleshooting", "Answers to common questions, and fixes for common problems.", Links.FaqAndTroubleshooting),
			Link("Change log", "What shipped in every release.", Links.ChangeLog),
			Link("Issues & requests", "Track bugs and feature ideas.", Links.Issues),
		];
	}

	/// <summary>The cards, in display order.</summary>
	public ObservableCollection<Resource> Resources { get; }

	/// <summary>
	/// The page open in place of the cards - a <see cref="WhatsNewViewModel"/> or an
	/// <see cref="AliasGuideViewModel"/> - or <see langword="null"/> on the cards.
	/// </summary>
	public object? Page
	{
		get => _page;
		private set => SetProperty(ref _page, value);
	}

	private void ShowCards() => Page = null;

	private void ShowWhatsNew() => Page = new WhatsNewViewModel(back: ShowCards, openGuide: () => ShowGuide(back: ShowWhatsNew));

	private void ShowGuide(Action back) => Page = new AliasGuideViewModel(back);

	/// <summary>A card that opens a web page in the browser.</summary>
	private static Resource Link(string title, string blurb, string url) =>
		new(title, blurb, new RelayCommand(() => BrowserLauncher.Open(url)));
}
