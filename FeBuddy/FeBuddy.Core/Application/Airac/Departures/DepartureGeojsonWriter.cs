using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Geojson;

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
	/// <summary>
	/// Generates every GeoJSON file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="airportProcedures">The airport + procedure pairs in scope - already filtered by the caller.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <returns>The files written and how many rendered Features each holds.</returns>
	public static GeojsonFileSet Generate(
		IReadOnlyList<DepartureAirportProcedure> airportProcedures,
		DepartureSettings settings)
	{
		ArgumentNullException.ThrowIfNull(airportProcedures);
		ArgumentNullException.ThrowIfNull(settings);

		GeojsonFileSet files = new(settings.CoordinatePrecision);

		if (!settings.GenerateGeojson)
		{
			return files;
		}

		foreach (DepartureAirportProcedure airportProcedure in airportProcedures)
		{
			string directory = DepartureOutputPaths.GeojsonDirectory(settings, airportProcedure);

			if (settings.EmitLines)
			{
				GenerateLines(airportProcedure, settings, directory, files);
			}

			if (settings.EmitSymbols)
			{
				GenerateSymbols(airportProcedure, settings, directory, files);
			}

			if (settings.EmitText)
			{
				GenerateText(airportProcedure, settings, directory, files);
			}
		}

		return files;
	}

	private static void GenerateLines(
		DepartureAirportProcedure airportProcedure,
		DepartureSettings settings,
		string directory,
		GeojsonFileSet files)
	{
		MultiLineString? geometry = DepartureGeometryBuilder.Build(airportProcedure);

		if (geometry is null)
		{
			return;
		}

		FeatureCollection collection = [];

		if (settings.IncludeCrcLineDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.LineDefaults[DepartureCrcClass.Departures]));
		}

		// One Feature for the whole procedure at this airport, so a controller sees it as a
		// single object however many bodies and transitions it has.
		AttributesTable attributes = [];
		AddFebProperties(attributes, airportProcedure, settings, point: null);
		collection.Add(new Feature(geometry, attributes));

		files.Write(collection, 1, directory, DepartureOutputPaths.GeojsonFileName(airportProcedure, "Lines"));
	}

	private static void GenerateSymbols(
		DepartureAirportProcedure airportProcedure,
		DepartureSettings settings,
		string directory,
		GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.IncludeCrcSymbolDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.SymbolDefaults[DepartureCrcClass.Departures]));
		}

		foreach (DeparturePoint point in airportProcedure.Points)
		{
			AttributesTable attributes = [];
			AddFebProperties(attributes, airportProcedure, settings, point);
			collection.Add(new Feature(CreatePoint(point), attributes));
		}

		files.Write(collection, airportProcedure.Points.Count, directory,
			DepartureOutputPaths.GeojsonFileName(airportProcedure, "Symbols"));
	}

	private static void GenerateText(
		DepartureAirportProcedure airportProcedure,
		DepartureSettings settings,
		string directory,
		GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.IncludeCrcTextDefaults)
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.TextDefaults[DepartureCrcClass.Departures]));
		}

		foreach (DeparturePoint point in airportProcedure.Points)
		{
			AttributesTable attributes = new()
			{
				{ "text", new[] { point.Id } }
			};
			AddFebProperties(attributes, airportProcedure, settings, point);
			collection.Add(new Feature(CreatePoint(point), attributes));
		}

		files.Write(collection, airportProcedure.Points.Count, directory,
			DepartureOutputPaths.GeojsonFileName(airportProcedure, "Text"));
	}

	private static Point CreatePoint(DeparturePoint point) =>
		Wgs84.Point(point.Latitude, point.Longitude);

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
		FebProperties.Add(attributes, settings.IncludeFebCustomProperties, settings.FebProperties, property =>
			ValueFor(airportProcedure, property, point));
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

	private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
