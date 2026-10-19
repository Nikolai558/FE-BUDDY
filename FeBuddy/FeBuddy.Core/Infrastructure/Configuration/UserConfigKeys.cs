namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Settings keys used outside the one screen that edits them, read in more than one place, or saved outside a
/// sub-service tab's node, named once here. <c>UserConfigLayoutTests</c> reads every one by name. Each
/// sub-service's own keys belong to the screen that edits them.
/// </summary>
public static class UserConfigKeys
{
	/// <summary>The lowest release channel the user accepts updates from: <c>Stable</c>, <c>ReleaseCandidate</c>, <c>Beta</c> or <c>Alpha</c>.</summary>
	public const string UpdateChannel = "General.UpdateChannel";

	/// <summary>The id of the newest News post the user has seen, e.g. <c>2026-08-30.3</c>.</summary>
	public const string NewsLastOpen = "General.NewsLastOpen";

	/// <summary>Whether GeoJSON is written indented (<c>Y</c>) or on one line (<c>N</c>, the default).</summary>
	public const string PrettyPrintGeojson = "General.PrettyPrintGeojson";

	/// <summary>The output folder new runs start with.</summary>
	public const string DefaultOutputDirectory = "General.DefaultOutputDirectory";

	/// <summary>Whether output goes in a <c>FE-Buddy_Output</c> folder inside the output folder (<c>Y</c>/<c>N</c>).</summary>
	public const string AddFeBuddyOutputFolder = "General.AddFeBuddyOutputFolder";

	/// <summary>
	/// The GitHub token credential FE-Buddy's own GitHub requests (update checks, News, update
	/// downloads) are sent with, by id; unset or blank sends them without one. Kept on this PC: a
	/// credential id means nothing on another.
	/// </summary>
	public const string FeBuddyGitHubCredentialId = "General.FeBuddyGitHub.CredentialId";

	/// <summary>
	/// <c>Y</c> once FE-Buddy has told the user that FE-Buddy 2.x's <c>FEBUDDY_GITHUB_TOKEN</c>
	/// environment variable is still set on this PC, so it never says so again. Kept on this PC: it is
	/// about this PC's variable.
	/// </summary>
	public const string LegacyGitHubTokenNoticeShown = "General.LegacyGitHubTokenNoticeShown";

	/// <summary>
	/// <c>Y</c> once FE-Buddy has shown its one-time welcome to FE-Buddy 3, so it never shows again.
	/// Kept on this PC: it is about this PC's first launch.
	/// </summary>
	public const string WelcomeShown = "General.WelcomeShown";

	/// <summary>
	/// Whether FE-Buddy's main window reopens where it was closed (<c>Y</c>, the default) or at its
	/// default size in the middle of the main screen (<c>N</c>). Kept on this PC: it goes with
	/// <see cref="MainWindowPlacement"/>.
	/// </summary>
	public const string RememberWindowPlacement = "General.RememberWindowPlacement";

	/// <summary>
	/// Where FE-Buddy's main window was when it last closed, in Windows' physical screen pixels:
	/// <c>left,top,width,height,dpi,state</c>, e.g. <c>2120,140,1800,1170,144,Maximized</c> (see
	/// <c>SavedWindowPlacement</c>). Kept on this PC: it is about this PC's screens.
	/// </summary>
	public const string MainWindowPlacement = "General.MainWindowPlacement";

	/// <summary>How many decimal places GeoJSON coordinates are rounded to, 0 to 15 (6 when unset). Settings ▸ GeoJSON Files.</summary>
	public const string CoordinatePrecision = "General.CoordinatePrecision";

	/// <summary>
	/// The facility from Settings ▸ Facility, an ARTCC ID such as <c>ZOB</c>: the run's <c>PrimaryFacility</c>, and the
	/// ARTCC ticked on the tabs with an area until each is first saved. Read by Settings, the AIRAC Service, Procedure
	/// Charts, the area rules (<see cref="UserConfigAreas.FacilityKey"/>) and a settings import's profile name.
	/// </summary>
	public const string Facility = "General.Facility";

	/// <summary>Whether the default ROI (Settings and the Map) is on: <c>Y</c> or <c>N</c>. Its corners sit beside it.</summary>
	public const string DefaultRoiFilter = "General.DefaultRoi.FilterByRoi";

	/// <summary>
	/// Whether the AIRAC run stops for the user to choose for a duplicated alias command no saved
	/// choice settles (<c>Y</c>), or lists it in <c>Duplicate_Alias_Commands.txt</c> (<c>N</c>, the default).
	/// </summary>
	public const string DuplicateAliasesReview = "Services.AiracService.DuplicateAliases.Review";

	/// <summary>
	/// The user's saved choices for duplicated alias commands, one per line as
	/// <c>FileKey|Command|Occurrence|Action|NewCommand</c> (see <c>DuplicateAliasChoices</c>).
	/// </summary>
	public const string DuplicateAliasesChoices = "Services.AiracService.DuplicateAliases.Choices";

	/// <summary>
	/// The output GeoJSON files the Map shows, <c>|</c>-joined and relative to the cycle's output
	/// folder - so the same picks work on any PC, and show as missing until that PC has run them.
	/// </summary>
	public const string MapOutputGeojson = "Services.MapService.OutputGeojson";
}
