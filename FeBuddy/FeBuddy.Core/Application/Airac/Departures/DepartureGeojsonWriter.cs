using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Airac.Departures;

/// <summary>
/// Generates the Departures GeoJSON output: for every airport + procedure, up to three files in
/// <c>…\Departure Procedures\&lt;ARTCC&gt;\&lt;ARPT&gt;\</c> -
/// <c>&lt;ARPT&gt;_&lt;CODE&gt;_Lines.geojson</c> (one MultiLineString),
/// <c>_Symbols.geojson</c> and <c>_Text.geojson</c> (one Point per procedure point).
/// </summary>
/// <remarks>
/// A procedure that is a single point, or whose points never form a segment, gets Symbols and
/// Text but no Lines file.
/// </remarks>
public static class DepartureGeojsonWriter
{
	private static readonly GeometryFactory GeometryFactory =
		NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

	/// <summary>
	/// Generates every GeoJSON file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="airportProcedures">The airport + procedure pairs in scope - already filtered by the caller.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <returns>Which files were written, their rendered feature counts, and any messages.</returns>
	public static DepartureGeojsonGenerateResult Generate(
		IReadOnlyList<DepartureAirportProcedure> airportProcedures,
		DepartureSettings settings)
	{
		ArgumentNullException.ThrowIfNull(airportProcedures);
		ArgumentNullException.ThrowIfNull(settings);

		List<string> filesWritten = new();
		Dictionary<string, int> renderedCounts = new();
		List<ServiceMessage> messages = new();

		if (!settings.GenerateGeojson)
		{
			return new DepartureGeojsonGenerateResult(filesWritten, renderedCounts, messages);
		}

		foreach (DepartureAirportProcedure airportProcedure in airportProcedures)
		{
			string directory = DepartureOutputPaths.GeojsonDirectory(settings, airportProcedure);

			if (settings.EmitLines)
			{
				GenerateLines(airportProcedure, settings, directory, filesWritten, renderedCounts);
			}

			if (settings.EmitSymbols)
			{
				GenerateSymbols(airportProcedure, settings, directory, filesWritten, renderedCounts);
			}

			if (settings.EmitText)
			{
				GenerateText(airportProcedure, settings, directory, filesWritten, renderedCounts);
			}
		}

		return new DepartureGeojsonGenerateResult(filesWritten, renderedCounts, messages);
	}

	private static void GenerateLines(
		DepartureAirportProcedure airportProcedure,
		DepartureSettings settings,
		string directory,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		MultiLineString? geometry = DepartureGeometryBuilder.Build(airportProcedure, GeometryFactory);

		if (geometry is null)
		{
			return;
		}

		FeatureCollection collection = new();

		if (settings.IncludeCrcLineDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefault(settings.LineDefaults[DepartureCrcClass.Departures]));
		}

		// One Feature for the whole procedure at this airport, so a controller sees it as a
		// single object however many bodies and transitions it has.
		AttributesTable attributes = new();
		AddFebProperties(attributes, airportProcedure, settings, point: null);
		collection.Add(new Feature(geometry, attributes));

		Write(collection, 1, directory, DepartureOutputPaths.GeojsonFileName(airportProcedure, "Lines"), settings, filesWritten, renderedCounts);
	}

	private static void GenerateSymbols(
		DepartureAirportProcedure airportProcedure,
		DepartureSettings settings,
		string directory,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcSymbolDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefault(settings.SymbolDefaults[DepartureCrcClass.Departures]));
		}

		foreach (DeparturePoint point in airportProcedure.Points)
		{
			AttributesTable attributes = new();
			AddFebProperties(attributes, airportProcedure, settings, point);
			collection.Add(new Feature(CreatePoint(point), attributes));
		}

		Write(collection, airportProcedure.Points.Count, directory,
			DepartureOutputPaths.GeojsonFileName(airportProcedure, "Symbols"), settings, filesWritten, renderedCounts);
	}

	private static void GenerateText(
		DepartureAirportProcedure airportProcedure,
		DepartureSettings settings,
		string directory,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		FeatureCollection collection = new();

		if (settings.IncludeCrcTextDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefault(settings.TextDefaults[DepartureCrcClass.Departures]));
		}

		foreach (DeparturePoint point in airportProcedure.Points)
		{
			AttributesTable attributes = new();
			attributes.Add("text", new[] { point.Id });
			AddFebProperties(attributes, airportProcedure, settings, point);
			collection.Add(new Feature(CreatePoint(point), attributes));
		}

		Write(collection, airportProcedure.Points.Count, directory,
			DepartureOutputPaths.GeojsonFileName(airportProcedure, "Text"), settings, filesWritten, renderedCounts);
	}

	private static Point CreatePoint(DeparturePoint point) =>
		GeometryFactory.CreatePoint(new Coordinate(point.Longitude, point.Latitude));

	/// <summary>Adds the selected <c>feb.*</c> properties to a Feature.</summary>
	/// <param name="attributes">The Feature's attribute table.</param>
	/// <param name="airportProcedure">The airport + procedure being written.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="point">
	/// The point a Symbols or Text Feature is for, which supplies <c>feb.pointId</c>; or
	/// <see langword="null"/> for the Lines Feature, which is the whole procedure and gets
	/// <c>feb.waypoints</c> instead.
	/// </param>
	internal static void AddFebProperties(
		AttributesTable attributes,
		DepartureAirportProcedure airportProcedure,
		DepartureSettings settings,
		DeparturePoint? point)
	{
		if (!settings.IncludeFebCustomProperties)
		{
			return;
		}

		foreach (DepartureFebProperty property in settings.FebProperties)
		{
			object? value = ValueFor(airportProcedure, property, point);

			if (value is not null)
			{
				attributes.Add($"feb.{Name(property)}", value);
			}
		}
	}

	private static object? ValueFor(
		DepartureAirportProcedure airportProcedure,
		DepartureFebProperty property,
		DeparturePoint? point) => property switch
	{
		DepartureFebProperty.DpName => airportProcedure.Procedure.DpName,
		DepartureFebProperty.PointId => point?.Id,
		DepartureFebProperty.ArptId => airportProcedure.AirportId,
		DepartureFebProperty.Artcc => airportProcedure.Procedure.Artcc,
		DepartureFebProperty.AmendmentNo => NullIfEmpty(airportProcedure.Procedure.AmendmentNo),
		DepartureFebProperty.AmendEffDate => NullIfEmpty(airportProcedure.Procedure.AmendmentEffectiveDateText),
		DepartureFebProperty.Waypoints => point is null ? airportProcedure.Points.Select(p => p.Id).ToArray() : null,
		_ => null,
	};

	/// <summary>
	/// The property name as it appears after the <c>feb.</c> prefix. Spelled out rather than
	/// derived from the enum so the JSON keys are camelCase (<c>dpName</c>, not <c>DpName</c>).
	/// </summary>
	/// <param name="property">The property.</param>
	/// <returns>The JSON name.</returns>
	internal static string Name(DepartureFebProperty property) => property switch
	{
		DepartureFebProperty.DpName => "dpName",
		DepartureFebProperty.PointId => "pointId",
		DepartureFebProperty.ArptId => "arptId",
		DepartureFebProperty.Artcc => "artcc",
		DepartureFebProperty.AmendmentNo => "amendmentNo",
		DepartureFebProperty.AmendEffDate => "amendEffDate",
		DepartureFebProperty.Waypoints => "waypoints",
		_ => property.ToString(),
	};

	private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

	private static void Write(
		FeatureCollection collection,
		int renderedFeatureCount,
		string directory,
		string fileName,
		DepartureSettings settings,
		List<string> filesWritten,
		Dictionary<string, int> renderedCounts)
	{
		string? path = GeojsonFileWriter.Write(
			collection, renderedFeatureCount, directory, fileName, settings.CoordinatePrecision);

		if (path is not null)
		{
			filesWritten.Add(path);
			renderedCounts[path] = renderedFeatureCount;
		}
	}
}
