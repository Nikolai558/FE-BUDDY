using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// SYSTEM ▸ Info: the What's New in v3.0? page, and links to the manual, the change log and the
/// issue tracker. Discord is on the Dashboard instead.
/// </summary>
public sealed class InfoViewModel : ObservableObject
{
	private WhatsNewViewModel? _whatsNew;

	/// <summary>One Info card.</summary>
	/// <param name="Title">The card's title.</param>
	/// <param name="Blurb">A one-line description.</param>
	/// <param name="Open">Opens the page or link.</param>
	/// <param name="IsFeatured"><see langword="true"/> for a card set apart in amber: What's New.</param>
	public sealed record Resource(string Title, string Blurb, ICommand Open, bool IsFeatured = false);

	/// <summary>Creates the view-model.</summary>
	public InfoViewModel()
	{
		Resources =
		[
			new("What's New in v3.0?", "What changed since FE-Buddy 2.x, and a guide to every alias command to share with your controllers.",
				new RelayCommand(() => WhatsNew = new WhatsNewViewModel(() => WhatsNew = null)), IsFeatured: true),
			Link("User Guide", "How each tool works, field by field.", Links.UserGuide),
			Link("FAQ / Troubleshooting", "Answers to common questions, and fixes for common problems.", Links.FaqAndTroubleshooting),
			Link("Change log", "What shipped in every release.", Links.ChangeLog),
			Link("Issues & requests", "Track bugs and feature ideas.", Links.Issues),
		];
	}

	/// <summary>The cards, in display order.</summary>
	public ObservableCollection<Resource> Resources { get; }

	/// <summary>The What's New page while it is open in place of the cards; <see langword="null"/> on the cards.</summary>
	public WhatsNewViewModel? WhatsNew
	{
		get => _whatsNew;
		private set => SetProperty(ref _whatsNew, value);
	}

	/// <summary>A card that opens a web page in the browser.</summary>
	private static Resource Link(string title, string blurb, string url) =>
		new(title, blurb, new RelayCommand(() => BrowserLauncher.Open(url)));
}
