using System.IO;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Info ▸ What's New in v3.0?: what FE-Buddy 3.0 changed from 2.x, and the button that exports the
/// FE-Buddy Alias Command Guide (<see cref="AliasGuideWriter"/>) for a facility's website.
/// </summary>
/// <remarks>
/// The content is <c>FE-Buddy_3.0_Whats_New.md</c>, at the repository's root, laid out as a page;
/// change the two together. Text uses inline Markdown (<c>**bold**</c>, <c>*italic*</c>,
/// <c>`code`</c>) for <c>InlineMarkdown</c> to show.
/// </remarks>
public sealed class WhatsNewViewModel : ObservableObject
{
	/// <summary>One row of the At a Glance table.</summary>
	/// <param name="Subject">What is compared.</param>
	/// <param name="Before">How FE-Buddy 2.x did it.</param>
	/// <param name="After">How FE-Buddy 3.0 does it.</param>
	public sealed record GlanceRow(string Subject, string Before, string After);

	/// <summary>A titled point: one of the engineers' highlights or map improvements.</summary>
	/// <param name="Title">The point's name.</param>
	/// <param name="Text">What it means.</param>
	public sealed record Point(string Title, string Text);

	/// <summary>One command that changed, from its 2.x form to its 3.0 form.</summary>
	/// <param name="Procedure">The chart it opens, or empty when the rule says it all.</param>
	/// <param name="Old">The 2.x command.</param>
	/// <param name="New">The 3.0 command.</param>
	public sealed record CommandChange(string Procedure, string Old, string New);

	/// <summary>One change to how chart recall commands are written.</summary>
	/// <param name="Title">The kind of chart it changes.</param>
	/// <param name="Text">What changed.</param>
	/// <param name="Changes">Before-and-after examples.</param>
	public sealed record RecallRule(string Title, string Text, IReadOnlyList<CommandChange> Changes);

	/// <summary>An approach type and its code.</summary>
	/// <param name="Type">The approach type, e.g. <c>ILS</c>.</param>
	/// <param name="Code">Its code, e.g. <c>I</c>.</param>
	public sealed record TypeCode(string Type, string Code);

	/// <summary>One example of a new chart recall command.</summary>
	/// <param name="Airport">The airport's FAA ID.</param>
	/// <param name="Procedure">The chart's name.</param>
	/// <param name="Commands">The command, or each of them joined by "or", in backticks.</param>
	/// <param name="Note">Why it is written that way, or <see langword="null"/>.</param>
	public sealed record CommandExample(string Airport, string Procedure, string Commands, string? Note);

	/// <summary>Creates the page.</summary>
	/// <param name="back">Returns to Info's links.</param>
	public WhatsNewViewModel(Action back)
	{
		BackCommand = new RelayCommand(back);
		ExportGuideCommand = new RelayCommand(ExportGuide);
	}

	/// <summary>Returns to Info's links.</summary>
	public ICommand BackCommand { get; }

	/// <summary>Asks what format to save the alias command guide in, then which folder, then writes it.</summary>
	public ICommand ExportGuideCommand { get; }

	/// <summary>FE-Buddy 2.x against 3.0, subject by subject.</summary>
	public IReadOnlyList<GlanceRow> Glance { get; } =
	[
		new("Graphical User Interface (GUI)", "Looks like the 1900s", "Major modern facelift"),
		new("Output files include...", "Everything; the FE has no control", "Only what **you** pick, with more control over format and filters"),
		new("Output files include...", "VRC, vSTARS and vERAM files", "Only CRC files, or those that may actually be useful"),
		new("Map coverage", "Whole country", "**Your** Region of Interest"),
		new("vNAS upload preparation", "Assembled by hand or with external scripts", "Output to an `Upload_to_vNAS` folder with **your** own CRC ERAM defaults, file names, etc."),
		new("Visualizing maps", "Outside tools, and only after the output is complete", "Built-in map that can visualize **before** you run the AIRAC Service"),
		new("Sharing your preferences", "Uh... what preferences?!", "Export/import your preferences and settings"),
		new("Merging FEB alias commands with your custom commands", "Not available", "Not only available, but it can reach out to your GitHub repo for the current file(s), even a private repo (using your GitHub token)"),
		new("Duplicate commands", "Many", "Almost none"),
		new("IAP type syntax", "Many variables to mentally parse", "8 types, each with a `*/DME` and a `* BC` (back course) variant"),
		new("DP/STAR/IAP syntax with no computer code", "A mix of the first letter of each word in the base name, or its first 5 characters", "Simply writes out the full base name"),
		new("d-TPP procedures (changes)", "Limited output options; not context-aware", "Monitor changes for chosen facilities, approaches, airports, or any combination of those"),
		new("d-TPP procedures (file types)", "Simple `.txt` files", "Procedures: `.json`; changes: `.md`"),
		new("ISR alias commands", "Limited data; no handling for duplicates", "Significantly more information, handles duplicates in the data, and makes better use of CRC's `\\s` and `\\t` formatting"),
	];

	/// <summary>What a facility engineer gets.</summary>
	public IReadOnlyList<Point> ForEngineers { get; } =
	[
		new("Pick what you need", "11 sub-services, each with its own options."),
		new("Region of Interest", "Draw one box and your maps stay in your area. Commands like `.apt`, `.nav` and chart recalls still cover the whole FAA."),
		new("vNAS-ready", "Mark files for upload. GeoJSON can carry your CRC ERAM defaults; alias files merge with your facility's own (local or private GitHub) into one `vNAS_Alias.txt`. Your commands win."),
		new("Procedure changes", "`Procedure_Changes.md` lists New, Changed and Deleted charts with links."),
		new("Set once", "Settings are remembered. Preview shows what will happen; Review shows what did. Share your settings with others."),
	];

	/// <summary>How the maps improved.</summary>
	public IReadOnlyList<Point> BetterMaps { get; } =
	[
		new("Airways", "One symbol and label per point, split High/Low or by designation, adjustable waypoint buffer."),
		new("SIDs/STARs", "Lines, fix symbols and labels per airport. Shared segments drawn once."),
		new("Runways", "More of them, water runways included."),
		new("NAVAIDs", "Every FAA type, one file or one per type."),
		new("Fixes and ARTCC Boundaries", "Split by use, chart, altitude or ARTCC."),
	];

	/// <summary>How the alias commands improved.</summary>
	public IReadOnlyList<string> BetterAliases { get; } =
	[
		"`.apt`, `.nav` and `.id` show clean, labeled cards. Airports add longest runway, pattern altitude, airspace and its hours, FSS, CTAF, attended hours and more.",
		"A NAVAID or airline identifier shared by several is one command listing every match.",
		"Telephony adds U.S. special call signs (`.idNASA`).",
		"FE-Buddy can pick up your own custom alias file(s) stored on your PC and combine them with its own into a single, ready-for-vNAS-upload file. It can even reach out to your private GitHub repo for the most current version.",
	];

	/// <summary>How chart recall commands changed, rule by rule.</summary>
	public IReadOnlyList<RecallRule> RecallRules { get; } =
	[
		new("Multi-page charts", "The page number (2 or higher) now goes after the trailing `c`.",
		[
			new(string.Empty, ".dtwCLVIN2c", ".dtwCLVINc2"),
		]),
		new("Procedures without a computer code", "Visual approaches, departures and arrivals now use the full base name, dropping \"VISUAL\", \"RWY\", \"(OBSTACLE)\", spaces, special characters, etc.",
		[
			new("SOUTH RIVER VISUAL RWY 19 (fictional AAA airport)", ".aaavSOUTH19c", ".aaavSOUTHRIVER19c"),
			new("TURNAGAIN EIGHT (ANC)", ".ancTURNAc", ".ancTURNAGAINc"),
		]),
		new("Instrument approaches", "Runway numbers are no longer shortened.",
		[
			new("ILS RWY 16R", "I6R", "I16R"),
		]),
	];

	/// <summary>The eight base approach types.</summary>
	public IReadOnlyList<TypeCode> BaseTypes { get; } =
	[
		new("RNAV", "R"),
		new("ILS", "I"),
		new("LOC", "L"),
		new("VOR", "O"),
		new("NDB", "N"),
		new("LDA", "D"),
		new("GPS", "G"),
		new("TACAN", "T"),
	];

	/// <summary>The <c>*/DME</c> approach types.</summary>
	public IReadOnlyList<TypeCode> DmeTypes { get; } =
	[
		new("LOC/DME", "LD"),
		new("VOR/DME", "OD"),
		new("NDB/DME", "ND"),
		new("LDA/DME", "DD"),
	];

	/// <summary>Real charts and their new commands.</summary>
	public IReadOnlyList<CommandExample> Examples { get; } =
	[
		new("DTW", "ILS Z OR LOC RWY 04L", "`.dtwIZ04Lc` or `.dtwLZ04Lc`", "The `Z` variant carries over to the LOC approach, even when the FAA doesn't label it that way."),
		new("LAX", "RNAV (RNP) Z RWY 07R", "`.laxRZ07Rc`", "Whether it says (RNP) or (GPS) doesn't matter; it is still an RNAV approach, so its IAP type code is `R`."),
		new("LAX", "RNAV (RNP) Z RWY 24L", "`.laxRZ24Lc`", null),
		new("LAX", "RNAV (GPS) Y RWY 07R", "`.laxRY07Rc`", null),
		new("LAX", "RNAV (GPS) Y RWY 24L", "`.laxRY24Lc`", null),
		new("MRY", "RACEWAY *charted* VISUAL RWY 28L", "`.mryvRACEWAY28Lc`", "`RACEWAY` is spelled out in full."),
		new("LGB", "LA RIVER *charted* VISUAL RWY 12", "`.lgbvLARIVER12c`", "`LA RIVER` is spelled out in full, with spaces (and any special characters) removed."),
		new("SFO", "QUIET BRIDGE *charted* VISUAL RWY 28R", "`.sfovQUIETBRIDGE28Rc`", null),
	];

	/// <summary>
	/// Asks which format the guide is wanted in (web page, Markdown or both), then which folder,
	/// and writes it there as <c>FE-Buddy Alias Command Guide.html</c> / <c>.md</c> - the user
	/// never names the files. A guide already in that folder is only replaced once the user says
	/// so, since it may hold their own edits. The guide names the user's facility when Settings ▸
	/// Facility Profile has one.
	/// </summary>
	private void ExportGuide()
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

		string? facility = UserConfigFile.GetValue(SettingsViewModel.ArtccKey);

		try
		{
			AliasGuideWriter.Export(folder, formats, new AliasGuideOptions(facility, AppVersion.Current, DateTime.UtcNow));
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
