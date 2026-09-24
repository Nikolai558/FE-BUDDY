namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// The <c>UserConfig.json</c> keys that are read in more than one place, so they are named once
/// here. Each sub-service's own keys belong to the screen that edits them.
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

	/// <summary>How many decimal places GeoJSON coordinates are rounded to, 0 to 15 (6 when unset).</summary>
	public const string CoordinatePrecision = "Services.AiracService.CoordinatePrecision";
}
