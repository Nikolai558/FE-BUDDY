namespace FeBuddy.Harness;

/// <summary>
/// Every path and toggle a developer needs to edit to run this harness. This is the GUI
/// stand-in's configuration surface - update the constants below rather than hunting through
/// <see cref="Program"/> or the runner classes.
/// </summary>
internal static class HarnessSettings
{
	/// <summary>Directory containing an unzipped NASR 28-day subscription CSV set.</summary>
	public const string NasrSourceDirectory = @"D:\Downloads\01_Oct_2026_CSV";

	/// <summary>Directory the Airways services write output under (see FE-Buddy_Output/Airways/...).</summary>
	public const string OutputDirectory = @"D:\Downloads\FE-Buddy-Output";

	/// <summary>Mirrors <c>FeBuddy.Core.Configuration.DevMode.IsEnabled</c> for this run.</summary>
	public const bool DevMode = true;

	/// <summary>
	/// Builds the raw settings dictionary for <c>AirwayService.Run</c>. Edit the values below
	/// to exercise different output modes, ROI filtering, buffering, etc.
	/// </summary>
	public static Dictionary<string, string> AirwaySettings()
	{
		Dictionary<string, string> settings = new()
		{
			{ "OutputDirectory", OutputDirectory },
			{ "OutputBy", "HighLow" },
			{ "BufferAirwayWaypoints", "N" },
			{ "IncludeFebCustomProperties", "Y" },
			{ "IncludeAirwayWaypointIds", "Y" },
			{ "GenerateAliasFile", "Y" },
			{ "SplitAtAntimeridian", "Y" },
			{ "IncludeCrcEramPropertyDefaults", "Y" },
			{ "FilterByRoi", "N" },

			// Phase 3.3-3.7 settings. Defaults shown; omit any of these and the parser uses
			// the same default.
			{ "ExcludedDesignations", "" },   // e.g. "RN,SL" to drop those designations entirely (3.3)
			{ "EmitLines", "Y" },             // per-kind output opt-out (3.4)
			{ "EmitSymbols", "Y" },
			{ "EmitText", "Y" },
			{ "AliasRoiScope", "All" },       // "All" or "RoiAirways" (3.5)
			{ "CoordinatePrecision", "6" },   // max decimal places in GeoJSON coords (3.6)
			{ "AddFeBuddyOutputFolder", "Y" } // N -> write straight into OutputDirectory\Airways (3.7)

			// To exercise Designation grouping, waypoint buffering, or ROI clipping
			// (all verified working against real NASR data during development), try e.g.:
			//   { "OutputBy", "Designation" },
			//   { "BufferAirwayWaypoints", "Y" },
			//   { "FilterByRoi", "Y" },
			//   { "RoiSwLat", "38.0" }, { "RoiSwLon", "-85.0" },
			//   { "RoiNeLat", "43.0" }, { "RoiNeLon", "-78.0" },
		};

		AddCrcDefaults(settings, "High", bcg: 3, filters: "3", lineStyle: "solid", thickness: "1",
			symbolStyle: "vor", symbolSize: "1", textSize: "1");

		AddCrcDefaults(settings, "Low", bcg: 2, filters: "2", lineStyle: "shortDashed", thickness: "1",
			symbolStyle: "vor", symbolSize: "1", textSize: "1");

		AddCrcDefaults(settings, "Other", bcg: 1, filters: "1", lineStyle: "longDashed", thickness: "1",
			symbolStyle: "otherWaypoints", symbolSize: "1", textSize: "1");

		return settings;
	}

	/// <summary>
	/// Settings for exercising the alias-only path (<c>OutputBy = None</c>, alias file still
	/// generated), matching the "written even when OutputBy = None" contract.
	/// </summary>
	public static Dictionary<string, string> AliasOnlySettings()
	{
		Dictionary<string, string> settings = AirwaySettings();
		settings["OutputBy"] = "None";
		settings["GenerateAliasFile"] = "Y";
		return settings;
	}

	private static void AddCrcDefaults(
		Dictionary<string, string> settings,
		string cls,
		int bcg,
		string filters,
		string lineStyle,
		string thickness,
		string symbolStyle,
		string symbolSize,
		string textSize)
	{
		settings[$"Crc.{cls}.Line.bcg"] = bcg.ToString();
		settings[$"Crc.{cls}.Line.filters"] = filters;
		settings[$"Crc.{cls}.Line.style"] = lineStyle;
		settings[$"Crc.{cls}.Line.thickness"] = thickness;

		settings[$"Crc.{cls}.Symbol.bcg"] = bcg.ToString();
		settings[$"Crc.{cls}.Symbol.filters"] = filters;
		settings[$"Crc.{cls}.Symbol.style"] = symbolStyle;
		settings[$"Crc.{cls}.Symbol.size"] = symbolSize;

		settings[$"Crc.{cls}.Text.bcg"] = bcg.ToString();
		settings[$"Crc.{cls}.Text.filters"] = filters;
		settings[$"Crc.{cls}.Text.size"] = textSize;
		settings[$"Crc.{cls}.Text.underline"] = "N";
		settings[$"Crc.{cls}.Text.xOffset"] = "0";
		settings[$"Crc.{cls}.Text.yOffset"] = "0";
		settings[$"Crc.{cls}.Text.opaque"] = "N";
	}
}
