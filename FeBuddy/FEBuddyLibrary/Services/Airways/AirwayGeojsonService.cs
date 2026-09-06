using FEBuddyLibrary.Models.Geojson;
using FEBuddyLibrary.Models.Services.Airways;
using FEBuddyLibrary.Services.General;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FEBuddyLibrary.Services.Airways;

/// <summary>
/// Generates the Airways GeoJSON output files (Lines, Symbols, Text) from a built list of
/// <see cref="Airway"/> objects, grouped per <see cref="AirwaySettings.OutputBy"/>.
/// </summary>
public static class AirwayGeojsonService
{
	/// <summary>
	/// Maps a NASR <c>FROM_PT_TYPE</c> value to the CRC symbol <c>style</c> it should render
	/// as. Any type not listed here (including <see langword="null"/>, for the final,
	/// type-less point of an airway - see <c>AirwayBuilder.ResolveOrderedPoints</c>) falls
	/// back to "airwayIntersections".
	/// </summary>
	private static readonly Dictionary<string, string> SymbolStyleByPointType =
		new(StringComparer.OrdinalIgnoreCase)
		{
			["NDB"] = "ndb",
			["NDB/DME"] = "ndb",
			["MARINE NDB"] = "ndb",
			["MARINE NDB/DME"] = "ndb",
			["UHF/NDB"] = "ndb",

			["VOR"] = "vor",
			["VOR/DME"] = "vor",
			["VORTAC"] = "vor",
			["DME"] = "vor",
			["TACAN"] = "vor",
			["VOT"] = "vor",
			["CONSOLAN"] = "vor",
		};

	private const string DefaultSymbolStyle = "airwayIntersections";

	/// <summary>
	/// Generates every Lines/Symbols/Text file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="airways">The airways to render (already built and, if applicable, ROI-clipped/buffered).</param>
	/// <param name="settings">The parsed Airways settings.</param>
	/// <returns>Which files were written, their rendered feature counts, and any warnings.</returns>
	public static AirwayGeojsonGenerateResult Generate(IReadOnlyList<Airway> airways, AirwaySettings settings)
	{
		ArgumentNullException.ThrowIfNull(airways);
		ArgumentNullException.ThrowIfNull(settings);

		List<string> filesWritten = new();
		Dictionary<string, int> renderedCounts = new();
		List<string> warnings = new();

		if (settings.OutputBy == AirwayGeojsonOutputBy.None || airways.Count == 0)
		{
			return new AirwayGeojsonGenerateResult(filesWritten, renderedCounts, warnings);
		}

		string geojsonDirectory = Path.Combine(settings.OutputDirectory, "FE-Buddy_Output", "Airways", "Geojson");

		foreach (var group in GroupAirways(airways, settings.OutputBy))
		{
			string filePrefix = $"Airways_{group.Key}";

			AirwayAltitudeClass referenceClass = DetermineReferenceClass(group.Value);

			List<Airway> orderedAirways =
				group.Value.OrderBy(a => a.AwyId, StringComparer.OrdinalIgnoreCase).ToList();

			GenerateLines(orderedAirways, referenceClass, settings, geojsonDirectory, filePrefix, filesWritten, renderedCounts);
			GenerateSymbolsAndText(orderedAirways, referenceClass, settings, geojsonDirectory, filePrefix, filesWritten, renderedCounts);
		}

		return new AirwayGeojsonGenerateResult(filesWritten, renderedCounts, warnings);
	}

	/// <summary>
	/// Groups airways by altitude class ("High"/"Low"/"Other") or by designation, per
	/// <paramref name="outputBy"/>.
	/// </summary>
	private static IEnumerable<KeyValuePair<string, List<Airway>>> GroupAirways(
		IReadOnlyList<Airway> airways,
		AirwayGeojsonOutputBy outputBy)
	{
		Dictionary<string, List<Airway>> groups = new(StringComparer.OrdinalIgnoreCase);

		foreach (Airway airway in airways)
		{
			string key = outputBy == AirwayGeojsonOutputBy.HighLow
				? airway.AltitudeClass.ToString()
				: string.IsNullOrWhiteSpace(airway.AwyDesignation)
					? "Unknown"
					: airway.AwyDesignation.Trim().ToUpperInvariant();

			if (!groups.TryGetValue(key, out List<Airway>? list))
			{
				list = new List<Airway>();
				groups[key] = list;
			}

			list.Add(airway);
		}

		return groups
			.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
			.Select(kvp => kvp);
	}

	/// <summary>
	/// Determines which altitude class's CRC defaults should be used as a group's isDefaults:
	/// the majority class among its airways, with ties broken High &gt; Low &gt; Other for
	/// determinism.
	/// </summary>
	private static AirwayAltitudeClass DetermineReferenceClass(IReadOnlyList<Airway> group)
	{
		return group
			.GroupBy(a => a.AltitudeClass)
			.OrderByDescending(g => g.Count())
			.ThenBy(g => (int)g.Key)
			.First()
			.Key;
	}

	private static void GenerateLines(
		IReadOnlyList<Airway> airways,
		AirwayAltitudeClass referenceClass,
		AirwaySettings settings,
		string directory,
		string filePrefix,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcEramPropertyDefaults)
		{
			collection.Add(CrcEramPropertyHandler.CreateDefault(CrcFeatureKind.Line, settings.LineDefaults[referenceClass]));
		}

		int renderedCount = 0;

		foreach (Airway airway in airways)
		{
			AttributesTable attributes;

			bool needsOverride =
				settings.IncludeCrcEramPropertyDefaults &&
				settings.OutputBy == AirwayGeojsonOutputBy.Designation &&
				airway.AltitudeClass != referenceClass;

			attributes = needsOverride
				? CrcEramPropertyHandler.CreateFeatureProperty(CrcFeatureKind.Line, settings.LineDefaults[airway.AltitudeClass])
				: new AttributesTable();

			if (settings.IncludeFebCustomProperties)
			{
				attributes.Add("feb.AwyId", airway.AwyId);

				if (settings.IncludeAirwayWaypointIds)
				{
					attributes.Add("feb.AwyWaypoints", airway.Points.Select(p => p.PointId).ToArray());
				}
			}

			collection.Add(new Feature(airway.Geometry, attributes));
			renderedCount++;
		}

		string? path = GeojsonFileWriter.Write(collection, renderedCount, directory, $"{filePrefix}_Lines.geojson");

		if (path is not null)
		{
			filesWritten.Add(path);
			renderedCounts[path] = renderedCount;
		}
	}

	private static void GenerateSymbolsAndText(
		IReadOnlyList<Airway> airways,
		AirwayAltitudeClass referenceClass,
		AirwaySettings settings,
		string directory,
		string filePrefix,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		// De-duplicate waypoints across every airway in this group; first occurrence wins.
		Dictionary<string, AirwayPoint> uniquePoints = new(StringComparer.OrdinalIgnoreCase);

		foreach (Airway airway in airways)
		{
			foreach (AirwayPoint point in airway.Points)
			{
				uniquePoints.TryAdd(point.PointId, point);
			}
		}

		IEnumerable<AirwayPoint> pointsToRender = uniquePoints.Values;

		if (settings.Roi is not null)
		{
			pointsToRender = pointsToRender.Where(p => RoiFilter.Contains(settings.Roi, p.Latitude, p.Longitude));
		}

		List<AirwayPoint> orderedPoints =
			pointsToRender.OrderBy(p => p.PointId, StringComparer.OrdinalIgnoreCase).ToList();

		GenerateSymbols(orderedPoints, referenceClass, settings, directory, filePrefix, filesWritten, renderedCounts);
		GenerateText(orderedPoints, referenceClass, settings, directory, filePrefix, filesWritten, renderedCounts);
	}

	private static void GenerateSymbols(
		IReadOnlyList<AirwayPoint> points,
		AirwayAltitudeClass referenceClass,
		AirwaySettings settings,
		string directory,
		string filePrefix,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcEramPropertyDefaults)
		{
			collection.Add(CrcEramPropertyHandler.CreateDefault(CrcFeatureKind.Symbol, settings.SymbolDefaults[referenceClass]));
		}

		foreach (AirwayPoint point in points)
		{
			AttributesTable attributes = new();
			attributes.Add("style", MapSymbolStyle(point.PointType));

			Feature feature = new(
				AirwayGeometryBuilder.GeometryFactory.CreatePoint(new Coordinate(point.Longitude, point.Latitude)),
				attributes);

			collection.Add(feature);
		}

		string? path = GeojsonFileWriter.Write(collection, points.Count, directory, $"{filePrefix}_Symbols.geojson");

		if (path is not null)
		{
			filesWritten.Add(path);
			renderedCounts[path] = points.Count;
		}
	}

	private static void GenerateText(
		IReadOnlyList<AirwayPoint> points,
		AirwayAltitudeClass referenceClass,
		AirwaySettings settings,
		string directory,
		string filePrefix,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcEramPropertyDefaults)
		{
			collection.Add(CrcEramPropertyHandler.CreateDefault(CrcFeatureKind.Text, settings.TextDefaults[referenceClass]));
		}

		foreach (AirwayPoint point in points)
		{
			AttributesTable attributes = new();
			attributes.Add("text", new[] { point.PointId });

			Feature feature = new(
				AirwayGeometryBuilder.GeometryFactory.CreatePoint(new Coordinate(point.Longitude, point.Latitude)),
				attributes);

			collection.Add(feature);
		}

		string? path = GeojsonFileWriter.Write(collection, points.Count, directory, $"{filePrefix}_Text.geojson");

		if (path is not null)
		{
			filesWritten.Add(path);
			renderedCounts[path] = points.Count;
		}
	}

	/// <summary>
	/// Maps a NASR <c>FROM_PT_TYPE</c> to its CRC symbol style, defaulting to
	/// "airwayIntersections" for any unrecognized or missing type.
	/// </summary>
	private static string MapSymbolStyle(string? pointType)
	{
		if (pointType is not null && SymbolStyleByPointType.TryGetValue(pointType.Trim(), out string? style))
		{
			return style;
		}

		return DefaultSymbolStyle;
	}
}
