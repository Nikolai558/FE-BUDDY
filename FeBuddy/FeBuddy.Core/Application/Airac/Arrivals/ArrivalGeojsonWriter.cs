using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Airac.Arrivals;

/// <summary>
/// Generates the Arrivals GeoJSON output: for every airport + procedure, up to three files in
/// <c>…\Geojson\&lt;ARTCC&gt;\&lt;ARPT&gt;\</c> -
/// <c>&lt;ARPT&gt;_&lt;CODE&gt;_STAR_Lines.geojson</c> (one MultiLineString),
/// <c>_STAR_Symbols.geojson</c> and <c>_STAR_Text.geojson</c> (one Point per procedure point).
/// </summary>
/// <remarks>
/// <para>
/// A procedure that is a single point, or whose points never form a segment, gets Symbols and
/// Text but no Lines file.
/// </para>
/// <para>
/// A kind the user marked for vNAS goes under <c>Upload_to_vNAS</c> instead, and only a kind
/// chosen for CRC-ERAM defaults gets an isDefaults Feature (see <see cref="ArrivalOutputFiles"/>).
/// </para>
/// </remarks>
public static class ArrivalGeojsonWriter
{
	/// <summary>
	/// Generates every GeoJSON file called for by <paramref name="settings"/>.
	/// </summary>
	/// <param name="airportProcedures">The airport + procedure pairs in scope - already filtered by the caller.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <returns>The files written and how many rendered Features each holds.</returns>
	public static GeojsonFileSet Generate(
		IReadOnlyList<ArrivalAirportProcedure> airportProcedures,
		ArrivalSettings settings)
	{
		ArgumentNullException.ThrowIfNull(airportProcedures);
		ArgumentNullException.ThrowIfNull(settings);

		GeojsonFileSet files = new(settings.CoordinatePrecision);

		if (!settings.GenerateGeojson)
		{
			return files;
		}

		foreach (ArrivalAirportProcedure airportProcedure in airportProcedures)
		{
			if (settings.EmitLines)
			{
				GenerateLines(airportProcedure, settings, files);
			}

			if (settings.EmitSymbols)
			{
				GenerateSymbols(airportProcedure, settings, files);
			}

			if (settings.EmitText)
			{
				GenerateText(airportProcedure, settings, files);
			}
		}

		return files;
	}

	/// <summary>Writes one airport + procedure's file of one kind, into the GeoJSON or vNAS folder as the user chose.</summary>
	private static void WriteFile(
		FeatureCollection collection,
		int renderedCount,
		ArrivalAirportProcedure airportProcedure,
		ArrivalSettings settings,
		CrcFeatureKind kind,
		GeojsonFileSet files) =>
		files.Write(
			collection,
			renderedCount,
			ArrivalOutputFiles.GeojsonDirectory(settings, airportProcedure, kind),
			ArrivalOutputFiles.GeojsonFileName(airportProcedure, kind));

	private static void GenerateLines(
		ArrivalAirportProcedure airportProcedure,
		ArrivalSettings settings,
		GeojsonFileSet files)
	{
		MultiLineString? geometry = ArrivalGeometryBuilder.Build(airportProcedure);

		if (geometry is null)
		{
			return;
		}

		FeatureCollection collection = [];

		if (settings.Vnas.HasCrcDefaults(ArrivalOutputFiles.Lines))
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.LineDefaults[ArrivalCrcClass.Arrivals]));
		}

		// One Feature for the whole procedure at this airport, so a controller sees it as a
		// single object however many transitions and bodies it has.
		AttributesTable attributes = [];
		AddFebProperties(attributes, airportProcedure, settings, point: null);
		collection.Add(new Feature(geometry, attributes));

		WriteFile(collection, 1, airportProcedure, settings, CrcFeatureKind.Line, files);
	}

	private static void GenerateSymbols(
		ArrivalAirportProcedure airportProcedure,
		ArrivalSettings settings,
		GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.Vnas.HasCrcDefaults(ArrivalOutputFiles.Symbols))
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.SymbolDefaults[ArrivalCrcClass.Arrivals]));
		}

		foreach (ArrivalPoint point in airportProcedure.Points)
		{
			AttributesTable attributes = [];
			AddFebProperties(attributes, airportProcedure, settings, point);
			collection.Add(new Feature(CreatePoint(point), attributes));
		}

		WriteFile(collection, airportProcedure.Points.Count, airportProcedure, settings, CrcFeatureKind.Symbol, files);
	}

	private static void GenerateText(
		ArrivalAirportProcedure airportProcedure,
		ArrivalSettings settings,
		GeojsonFileSet files)
	{
		FeatureCollection collection = [];

		if (settings.Vnas.HasCrcDefaults(ArrivalOutputFiles.Text))
		{
			collection.Add(CrcFeatureFactory.CreateDefaultsFeature(settings.TextDefaults[ArrivalCrcClass.Arrivals]));
		}

		foreach (ArrivalPoint point in airportProcedure.Points)
		{
			AttributesTable attributes = new()
			{
				{ "text", new[] { point.Id } }
			};
			AddFebProperties(attributes, airportProcedure, settings, point);
			collection.Add(new Feature(CreatePoint(point), attributes));
		}

		WriteFile(collection, airportProcedure.Points.Count, airportProcedure, settings, CrcFeatureKind.Text, files);
	}

	private static Point CreatePoint(ArrivalPoint point) =>
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
		ArrivalAirportProcedure airportProcedure,
		ArrivalSettings settings,
		ArrivalPoint? point)
	{
		FebProperties.Add(attributes, settings.IncludeFebCustomProperties, settings.FebProperties, property =>
			ValueFor(airportProcedure, property, point));
	}

	private static object? ValueFor(
		ArrivalAirportProcedure airportProcedure,
		ArrivalFebProperty property,
		ArrivalPoint? point) => property switch
		{
			ArrivalFebProperty.ArrivalName => airportProcedure.Procedure.ArrivalName,
			ArrivalFebProperty.PointId => point?.Id,
			ArrivalFebProperty.ArptId => airportProcedure.AirportId,
			ArrivalFebProperty.Artcc => airportProcedure.Artcc,
			ArrivalFebProperty.AmendmentNo => NullIfEmpty(airportProcedure.Procedure.AmendmentNo),
			ArrivalFebProperty.AmendEffDate => NullIfEmpty(airportProcedure.Procedure.AmendmentEffectiveDateText),
			ArrivalFebProperty.Waypoints => point is null ? airportProcedure.Points.Select(p => p.Id).ToArray() : null,
			_ => null,
		};

	private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
