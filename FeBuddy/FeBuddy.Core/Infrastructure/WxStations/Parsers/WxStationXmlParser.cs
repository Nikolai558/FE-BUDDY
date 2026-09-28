using System.Globalization;
using System.Xml.Linq;

using FeBuddy.Core.Infrastructure.WxStations.Models;

namespace FeBuddy.Core.Infrastructure.WxStations.Parsers;

/// <summary>
/// Reads the aviationweather.gov Wx Stations cache file (<c>stations.cache.xml</c>).
/// </summary>
/// <remarks>
/// Unlike the NASR CSV parsers, this reads one XML file rather than columns of a delimited file,
/// so it works directly against <see cref="XDocument"/> instead of <c>NasrCsvReader</c>.
/// </remarks>
public static class WxStationXmlParser
{
	/// <summary>Reads <c>stations.cache.xml</c>.</summary>
	/// <param name="xmlPath">The full path of the file.</param>
	/// <returns>Every station the file lists, plus its reported result count.</returns>
	/// <exception cref="InvalidDataException">
	/// Thrown when the file's root is not <c>&lt;response&gt;</c>, or it has no <c>&lt;data&gt;</c>
	/// element.
	/// </exception>
	public static WxStationDataCollection Parse(string xmlPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(xmlPath);

		// Default XmlReaderSettings (DtdProcessing.Prohibit) apply here - stations.cache.xml
		// carries no DTD, and one should never be honoured from an external download.
		XDocument document = XDocument.Load(xmlPath);

		XElement root = document.Root is { Name.LocalName: "response" } responseElement
			? responseElement
			: throw new InvalidDataException($"Wx station file '{xmlPath}' does not have a <response> root element.");

		XElement dataElement = root.Element("data")
			?? throw new InvalidDataException($"Wx station file '{xmlPath}' has no <data> element under <response>.");

		List<WxStationXmlDataModel.Station> stations = [.. dataElement.Elements("Station").Select(ReadStation)];

		return new WxStationDataCollection
		{
			Stations = stations,
			NumResults = ParseNullableInt((string?)dataElement.Attribute("num_results")) ?? stations.Count,
		};
	}

	/// <summary>Async wrapper over <see cref="Parse"/>, for callers already on an async pipeline (e.g. <c>AiracCycleDataCache</c>).</summary>
	/// <param name="xmlPath">The full path of the file.</param>
	/// <param name="cancellationToken">Checked before parsing starts.</param>
	/// <returns>The parsed data.</returns>
	/// <exception cref="InvalidDataException">See <see cref="Parse"/>.</exception>
	public static Task<WxStationDataCollection> ParseAsync(string xmlPath, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.Run(() => Parse(xmlPath), cancellationToken);
	}

	private static WxStationXmlDataModel.Station ReadStation(XElement element) => new()
	{
		StationId = ReadString(element, "station_id"),
		IcaoId = ReadString(element, "icao_id"),
		IataId = ReadString(element, "iata_id"),
		FaaId = ReadString(element, "faa_id"),
		WmoId = ReadString(element, "wmo_id"),
		Latitude = ParseNullableDouble(ReadString(element, "latitude")),
		Longitude = ParseNullableDouble(ReadString(element, "longitude")),
		ElevationM = ParseNullableDouble(ReadString(element, "elevation_m")),
		Site = ReadString(element, "site"),
		State = ReadString(element, "state"),
		Country = ReadString(element, "country"),
		SiteTypes = ReadSiteTypes(element),
	};

	/// <summary>Reads a child element's text, trimmed.</summary>
	/// <returns>The trimmed value, or <see langword="null"/> when the element is missing or blank.</returns>
	private static string? ReadString(XElement station, string elementName)
	{
		string? value = station.Element(elementName)?.Value.Trim();
		return string.IsNullOrEmpty(value) ? null : value;
	}

	/// <summary>The names of <c>&lt;site_type&gt;</c>'s child elements, e.g. <c>METAR</c>, <c>TAF</c>.</summary>
	private static IReadOnlyList<string> ReadSiteTypes(XElement station)
	{
		XElement? siteType = station.Element("site_type");
		return siteType is null ? [] : [.. siteType.Elements().Select(e => e.Name.LocalName)];
	}

	private static double? ParseNullableDouble(string? value) =>
		value is not null && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
			? result
			: null;

	private static int? ParseNullableInt(string? value) =>
		value is not null && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
			? result
			: null;
}
