using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.WxStations;

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

	/// <summary>Full path to an unzipped copy of the aviationweather.gov Wx Stations cache file.</summary>
	public const string WxStationsSourceFile = @"C:\Users\ksand\Downloads\stations.cache.xml\stations.cache.xml";

	/// <summary>
	/// The folder the services write into, as the AIRAC Service's <c>AIRAC_&lt;cycle&gt;</c> folder
	/// would be: alias files here, GeoJSON in its <c>Geojson</c> folder, and anything marked for
	/// vNAS under <c>Upload_to_vNAS</c>. The harness calls each service directly, so there is no
	/// cycle folder of its own.
	/// </summary>
	public const string OutputDirectory = @"C:\Users\ksand\Downloads\FE-Buddy-Output";

	/// <summary>Mirrors <c>DevMode.IsEnabled</c> for this run.</summary>
	public const bool DevMode = true;

	/// <summary>Every GeoJSON file a HighLow Airways run can write, by file key.</summary>
	private const string AirwayHighLowFiles =
		"Airways_High_Lines,Airways_High_Symbols,Airways_High_Text," +
		"Airways_Low_Lines,Airways_Low_Symbols,Airways_Low_Text," +
		"Airways_Other_Lines,Airways_Other_Symbols,Airways_Other_Text";

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

			// Files marked for vNAS go under Upload_to_vNAS; only those in CrcDefaultsFor get the
			// CRC ERAM defaults Feature, using the Crc.* values added by AddCrcDefaults below.
			{ "UploadToVnas", AirwayHighLowFiles + ",Airways.txt" },
			{ "CrcDefaultsFor", AirwayHighLowFiles },
			{ "FilterByRoi", "N" },

			// Phase 3.3-3.7 settings. Defaults shown; omit any of these and the parser uses
			// the same default.
			{ "ExcludedDesignations", "" },   // e.g. "RN,SL" to drop those designations entirely (3.3)
			{ "EmitLines", "Y" },             // per-kind output opt-out (3.4)
			{ "EmitSymbols", "Y" },
			{ "EmitText", "Y" },
			{ "AliasRoiScope", "All" },       // "All" or "RoiAirways" (3.5)
			{ "CoordinatePrecision", "6" },   // max decimal places in GeoJSON coords (3.6)

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

			// Files marked for vNAS go under Upload_to_vNAS; only those in CrcDefaultsFor get the
			// CRC ERAM defaults Feature, using the Crc.* values added by AddAirportCrcDefaults below.
			{ "UploadToVnas", "Runways_Lines,Airports_Symbols,Airports_Text,Airports.txt" },
			{ "CrcDefaultsFor", "Runways_Lines,Airports_Symbols,Airports_Text" },

			// ROI filtering applies to the GeoJSON output only; the alias file always covers
			// every airport. The four corner keys are read only when FilterByRoi is "Y" - set
			// all four before flipping it, or the parser rejects the run naming the blank key.
			{ "FilterByRoi", "N" },
			{ "RoiSwLat", "" },              // e.g. "38.0"
			{ "RoiSwLon", "" },              // e.g. "-85.0"
			{ "RoiNeLat", "" },              // e.g. "43.0"
			{ "RoiNeLon", "" },              // e.g. "-78.0"

			{ "CoordinatePrecision", "6" },  // max decimal places in GeoJSON coords (0-15)
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

			// Chosen per kind (a run writes thousands of files): each kind listed goes under
			// Upload_to_vNAS, and only those in CrcDefaultsFor get the CRC ERAM defaults Feature,
			// using the Crc.Departures.* values added by AddDepartureCrcDefaults below.
			{ "UploadToVnas", "Departures_Lines,Departures_Symbols,Departures_Text,Departures.txt" },
			{ "CrcDefaultsFor", "Departures_Lines,Departures_Symbols,Departures_Text" },

			{ "CoordinatePrecision", "6" },       // max decimal places in GeoJSON coords (0-15)
		};

		AddDepartureCrcDefaults(settings,
			lineBcg: 7, lineFilters: "7", lineStyle: "solid", lineThickness: 1,
			symbolBcg: 7, symbolFilters: "7", symbolStyle: "otherWaypoints", symbolSize: 1,
			textBcg: 7, textFilters: "7", textSize: 1);

		return settings;
	}

	/// <summary>
	/// Builds the raw settings dictionary for <c>ArrivalService.Run</c>. Every key the
	/// Arrivals settings parser recognizes is listed below with its default value, so any of
	/// them can be flipped here without hunting through <c>ArrivalSettingsParser</c>.
	/// </summary>
	public static Dictionary<string, string> ArrivalSettings()
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

			// Filters - every one applies to GeoJSON AND the alias file. STAR_BASE.ARTCC can list
			// several centres space-separated (e.g. "ZDC ZNY") when a STAR is shared between them;
			// each airport's copy of the procedure is still filtered by its own ARTCC.
			{ "ArtccFilter", "ZOB" },                // e.g. "ZLA,ZOA"; empty = every ARTCC
			// AmendmentFilter "None" keeps every procedure. Each other mode reads only its own key:
			// "Cycles" + AmendedWithinCycles (1 = amended this cycle; 4 = this cycle or the 3 before),
			// "Days" + AmendedWithinDays (e.g. "90", counted back from today),
			// "Date" + AmendedOnOrAfter (yyyy-MM-dd, e.g. "2026-01-01").
			{ "AmendmentFilter", "None" },

			// ROI. The four corner keys are read only when FilterByRoi is "Y". RoiMode "Airport"
			// keeps every arrival of an airport inside the box; "Waypoint" keeps any arrival
			// with a point inside it.
			{ "FilterByRoi", "N" },
			{ "RoiMode", "Airport" },
			{ "RoiSwLat", "" },                   // e.g. "32.5"
			{ "RoiSwLon", "" },                   // e.g. "-121.0"
			{ "RoiNeLat", "" },                   // e.g. "36.0"
			{ "RoiNeLon", "" },                   // e.g. "-114.0"

			// FE-Buddy's own (non-CRC) properties. FebProperties is required when this is "Y".
			{ "IncludeFebCustomProperties", "Y" },
			{ "FebProperties", "arrivalName,pointId,arptId,artcc,amendmentNo,amendEffDate,waypoints" },

			// Chosen per kind (a run writes thousands of files): each kind listed goes under
			// Upload_to_vNAS, and only those in CrcDefaultsFor get the CRC ERAM defaults Feature,
			// using the Crc.Arrivals.* values added by AddArrivalCrcDefaults below.
			{ "UploadToVnas", "Arrivals_Lines,Arrivals_Symbols,Arrivals_Text,Arrivals.txt" },
			{ "CrcDefaultsFor", "Arrivals_Lines,Arrivals_Symbols,Arrivals_Text" },

			{ "CoordinatePrecision", "6" },       // max decimal places in GeoJSON coords (0-15)
		};

		AddArrivalCrcDefaults(settings,
			lineBcg: 8, lineFilters: "8", lineStyle: "solid", lineThickness: 1,
			symbolBcg: 8, symbolFilters: "8", symbolStyle: "otherWaypoints", symbolSize: 1,
			textBcg: 8, textFilters: "8", textSize: 1);

		return settings;
	}

	/// <summary>
	/// Builds the raw settings dictionary for <c>NavaidService.Run</c>. Every key the NAVAIDs
	/// settings parser recognizes is listed below with its default value, so any of them can be
	/// flipped here without hunting through <c>NavaidSettingsParser</c>.
	/// </summary>
	public static Dictionary<string, string> NavaidSettings()
	{
		Dictionary<string, string> settings = new()
		{
			{ "OutputDirectory", OutputDirectory },

			// Which of the two outputs run. Turning both off is rejected by the parser, as is
			// GenerateGeojson = "Y" with both Emit* keys off. There is no EmitLines: NAVAIDs has
			// no Lines file.
			{ "GenerateGeojson", "Y" },
			{ "GenerateAliasFile", "Y" },
			{ "EmitSymbols", "Y" },
			{ "EmitText", "Y" },

			// How the GeoJSON is grouped. "All" writes one merged Symbols/Text pair; "Type"
			// writes a Symbols/Text pair per NAVAID type present (NavaidOutputFiles.TypeKey).
			{ "OutputBy", "All" },   // or "Type"
			{ "ExcludedTypes", "" }, // e.g. "CONSOLAN,MARINE NDB" to drop those types entirely

			// SymbolStyleBy and FanMarkerStyle matter only in "All" mode, and only when the
			// merged Symbols file gets CRC-ERAM defaults (below). "Type" styles each NAVAID from
			// its own type (NavaidTypes.SymbolStyleFor); fan markers have no fixed style, so they
			// take FanMarkerStyle - without it they get none, and the run warns.
			{ "SymbolStyleBy", "Type" }, // or "File"
			{ "FanMarkerStyle", "otherWaypoints" },

			// FE-Buddy's own (non-CRC) properties. FebProperties is required when this is "Y".
			{ "IncludeFebCustomProperties", "Y" },
			{ "FebProperties", "navId,navType,name,freq,lowAltArtccId,highAltArtccId" },

			// Files marked for vNAS go under Upload_to_vNAS; only those in CrcDefaultsFor get the
			// CRC ERAM defaults Feature, using the Crc.NAVAIDs.* values added by
			// AddNavaidCrcDefaults below.
			{ "UploadToVnas", "NAVAIDs_Symbols,NAVAIDs_Text,NAVAIDs.txt" },
			{ "CrcDefaultsFor", "NAVAIDs_Symbols,NAVAIDs_Text" },

			// ROI filtering applies to the GeoJSON output only; the alias file always covers
			// every NAVAID.
			{ "FilterByRoi", "N" },
			{ "RoiSwLat", "" },              // e.g. "38.0"
			{ "RoiSwLon", "" },              // e.g. "-85.0"
			{ "RoiNeLat", "" },              // e.g. "43.0"
			{ "RoiNeLon", "" },              // e.g. "-78.0"

			{ "CoordinatePrecision", "6" },  // max decimal places in GeoJSON coords (0-15)
		};

		AddNavaidCrcDefaults(settings,
			symbolBcg: 9, symbolFilters: "9", symbolSize: 1,
			textBcg: 9, textFilters: "9", textSize: 1);

		return settings;
	}

	/// <summary>
	/// Builds the raw settings dictionary for <c>ArtccBoundaryService.Run</c>. Every key the ARTCC
	/// Boundaries settings parser recognizes is listed below with its default value, so any of
	/// them can be flipped here without hunting through <c>ArtccBoundarySettingsParser</c>.
	/// </summary>
	public static Dictionary<string, string> ArtccBoundarySettings()
	{
		Dictionary<string, string> settings = new()
		{
			{ "OutputDirectory", OutputDirectory },

			// How the GeoJSON is grouped. "HighLow" writes one High file and one Low file, an
			// UNLIMITED ring going into both; "HighLowUnlimited" adds a third, Unlimited-only
			// file; "ArtccAltitude" writes one file per LocationId and altitude present, e.g.
			// ARTCC-Boundary_ZOB-HIGH_Lines.
			{ "OutputBy", "HighLow" },   // or "HighLowUnlimited", "ArtccAltitude"
			{ "LocationFilter", "" },    // e.g. "ZOB,ZNY"; empty = every ARTCC

			{ "SplitAtAntimeridian", "Y" },

			// FE-Buddy's own (non-CRC) properties. FebProperties is required when this is "Y".
			{ "IncludeFebCustomProperties", "Y" },
			{ "FebProperties", "locationId,locationName,locationType,icaoId,computerId,altitude,type,city,countryCode" },

			// Files marked for vNAS go under Upload_to_vNAS; only those in CrcDefaultsFor get the
			// CRC ERAM defaults Feature, using the Crc.High.* / Crc.Low.* values added by
			// AddArtccBoundaryCrcDefaults below.
			{ "UploadToVnas", "ARTCC-Boundary_High_Lines,ARTCC-Boundary_Low_Lines" },
			{ "CrcDefaultsFor", "ARTCC-Boundary_High_Lines,ARTCC-Boundary_Low_Lines" },

			{ "FilterByRoi", "N" },
			{ "RoiSwLat", "" },              // e.g. "38.0"
			{ "RoiSwLon", "" },              // e.g. "-85.0"
			{ "RoiNeLat", "" },              // e.g. "43.0"
			{ "RoiNeLon", "" },              // e.g. "-78.0"

			{ "CoordinatePrecision", "6" },  // max decimal places in GeoJSON coords (0-15)
		};

		AddArtccBoundaryCrcDefaults(settings,
			bcg: 10, filters: "10", highStyle: "solid", lowStyle: "longDashed", thickness: 1);

		return settings;
	}

	/// <summary>
	/// Builds the raw settings dictionary for <c>FixService.Run</c>. Every key the Fixes settings
	/// parser recognizes is listed below with its default value, so any of them can be flipped
	/// here without hunting through <c>FixSettingsParser</c>.
	/// </summary>
	public static Dictionary<string, string> FixSettings()
	{
		Dictionary<string, string> settings = new()
		{
			{ "OutputDirectory", OutputDirectory },

			// GeoJSON is the only output the Fixes sub-service has - there is no GenerateGeojson
			// and no alias file.
			{ "EmitSymbols", "Y" },
			{ "EmitText", "Y" },

			// How the GeoJSON is grouped. "ChartAndFixUse" writes a Symbols/Text pair per listed
			// combination below; the other layouts are "All", "FixUse" and "Chart".
			{ "OutputBy", "ChartAndFixUse" },   // or "All", "FixUse", "Chart"
			{ "ExcludedFixUses", "" },          // e.g. "RADAR,MIL-WYPNT" - only used in "FixUse" layout
			{ "ExcludedCharts", "" },           // e.g. "SECTIONAL,AREA" - only used in "Chart" layout
			{ "Combinations", "ENROUTE-LOW+WYPNT,ENROUTE-HIGH+WYPNT,IAP+RPRTNG-PNT" },

			// FE-Buddy's own (non-CRC) properties. FebProperties is required when this is "Y".
			{ "IncludeFebCustomProperties", "Y" },
			{ "FebProperties", "fixId,fixUseCode,charts" },

			// Files marked for vNAS go under Upload_to_vNAS; only those in CrcDefaultsFor get the
			// CRC ERAM defaults Feature, using the Crc.ENROUTE-LOW-WYPNT.* values added by
			// AddFixCrcDefaults below.
			{ "UploadToVnas", "Fix_ENROUTE-LOW-WYPNT_Symbols,Fix_ENROUTE-LOW-WYPNT_Text" },
			{ "CrcDefaultsFor", "Fix_ENROUTE-LOW-WYPNT_Symbols,Fix_ENROUTE-LOW-WYPNT_Text" },

			// ROI filtering applies to the GeoJSON output only.
			{ "FilterByRoi", "N" },
			{ "RoiSwLat", "" },              // e.g. "38.0"
			{ "RoiSwLon", "" },              // e.g. "-85.0"
			{ "RoiNeLat", "" },              // e.g. "43.0"
			{ "RoiNeLon", "" },              // e.g. "-78.0"

			{ "CoordinatePrecision", "6" },  // max decimal places in GeoJSON coords (0-15)
		};

		AddFixCrcDefaults(settings, "ENROUTE-LOW-WYPNT",
			symbolBcg: 11, symbolFilters: "11", symbolStyle: "otherWaypoints", symbolSize: 1,
			textBcg: 11, textFilters: "11", textSize: 1);

		return settings;
	}

	/// <summary>
	/// Builds the raw settings dictionary for <c>WxStationService.Run</c>. Every key the Wx
	/// Stations settings parser recognizes is listed below with its default value.
	/// </summary>
	public static Dictionary<string, string> WxStationSettings()
	{
		Dictionary<string, string> settings = new()
		{
			{ "OutputDirectory", OutputDirectory },

			// GeoJSON is the only output the Wx Stations sub-service has - there is no
			// GenerateGeojson, no alias file, and no feb.* properties.
			{ "EmitSymbols", "Y" },
			{ "EmitText", "Y" },

			// Only the Symbols file goes to vNAS/gets CRC-ERAM defaults here, even though both
			// Crc.Wx.Symbol.* and Crc.Wx.Text.* are populated below - Text's block is simply
			// unused by this run, exercising a station being labelled without CRC defaults.
			{ "UploadToVnas", WxStationOutputFiles.Symbols },
			{ "CrcDefaultsFor", WxStationOutputFiles.Symbols },

			// ROI filtering applies to the GeoJSON output only.
			{ "FilterByRoi", "N" },
			{ "RoiSwLat", "" },              // e.g. "38.0"
			{ "RoiSwLon", "" },              // e.g. "-85.0"
			{ "RoiNeLat", "" },              // e.g. "43.0"
			{ "RoiNeLon", "" },              // e.g. "-78.0"

			{ "CoordinatePrecision", "6" },  // max decimal places in GeoJSON coords (0-15)
		};

		AddWxStationCrcDefaults(settings,
			symbolBcg: 12, symbolFilters: "12", symbolStyle: "otherWaypoints", symbolSize: 1,
			textBcg: 12, textFilters: "12", textSize: 1);

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
	/// <param name="symbolStyle">Airport symbol style, one of <c>CrcPropertyValidator.ValidSymbolStyles</c>.</param>
	/// <param name="symbolSize">Airport symbol size, 1-4.</param>
	/// <param name="textBcg">Airport text BCG group, 1-40.</param>
	/// <param name="textFilters">Airport text filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="textSize">Airport text size, 0-5.</param>
	/// <param name="lineBcg">Runway line BCG group, 1-40.</param>
	/// <param name="lineFilters">Runway line filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="lineStyle">Runway line style, one of <c>CrcPropertyValidator.ValidLineStyles</c>.</param>
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
	/// <param name="lineStyle">Line style, one of <c>CrcPropertyValidator.ValidLineStyles</c>.</param>
	/// <param name="lineThickness">Line thickness, 1-3.</param>
	/// <param name="symbolBcg">Symbol BCG group, 1-40.</param>
	/// <param name="symbolFilters">Symbol filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="symbolStyle">Symbol style, one of <c>CrcPropertyValidator.ValidSymbolStyles</c>.</param>
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

	/// <summary>
	/// Adds the <c>Crc.Arrivals.*</c> property defaults - one class covering the Lines,
	/// Symbols and Text files. These values are the harness's own; the GUI starts every CRC box
	/// empty and makes the user choose.
	/// </summary>
	/// <param name="settings">The dictionary being built.</param>
	/// <param name="lineBcg">Line BCG group, 1-40.</param>
	/// <param name="lineFilters">Line filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="lineStyle">Line style, one of <c>CrcPropertyValidator.ValidLineStyles</c>.</param>
	/// <param name="lineThickness">Line thickness, 1-3.</param>
	/// <param name="symbolBcg">Symbol BCG group, 1-40.</param>
	/// <param name="symbolFilters">Symbol filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="symbolStyle">Symbol style, one of <c>CrcPropertyValidator.ValidSymbolStyles</c>.</param>
	/// <param name="symbolSize">Symbol size, 1-4.</param>
	/// <param name="textBcg">Text BCG group, 1-40.</param>
	/// <param name="textFilters">Text filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="textSize">Text size, 0-5.</param>
	private static void AddArrivalCrcDefaults(
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
		settings["Crc.Arrivals.Line.bcg"] = lineBcg.ToString();
		settings["Crc.Arrivals.Line.filters"] = lineFilters;
		settings["Crc.Arrivals.Line.style"] = lineStyle;
		settings["Crc.Arrivals.Line.thickness"] = lineThickness.ToString();

		settings["Crc.Arrivals.Symbol.bcg"] = symbolBcg.ToString();
		settings["Crc.Arrivals.Symbol.filters"] = symbolFilters;
		settings["Crc.Arrivals.Symbol.style"] = symbolStyle;
		settings["Crc.Arrivals.Symbol.size"] = symbolSize.ToString();

		// No "Crc.Arrivals.Text.text": every point is labelled with its own identifier, and
		// the parser warns if one is supplied here.
		settings["Crc.Arrivals.Text.bcg"] = textBcg.ToString();
		settings["Crc.Arrivals.Text.filters"] = textFilters;
		settings["Crc.Arrivals.Text.size"] = textSize.ToString();
		settings["Crc.Arrivals.Text.underline"] = "N";
		settings["Crc.Arrivals.Text.xOffset"] = "0";
		settings["Crc.Arrivals.Text.yOffset"] = "0";
		settings["Crc.Arrivals.Text.opaque"] = "N";
	}

	/// <summary>
	/// Adds the <c>Crc.NAVAIDs.*</c> property defaults for the merged All-mode Symbols and Text
	/// files. These values are the harness's own; the GUI starts every CRC box empty and makes
	/// the user choose.
	/// </summary>
	/// <param name="settings">The dictionary being built.</param>
	/// <param name="symbolBcg">Symbol BCG group, 1-40.</param>
	/// <param name="symbolFilters">Symbol filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="symbolSize">Symbol size, 1-4.</param>
	/// <param name="textBcg">Text BCG group, 1-40.</param>
	/// <param name="textFilters">Text filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="textSize">Text size, 0-5.</param>
	private static void AddNavaidCrcDefaults(
		Dictionary<string, string> settings,
		int symbolBcg,
		string symbolFilters,
		int symbolSize,
		int textBcg,
		string textFilters,
		int textSize)
	{
		// No "Crc.NAVAIDs.Symbol.style": SymbolStyleBy "Type" styles each NAVAID from its own
		// type (NavaidTypes.SymbolStyleFor), so the merged file's own defaults carry no style of
		// their own - see NavaidSettingsParser's readStyle argument.
		settings["Crc.NAVAIDs.Symbol.bcg"] = symbolBcg.ToString();
		settings["Crc.NAVAIDs.Symbol.filters"] = symbolFilters;
		settings["Crc.NAVAIDs.Symbol.size"] = symbolSize.ToString();

		// No "Crc.NAVAIDs.Text.text": every NAVAID supplies its own label from its identifier,
		// name and type, and the parser warns if one is supplied here.
		settings["Crc.NAVAIDs.Text.bcg"] = textBcg.ToString();
		settings["Crc.NAVAIDs.Text.filters"] = textFilters;
		settings["Crc.NAVAIDs.Text.size"] = textSize.ToString();
		settings["Crc.NAVAIDs.Text.underline"] = "N";
		settings["Crc.NAVAIDs.Text.xOffset"] = "0";
		settings["Crc.NAVAIDs.Text.yOffset"] = "0";
		settings["Crc.NAVAIDs.Text.opaque"] = "N";
	}

	/// <summary>
	/// Adds the <c>Crc.High.Line.*</c> and <c>Crc.Low.Line.*</c> property defaults. These values
	/// are the harness's own; the GUI starts every CRC box empty and makes the user choose.
	/// </summary>
	/// <param name="settings">The dictionary being built.</param>
	/// <param name="bcg">BCG group, 1-40, shared by both classes.</param>
	/// <param name="filters">Filters, comma-separated, each 0-40, at least one, shared by both classes.</param>
	/// <param name="highStyle">High line style, one of <c>CrcPropertyValidator.ValidLineStyles</c>.</param>
	/// <param name="lowStyle">Low line style, one of <c>CrcPropertyValidator.ValidLineStyles</c>.</param>
	/// <param name="thickness">Line thickness, 1-3, shared by both classes.</param>
	private static void AddArtccBoundaryCrcDefaults(
		Dictionary<string, string> settings,
		int bcg,
		string filters,
		string highStyle,
		string lowStyle,
		int thickness)
	{
		settings["Crc.High.Line.bcg"] = bcg.ToString();
		settings["Crc.High.Line.filters"] = filters;
		settings["Crc.High.Line.style"] = highStyle;
		settings["Crc.High.Line.thickness"] = thickness.ToString();

		settings["Crc.Low.Line.bcg"] = bcg.ToString();
		settings["Crc.Low.Line.filters"] = filters;
		settings["Crc.Low.Line.style"] = lowStyle;
		settings["Crc.Low.Line.thickness"] = thickness.ToString();
	}

	/// <summary>
	/// Adds the <c>Crc.&lt;cls&gt;.Symbol.*</c> and <c>Crc.&lt;cls&gt;.Text.*</c> property defaults
	/// for one Fixes group - a fix use, a chart, or a chart + fix use combination. These values
	/// are the harness's own; the GUI starts every CRC box empty and makes the user choose.
	/// </summary>
	/// <param name="settings">The dictionary being built.</param>
	/// <param name="cls">The group's CRC class name, e.g. <c>ENROUTE-LOW-WYPNT</c>.</param>
	/// <param name="symbolBcg">Symbol BCG group, 1-40.</param>
	/// <param name="symbolFilters">Symbol filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="symbolStyle">Symbol style, one of <c>CrcPropertyValidator.ValidSymbolStyles</c>.</param>
	/// <param name="symbolSize">Symbol size, 1-4.</param>
	/// <param name="textBcg">Text BCG group, 1-40.</param>
	/// <param name="textFilters">Text filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="textSize">Text size, 0-5.</param>
	private static void AddFixCrcDefaults(
		Dictionary<string, string> settings,
		string cls,
		int symbolBcg,
		string symbolFilters,
		string symbolStyle,
		int symbolSize,
		int textBcg,
		string textFilters,
		int textSize)
	{
		settings[$"Crc.{cls}.Symbol.bcg"] = symbolBcg.ToString();
		settings[$"Crc.{cls}.Symbol.filters"] = symbolFilters;
		settings[$"Crc.{cls}.Symbol.style"] = symbolStyle;
		settings[$"Crc.{cls}.Symbol.size"] = symbolSize.ToString();

		// No "Crc.<cls>.Text.text": every fix supplies its own label from its identifier, and the
		// parser warns if one is supplied here.
		settings[$"Crc.{cls}.Text.bcg"] = textBcg.ToString();
		settings[$"Crc.{cls}.Text.filters"] = textFilters;
		settings[$"Crc.{cls}.Text.size"] = textSize.ToString();
		settings[$"Crc.{cls}.Text.underline"] = "N";
		settings[$"Crc.{cls}.Text.xOffset"] = "0";
		settings[$"Crc.{cls}.Text.yOffset"] = "0";
		settings[$"Crc.{cls}.Text.opaque"] = "N";
	}

	/// <summary>
	/// Adds the <c>Crc.Wx.Symbol.*</c> and <c>Crc.Wx.Text.*</c> property defaults. These values are
	/// the harness's own; the GUI starts every CRC box empty and makes the user choose.
	/// </summary>
	/// <param name="settings">The dictionary being built.</param>
	/// <param name="symbolBcg">Symbol BCG group, 1-40.</param>
	/// <param name="symbolFilters">Symbol filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="symbolStyle">Symbol style, one of <c>CrcPropertyValidator.ValidSymbolStyles</c>.</param>
	/// <param name="symbolSize">Symbol size, 1-4.</param>
	/// <param name="textBcg">Text BCG group, 1-40.</param>
	/// <param name="textFilters">Text filters, comma-separated, each 0-40, at least one.</param>
	/// <param name="textSize">Text size, 0-5.</param>
	private static void AddWxStationCrcDefaults(
		Dictionary<string, string> settings,
		int symbolBcg,
		string symbolFilters,
		string symbolStyle,
		int symbolSize,
		int textBcg,
		string textFilters,
		int textSize)
	{
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Symbol.bcg"] = symbolBcg.ToString();
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Symbol.filters"] = symbolFilters;
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Symbol.style"] = symbolStyle;
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Symbol.size"] = symbolSize.ToString();

		// No "Crc.Wx.Text.text": every station supplies its own label from its ICAO ID, IATA ID
		// and site, and the parser warns if one is supplied here.
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Text.bcg"] = textBcg.ToString();
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Text.filters"] = textFilters;
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Text.size"] = textSize.ToString();
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Text.underline"] = "N";
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Text.xOffset"] = "0";
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Text.yOffset"] = "0";
		settings[$"Crc.{WxStationOutputFiles.AllClass}.Text.opaque"] = "N";
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
