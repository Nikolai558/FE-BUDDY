using FeBuddy.Core.Models.Geojson;
using FeBuddy.Core.Models.Services.Airac.Airways;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Services.Airac.Airways;

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
		List<ServiceMessage> messages = new();

		if (settings.OutputBy == AirwayGeojsonOutputBy.None || airways.Count == 0)
		{
			return new AirwayGeojsonGenerateResult(filesWritten, renderedCounts, messages);
		}

		string geojsonDirectory = AirwayOutputPaths.Resolve(settings, "Geojson");

		foreach (var group in GroupAirways(airways, settings.OutputBy))
		{
			string filePrefix = $"Airways_{group.Key}";

			AirwayAltitudeClass referenceClass = DetermineReferenceClass(group.Value);

			List<Airway> orderedAirways =
				group.Value.OrderBy(a => a.AwyId, StringComparer.OrdinalIgnoreCase).ToList();

			if (settings.EmitLines)
			{
				GenerateLines(orderedAirways, referenceClass, settings, geojsonDirectory, filePrefix, filesWritten, renderedCounts);
			}

			GenerateSymbolsAndText(orderedAirways, referenceClass, settings, geojsonDirectory, filePrefix, filesWritten, renderedCounts);
		}

		return new AirwayGeojsonGenerateResult(filesWritten, renderedCounts, messages);
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
				: airway.Designation;

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

		if (settings.IncludeCrcLineDefaults)
		{
			collection.Add(CrcEramPropertyHandler.CreateDefault(settings.LineDefaults[referenceClass]));
		}

		int renderedCount = 0;

		foreach (Airway airway in airways)
		{
			AttributesTable attributes;

			bool needsOverride =
				settings.IncludeCrcLineDefaults &&
				settings.OutputBy == AirwayGeojsonOutputBy.Designation &&
				airway.AltitudeClass != referenceClass;

			attributes = needsOverride
				? CrcEramPropertyHandler.CreateFeatureProperty(CrcFeatureKind.Line, settings.LineDefaults[airway.AltitudeClass].ToFeatureProperties())
				: new AttributesTable();

			// A Lines Feature is the whole airway: it carries the airway's own ID and its
			// ordered point list, never a single point's ID.
			AddFebProperties(attributes, settings, property => property switch
			{
				AirwayFebProperty.AwyId => airway.AwyId,
				AirwayFebProperty.Waypoints => airway.Points.Select(p => p.PointId).ToArray(),
				_ => null,
			});

			collection.Add(new Feature(airway.Geometry, attributes));
			renderedCount++;
		}

		string? path = GeojsonFileWriter.Write(collection, renderedCount, directory, $"{filePrefix}_Lines.geojson", settings.CoordinatePrecision);

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
		// Alongside, note every airway that uses each point: a shared point is written once,
		// so its feb.awyId has to name all of them.
		Dictionary<string, AirwayPoint> uniquePoints = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, HashSet<string>> airwayIdSetsByPoint = new(StringComparer.OrdinalIgnoreCase);

		foreach (Airway airway in airways)
		{
			foreach (AirwayPoint point in airway.Points)
			{
				uniquePoints.TryAdd(point.PointId, point);

				if (!airwayIdSetsByPoint.TryGetValue(point.PointId, out HashSet<string>? airwayIds))
				{
					airwayIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					airwayIdSetsByPoint[point.PointId] = airwayIds;
				}

				airwayIds.Add(airway.AwyId);
			}
		}

		Dictionary<string, string[]> airwayIdsByPoint = airwayIdSetsByPoint.ToDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(),
			StringComparer.OrdinalIgnoreCase);

		IEnumerable<AirwayPoint> pointsToRender = uniquePoints.Values;

		if (settings.Roi is not null)
		{
			pointsToRender = pointsToRender.Where(p => RoiFilter.Contains(settings.Roi, p.Latitude, p.Longitude));
		}

		List<AirwayPoint> orderedPoints =
			pointsToRender.OrderBy(p => p.PointId, StringComparer.OrdinalIgnoreCase).ToList();

		if (settings.EmitSymbols)
		{
			GenerateSymbols(orderedPoints, airwayIdsByPoint, referenceClass, settings, directory, filePrefix, filesWritten, renderedCounts);
		}

		if (settings.EmitText)
		{
			GenerateText(orderedPoints, airwayIdsByPoint, referenceClass, settings, directory, filePrefix, filesWritten, renderedCounts);
		}
	}

	private static void GenerateSymbols(
		IReadOnlyList<AirwayPoint> points,
		IReadOnlyDictionary<string, string[]> airwayIdsByPoint,
		AirwayAltitudeClass referenceClass,
		AirwaySettings settings,
		string directory,
		string filePrefix,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcSymbolDefaults)
		{
			collection.Add(CrcEramPropertyHandler.CreateDefault(settings.SymbolDefaults[referenceClass]));
		}

		foreach (AirwayPoint point in points)
		{
			AttributesTable attributes = new();
			attributes.Add("style", MapSymbolStyle(point.PointType));
			AddFebProperties(attributes, settings, property => property switch
			{
				AirwayFebProperty.AwyId => airwayIdsByPoint[point.PointId],
				AirwayFebProperty.PointId => point.PointId,
				_ => null,
			});

			Feature feature = new(
				AirwayGeometryBuilder.GeometryFactory.CreatePoint(new Coordinate(point.Longitude, point.Latitude)),
				attributes);

			collection.Add(feature);
		}

		string? path = GeojsonFileWriter.Write(collection, points.Count, directory, $"{filePrefix}_Symbols.geojson", settings.CoordinatePrecision);

		if (path is not null)
		{
			filesWritten.Add(path);
			renderedCounts[path] = points.Count;
		}
	}

	private static void GenerateText(
		IReadOnlyList<AirwayPoint> points,
		IReadOnlyDictionary<string, string[]> airwayIdsByPoint,
		AirwayAltitudeClass referenceClass,
		AirwaySettings settings,
		string directory,
		string filePrefix,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcTextDefaults)
		{
			collection.Add(CrcEramPropertyHandler.CreateDefault(settings.TextDefaults[referenceClass]));
		}

		foreach (AirwayPoint point in points)
		{
			AttributesTable attributes = new();
			attributes.Add("text", new[] { point.PointId });

			// No feb.pointId here: the label already is the point's ID.
			AddFebProperties(attributes, settings, property => property switch
			{
				AirwayFebProperty.AwyId => airwayIdsByPoint[point.PointId],
				_ => null,
			});

			Feature feature = new(
				AirwayGeometryBuilder.GeometryFactory.CreatePoint(new Coordinate(point.Longitude, point.Latitude)),
				attributes);

			collection.Add(feature);
		}

		string? path = GeojsonFileWriter.Write(collection, points.Count, directory, $"{filePrefix}_Text.geojson", settings.CoordinatePrecision);

		if (path is not null)
		{
			filesWritten.Add(path);
			renderedCounts[path] = points.Count;
		}
	}

	/// <summary>
	/// Adds the selected <c>feb.*</c> properties to a Feature, in <see cref="AirwayFebProperty"/>
	/// order, after its CRC attributes.
	/// </summary>
	/// <param name="attributes">The Feature's attribute table.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="valueFor">
	/// The value of a property for this Feature, or <see langword="null"/> when the property is
	/// not written on this kind of Feature.
	/// </param>
	private static void AddFebProperties(
		AttributesTable attributes,
		AirwaySettings settings,
		Func<AirwayFebProperty, object?> valueFor)
	{
		if (!settings.IncludeFebCustomProperties)
		{
			return;
		}

		foreach (AirwayFebProperty property in settings.FebProperties.OrderBy(p => p))
		{
			object? value = valueFor(property);

			if (value is not null)
			{
				attributes.Add($"feb.{Name(property)}", value);
			}
		}
	}

	/// <summary>
	/// The property name as it appears after the <c>feb.</c> prefix. Spelled out rather than
	/// derived from the enum so the JSON keys are camelCase (<c>awyId</c>, not <c>AwyId</c>).
	/// </summary>
	/// <param name="property">The property.</param>
	/// <returns>The JSON name.</returns>
	internal static string Name(AirwayFebProperty property) => property switch
	{
		AirwayFebProperty.AwyId => "awyId",
		AirwayFebProperty.PointId => "pointId",
		AirwayFebProperty.Waypoints => "waypoints",
		_ => property.ToString(),
	};

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
