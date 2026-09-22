namespace FeBuddy.Harness;

/// <summary>
/// Every path and toggle a developer needs to edit to run this harness. This is the GUI
/// stand-in's configuration surface - update the constants below rather than hunting through
/// <see cref="Program"/> or the runner classes.
/// </summary>
internal static class HarnessSettings
{
	/// <summary>Directory containing an unzipped NASR 28-day subscription CSV set.</summary>
	public const string NasrSourceDirectory = @"C:\Users\ksand\Downloads\03_Sep_2026_CSV";

	/// <summary>Directory the services write output under (see FE-Buddy_Output/Airways/..., FE-Buddy_Output/Airports/...).</summary>
	public const string OutputDirectory = @"C:\Users\ksand\Downloads\FE-Buddy-Output";

	/// <summary>Mirrors <c>FeBuddy.Core.Configuration.DevMode.IsEnabled</c> for this run.</summary>
	public const bool DevMode = true;

	/// <summary>
	/// Mirrors the Settings "File layout" choice (<c>OutputFormatting.PrettyPrintGeojson</c>).
	/// Only visible with <see cref="DevMode"/> off - developer mode always pretty prints.
	/// </summary>
	public const bool PrettyPrintGeojson = false;

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

			// FE-Buddy's own (non-CRC) properties. FebProperties is required when this is "Y".
			{ "IncludeFebCustomProperties", "Y" },
			{ "FebProperties", "awyId,pointId,waypoints" },

			{ "GenerateAliasFile", "Y" },
			{ "SplitAtAntimeridian", "Y" },
			{ "IncludeCrcLineDefaults", "Y" },
			{ "IncludeCrcSymbolDefaults", "Y" },
			{ "IncludeCrcTextDefaults", "Y" },
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
	/// Builds the raw settings dictionary for <c>AirportService.Run</c>. Every key the Airports
	/// settings parser recognizes is listed below with its default value, so any of them can be
	/// flipped here without hunting through <c>AirportSettingsParser</c>.
	/// </summary>
	public static Dictionary<string, string> AirportSettings()
	{
		Dictionary<string, string> settings = new()
		{
			{ "OutputDirectory", OutputDirectory },

			// Which of the two outputs run. Turning both off is rejected by the parser, as is
			// GenerateGeojson = "Y" with all three Emit* keys off.
			{ "GenerateGeojson", "Y" },
			{ "GenerateAliasFile", "Y" },

			// One file per kind; each can be turned off independently.
			{ "EmitAirportSymbols", "Y" },   // Airports Symbol GeoJSON
			{ "EmitAirportText", "Y" },      // Airports Text GeoJSON
			{ "EmitRunwayLines", "Y" },      // Runways Line GeoJSON

			// FE-Buddy's own (non-CRC) properties. FebProperties is required when this is "Y";
			// the full list below exercises every property the parser knows.
			{ "IncludeFebCustomProperties", "Y" },
			{ "FebProperties", "faaId,icaoId,name,elev,respArtcc,tfcPtrnAlt,fssId,twrType,rwyId" },

			// Writes the CRC ERAM defaults Feature at the head of each GeoJSON file, using the
			// Crc.* values added by AddAirportCrcDefaults below.
			{ "IncludeCrcLineDefaults", "Y" },
			{ "IncludeCrcSymbolDefaults", "Y" },
			{ "IncludeCrcTextDefaults", "Y" },

			// ROI filtering applies to the GeoJSON output only; the alias file always covers
			// every airport. The four corner keys are read only when FilterByRoi is "Y" - set
			// all four before flipping it, or the parser rejects the run naming the blank key.
			{ "FilterByRoi", "N" },
			{ "RoiSwLat", "" },              // e.g. "38.0"
			{ "RoiSwLon", "" },              // e.g. "-85.0"
			{ "RoiNeLat", "" },              // e.g. "43.0"
			{ "RoiNeLon", "" },              // e.g. "-78.0"

			{ "CoordinatePrecision", "6" },  // max decimal places in GeoJSON coords (0-15)
			{ "AddFeBuddyOutputFolder", "Y" } // N -> write straight into OutputDirectory\Airports
		};

		AddAirportCrcDefaults(settings,
			symbolBcg: 5, symbolFilters: "5", symbolStyle: "airport", symbolSize: 1,
			textBcg: 5, textFilters: "5", textSize: 1,
			lineBcg: 6, lineFilters: "6", lineStyle: "solid", lineThickness: 1);

		return settings;
	}

	/// <summary>
	/// Builds the raw settings dictionary for <c>DepartureService.Run</c>. Every key the
	/// Departures settings parser recognizes is listed below with its default value, so any of
	/// them can be flipped here without hunting through <c>DepartureSettingsParser</c>.
	/// </summary>
	public static Dictionary<string, string> DepartureSettings()
	{
		Dictionary<string, string> settings = new()
		{
			{ "OutputDirectory", OutputDirectory },

			// Which of the two outputs run. Turning both off is rejected by the parser, as is
			// GenerateGeojson = "Y" with all three Emit* keys off.
			{ "GenerateGeojson", "Y" },
			{ "GenerateAliasFile", "Y" },

			// Up to three files per airport + procedure. Lines is skipped automatically for a
			// procedure that is a single point.
			{ "EmitLines", "Y" },
			{ "EmitSymbols", "Y" },
			{ "EmitText", "Y" },

			// Filters - every one applies to GeoJSON AND the alias file.
			{ "IncludeObstacleDepartures", "Y" }, // N -> SIDs only
			{ "ArtccFilter", "ZOB" },                // e.g. "ZLA,ZOA"; empty = every ARTCC
			// AmendmentFilter "None" keeps every procedure. Each other mode reads only its own key:
			// "Cycles" + AmendedWithinCycles (1 = amended this cycle; 4 = this cycle or the 3 before),
			// "Days" + AmendedWithinDays (e.g. "90", counted back from today),
			// "Date" + AmendedOnOrAfter (yyyy-MM-dd, e.g. "2026-01-01").
			{ "AmendmentFilter", "None" },

			// ROI. The four corner keys are read only when FilterByRoi is "Y". RoiMode "Airport"
			// keeps every departure of an airport inside the box; "Waypoint" keeps any departure
			// with a point inside it.
			{ "FilterByRoi", "N" },
			{ "RoiMode", "Airport" },
			{ "RoiSwLat", "" },                   // e.g. "32.5"
			{ "RoiSwLon", "" },                   // e.g. "-121.0"
			{ "RoiNeLat", "" },                   // e.g. "36.0"
			{ "RoiNeLon", "" },                   // e.g. "-114.0"

			// FE-Buddy's own (non-CRC) properties. FebProperties is required when this is "Y".
			{ "IncludeFebCustomProperties", "Y" },
			{ "FebProperties", "dpName,pointId,arptId,artcc,amendmentNo,amendEffDate,waypoints" },

			// Writes the CRC ERAM defaults Feature at the head of each GeoJSON file, using the
			// Crc.Departures.* values added by AddDepartureCrcDefaults below.
			{ "IncludeCrcLineDefaults", "Y" },
			{ "IncludeCrcSymbolDefaults", "Y" },
			{ "IncludeCrcTextDefaults", "Y" },

			{ "CoordinatePrecision", "6" },       // max decimal places in GeoJSON coords (0-15)
			{ "AddFeBuddyOutputFolder", "Y" }     // N -> write straight into OutputDirectory\Departure Procedures
		};

		AddDepartureCrcDefaults(settings,
			lineBcg: 7, lineFilters: "7", lineStyle: "solid", lineThickness: 1,
			symbolBcg: 7, symbolFilters: "7", symbolStyle: "otherWaypoints", symbolSize: 1,
			textBcg: 7, textFilters: "7", textSize: 1);

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

	/// <summary>
	/// Adds the <c>Crc.Airports.*</c> and <c>Crc.Runways.*</c> property defaults. Kept separate
	/// from <see cref="AddCrcDefaults"/> because the Airports sub-service splits its blocks
	/// across two classes - Symbol and Text belong to <c>Airports</c>, Line to <c>Runways</c> -
	/// rather than writing all three kinds for one class.
	/// </summary>
	/// <param name="settings">The dictionary being built.</param>
	/// <param name="symbolBcg">Airport symbol BCG group, 1-40.</param>
	/// <param name="symbolFilters">Airport symbol filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="symbolStyle">Airport symbol style, one of <c>CrcGeojsonPropertyValidator.ValidSymbolStyles</c>.</param>
	/// <param name="symbolSize">Airport symbol size, 1-4.</param>
	/// <param name="textBcg">Airport text BCG group, 1-40.</param>
	/// <param name="textFilters">Airport text filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="textSize">Airport text size, 0-5.</param>
	/// <param name="lineBcg">Runway line BCG group, 1-40.</param>
	/// <param name="lineFilters">Runway line filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="lineStyle">Runway line style, one of <c>CrcGeojsonPropertyValidator.ValidLineStyles</c>.</param>
	/// <param name="lineThickness">Runway line thickness, 1-3.</param>
	private static void AddAirportCrcDefaults(
		Dictionary<string, string> settings,
		int symbolBcg,
		string symbolFilters,
		string symbolStyle,
		int symbolSize,
		int textBcg,
		string textFilters,
		int textSize,
		int lineBcg,
		string lineFilters,
		string lineStyle,
		int lineThickness)
	{
		settings["Crc.Airports.Symbol.bcg"] = symbolBcg.ToString();
		settings["Crc.Airports.Symbol.filters"] = symbolFilters;
		settings["Crc.Airports.Symbol.style"] = symbolStyle;
		settings["Crc.Airports.Symbol.size"] = symbolSize.ToString();

		// No "Crc.Airports.Text.text": every airport supplies its own label from its identifier
		// and name, and the parser warns if one is supplied here.
		settings["Crc.Airports.Text.bcg"] = textBcg.ToString();
		settings["Crc.Airports.Text.filters"] = textFilters;
		settings["Crc.Airports.Text.size"] = textSize.ToString();
		settings["Crc.Airports.Text.underline"] = "N";
		settings["Crc.Airports.Text.xOffset"] = "0";
		settings["Crc.Airports.Text.yOffset"] = "0";
		settings["Crc.Airports.Text.opaque"] = "N";

		settings["Crc.Runways.Line.bcg"] = lineBcg.ToString();
		settings["Crc.Runways.Line.filters"] = lineFilters;
		settings["Crc.Runways.Line.style"] = lineStyle;
		settings["Crc.Runways.Line.thickness"] = lineThickness.ToString();
	}

	/// <summary>
	/// Adds the <c>Crc.Departures.*</c> property defaults - one class covering the Lines,
	/// Symbols and Text files. These values are the harness's own; the GUI starts every CRC box
	/// empty and makes the user choose.
	/// </summary>
	/// <param name="settings">The dictionary being built.</param>
	/// <param name="lineBcg">Line BCG group, 1-40.</param>
	/// <param name="lineFilters">Line filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="lineStyle">Line style, one of <c>CrcGeojsonPropertyValidator.ValidLineStyles</c>.</param>
	/// <param name="lineThickness">Line thickness, 1-3.</param>
	/// <param name="symbolBcg">Symbol BCG group, 1-40.</param>
	/// <param name="symbolFilters">Symbol filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="symbolStyle">Symbol style, one of <c>CrcGeojsonPropertyValidator.ValidSymbolStyles</c>.</param>
	/// <param name="symbolSize">Symbol size, 1-4.</param>
	/// <param name="textBcg">Text BCG group, 1-40.</param>
	/// <param name="textFilters">Text filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="textSize">Text size, 0-5.</param>
	private static void AddDepartureCrcDefaults(
		Dictionary<string, string> settings,
		int lineBcg,
		string lineFilters,
		string lineStyle,
		int lineThickness,
		int symbolBcg,
		string symbolFilters,
		string symbolStyle,
		int symbolSize,
		int textBcg,
		string textFilters,
		int textSize)
	{
		settings["Crc.Departures.Line.bcg"] = lineBcg.ToString();
		settings["Crc.Departures.Line.filters"] = lineFilters;
		settings["Crc.Departures.Line.style"] = lineStyle;
		settings["Crc.Departures.Line.thickness"] = lineThickness.ToString();

		settings["Crc.Departures.Symbol.bcg"] = symbolBcg.ToString();
		settings["Crc.Departures.Symbol.filters"] = symbolFilters;
		settings["Crc.Departures.Symbol.style"] = symbolStyle;
		settings["Crc.Departures.Symbol.size"] = symbolSize.ToString();

		// No "Crc.Departures.Text.text": every point is labelled with its own identifier, and
		// the parser warns if one is supplied here.
		settings["Crc.Departures.Text.bcg"] = textBcg.ToString();
		settings["Crc.Departures.Text.filters"] = textFilters;
		settings["Crc.Departures.Text.size"] = textSize.ToString();
		settings["Crc.Departures.Text.underline"] = "N";
		settings["Crc.Departures.Text.xOffset"] = "0";
		settings["Crc.Departures.Text.yOffset"] = "0";
		settings["Crc.Departures.Text.opaque"] = "N";
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
