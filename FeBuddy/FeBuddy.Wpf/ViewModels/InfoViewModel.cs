using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// SYSTEM ▸ Info: links to the manual, the change log and the issue tracker. Discord is on the
/// Dashboard instead.
/// </summary>
public sealed class InfoViewModel : ObservableObject
{
	/// <summary>One Info resource row.</summary>
	/// <param name="Title">The link title.</param>
	/// <param name="Blurb">A one-line description.</param>
	/// <param name="Url">The link target.</param>
	public sealed record Resource(string Title, string Blurb, string Url);

	/// <summary>Creates the view-model.</summary>
	public InfoViewModel()
	{
		OpenCommand = new RelayCommand<string>(BrowserLauncher.Open);
	}

	/// <summary>Opens a link in the browser. The command parameter is the URL.</summary>
	public ICommand OpenCommand { get; }

	/// <summary>The links, in display order.</summary>
	public ObservableCollection<Resource> Resources { get; } =
	[
		new("Manual", "How each tool works, field by field.", Links.Manual),
		new("Change log", "What shipped in every release.", Links.ChangeLog),
		new("Issues & requests", "Track bugs and feature ideas.", Links.Issues),
	];
}
