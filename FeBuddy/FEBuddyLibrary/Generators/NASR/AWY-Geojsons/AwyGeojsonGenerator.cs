using FEBuddyLibrary.Handlers.CSV;
using FEBuddyLibrary.Models.NASR.CSV;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using System.Text.Json;

namespace FEBuddyLibrary.Generators.NASR;

/// <summary>
/// Generates GeoJSON airway geometry from parsed NASR AWY CSV data.
/// </summary>
public static partial class AwyGeojsonGenerator
{
	private static readonly GeometryFactory GeometryFactory =
		NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

	/// <summary>
	/// Generates an RFC 7946 GeoJSON FeatureCollection containing NASR airways.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data.</param>
	/// <param name="outputDirectory">Directory where the GeoJSON file will be written.</param>
	/// <param name="fileName">Name of the generated GeoJSON file.</param>
	/// <returns>The full path to the generated GeoJSON file.</returns>
	public static string Generate(
		NasrCsvDataCollection allNasrCsvData,
		string outputDirectory,
		string fileName = "AWY.geojson")
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		if (string.IsNullOrWhiteSpace(outputDirectory))
		{
			throw new ArgumentException(
				"Output directory cannot be null, empty, or whitespace.",
				nameof(outputDirectory));
		}

		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentException(
				"File name cannot be null, empty, or whitespace.",
				nameof(fileName));
		}

		if (allNasrCsvData.Awy is null)
		{
			throw new InvalidOperationException(
				"AWY NASR CSV data has not been parsed.");
		}

		Directory.CreateDirectory(outputDirectory);

		FeatureCollection featureCollection = new();

		/*
		 * Build a dictionary of all airway IDs from AWY_BASE.
		 *
		 * Key:
		 *      AwyId
		 *
		 * Value:
		 *      AwyDesignation
		 *
		 * AwyDesignation is retained for future generator updates,
		 * but is not currently written to the GeoJSON properties.
		 *
		 * GroupBy is used so duplicate AWY_BASE records with the same
		 * AwyId do not cause ToDictionary() to throw an exception.
		 */
		Dictionary<string, string> airways =
			allNasrCsvData.Awy.AwyBase
				.Where(x => !string.IsNullOrWhiteSpace(x.AwyId))
				.GroupBy(
					x => x.AwyId.Trim(),
					StringComparer.OrdinalIgnoreCase)
				.ToDictionary(
					group => group.Key,
					group => group.First().AwyDesignation,
					StringComparer.OrdinalIgnoreCase);

		/*
		 * Group AWY_SEG_ALT records by airway ID once.
		 */
		ILookup<string, AwyCsvDataModel.AwySegAlt> airwaySegments =
			allNasrCsvData.Awy.AwySegAlt
				.Where(x => !string.IsNullOrWhiteSpace(x.AwyId))
				.ToLookup(
					x => x.AwyId.Trim(),
					StringComparer.OrdinalIgnoreCase);

		foreach (KeyValuePair<string, string> airway in airways)
		{
			string awyId = airway.Key;

			// Retained for future use.
			string awyType = airway.Value;

			/*
			 * POINT_SEQ defines the order of the airway segment records.
			 */
			List<AwyCsvDataModel.AwySegAlt> rawSegments =
				airwaySegments[awyId]
					.OrderBy(x => x.PointSeq)
					.ToList();

			/*
			 * Normalize the raw AWY_SEG_ALT records before attempting
			 * coordinate lookups.
			 *
			 * Records whose FromPtType is null/empty are treated as
			 * reference-only points and are collapsed out.
			 */
			List<AirwaySegment> normalizedSegments =
				NormalizeAirwaySegments(rawSegments);

			List<LineString> lineStrings =
				BuildAirwayLineStrings(
					allNasrCsvData,
					awyId,
					normalizedSegments);

			/*
			 * No usable segment geometry means there is nothing to
			 * write for this airway.
			 */
			if (lineStrings.Count == 0)
			{
				continue;
			}

			Geometry geometry =
				lineStrings.Count == 1
					? lineStrings[0]
					: GeometryFactory.CreateMultiLineString(
						lineStrings.ToArray());

			AttributesTable properties = new();

			properties.Add(
				"feb_AWY-ID",
				awyId);

			Feature feature = new(
				geometry,
				properties);

			featureCollection.Add(feature);
		}

		JsonSerializerOptions jsonOptions = new()
		{
			WriteIndented = true
		};

		jsonOptions.Converters.Add(
			new GeoJsonConverterFactory());

		string geoJson = JsonSerializer.Serialize(
			featureCollection,
			jsonOptions);

		string outputPath = Path.Combine(
			outputDirectory,
			fileName);

		File.WriteAllText(
			outputPath,
			geoJson);

		return outputPath;
	}
}