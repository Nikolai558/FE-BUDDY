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
/// The two exports on Info ▸ Alias Command Guide: the guide itself (<see cref="Run"/>) and the
/// Alias Command Practice page (<see cref="RunPractice"/>).
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

		string[] names = [.. formats.Select(AliasGuideWriter.FileName)];

		if (ChooseFolder(owner, "Choose the folder to save the FE-Buddy Alias Command Guide in", names) is not { } folder)
		{
			return;
		}

		Write("alias command guide", folder, names, () => AliasGuideWriter.Export(folder, formats, Options()));
	}

	/// <summary>
	/// Asks which folder to save the Alias Command Practice page in, and writes it there as
	/// <c>FE-Buddy Alias Command Practice.html</c>, replacing one already there once the user says so.
	/// </summary>
	public static void RunPractice()
	{
		Window? owner = Application.Current?.MainWindow;
		string[] names = [AliasPracticeWriter.FileName];

		if (ChooseFolder(owner, "Choose the folder to save the FE-Buddy Alias Command Practice in", names) is not { } folder)
		{
			return;
		}

		Write("alias command practice", folder, names, () => AliasPracticeWriter.Export(folder, Options()));
	}

	private static AliasGuideOptions Options() => new(AppVersion.Current, DateTime.UtcNow);

	/// <summary>
	/// The folder the user picks, once they have agreed to replace any of <paramref name="names"/>
	/// already in it; <see langword="null"/> when they cancel either.
	/// </summary>
	private static string? ChooseFolder(Window? owner, string title, IReadOnlyList<string> names)
	{
		OpenFolderDialog dialog = new()
		{
			Title = title,
			InitialDirectory = OutputPreferences.BrowseDirectory(),
		};

		if (dialog.ShowDialog(owner) != true)
		{
			return null;
		}

		string folder = dialog.FolderName;
		string[] existing = [.. names.Where(name => File.Exists(Path.Combine(folder, name)))];
		bool one = existing.Length == 1;

		if (existing.Length > 0
			&& !ConfirmWindow.Show(
				owner,
				one ? "Replace the file already there?" : "Replace the files already there?",
				$"{JoinCode(existing)} {(one ? "is" : "are")} already in `{folder}`. "
				+ $"Replacing {(one ? "it" : "them")} loses any changes made to {(one ? "it" : "them")}.",
				confirmText: "Replace"))
		{
			return null;
		}

		return folder;
	}

	/// <summary>Writes the files, then says where they went - or why they could not be written.</summary>
	private static void Write(string what, string folder, IReadOnlyList<string> names, Action write)
	{
		try
		{
			write();
			AppLog.Info("Info", $"Exported the {what} to '{folder}': {string.Join(", ", names)}.");
			Toast.Success($"{char.ToUpperInvariant(what[0])}{what[1..]} exported", $"{string.Join(" and ", names)} saved to {folder}, ready to share or post on your facility's website.");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning("Info", $"Could not export the {what} to '{folder}': {ex.Message}");
			Toast.Error("Export failed", ex.Message);
		}
	}

	/// <summary>File names in the code look, joined: <c>`a`</c>, or <c>`a` and `b`</c>.</summary>
	private static string JoinCode(IReadOnlyList<string> names) => string.Join(" and ", names.Select(name => $"`{name}`"));
}
