using System.IO;
using System.Windows;

using Microsoft.Win32;

using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Export FE-Buddy Alias Command Guide, the same from both places it is offered: Info ▸ Alias
/// Command Guide and Info ▸ What's New in v3.0?.
/// </summary>
public static class AliasGuideExport
{
	/// <summary>
	/// Asks which format the guide is wanted in (web page, Markdown or both), then which folder,
	/// and writes it there as <c>FE-Buddy Alias Command Guide.html</c> / <c>.md</c> - the user
	/// never names the files. A guide already in that folder is only replaced once the user says
	/// so, since it may hold their own edits.
	/// </summary>
	public static void Run()
	{
		Window? owner = Application.Current?.MainWindow;

		if (AliasGuideFormatWindow.Ask(owner) is not { } formats)
		{
			return;
		}

		OpenFolderDialog dialog = new()
		{
			Title = "Choose the folder to save the FE-Buddy Alias Command Guide in",
			InitialDirectory = OutputPreferences.BrowseDirectory(),
		};

		if (dialog.ShowDialog(owner) != true)
		{
			return;
		}

		string folder = dialog.FolderName;
		string[] names = [.. formats.Select(AliasGuideWriter.FileName)];
		string[] existing = [.. names.Where(name => File.Exists(Path.Combine(folder, name)))];

		if (existing.Length > 0
			&& !ConfirmWindow.Show(
				owner,
				existing.Length == 1 ? "Replace the guide already there?" : "Replace the guides already there?",
				$"{JoinCode(existing)} {(existing.Length == 1 ? "is" : "are")} already in `{folder}`. "
				+ $"Replacing {(existing.Length == 1 ? "it" : "them")} loses any changes made to {(existing.Length == 1 ? "it" : "them")}.",
				confirmText: "Replace"))
		{
			return;
		}

		try
		{
			AliasGuideWriter.Export(folder, formats, new AliasGuideOptions(AppVersion.Current, DateTime.UtcNow));
			AppLog.Info("Info", $"Exported the alias command guide to '{folder}': {string.Join(", ", names)}.");
			Toast.Success("Alias command guide exported", $"{string.Join(" and ", names)} saved to {folder}, ready to share or post on your facility's website.");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning("Info", $"Could not export the alias command guide to '{folder}': {ex.Message}");
			Toast.Error("Export failed", ex.Message);
		}
	}

	/// <summary>File names in the code look, joined: <c>`a`</c>, or <c>`a` and `b`</c>.</summary>
	private static string JoinCode(IReadOnlyList<string> names) => string.Join(" and ", names.Select(name => $"`{name}`"));
}
