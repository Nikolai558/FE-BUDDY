using System.Text.Json;

using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.Core.Infrastructure.Telephony.Parsers;

/// <summary>
/// Reads VATSIM-Radar's own airline list (<see cref="TelephonyFiles.VatsimRadarAirlinesUrl"/>): one JSON
/// array of <c>{ "icao", "name", "callsign", "virtual", "country"? }</c>. Only the virtual airlines are
/// read; the rest (RAAF, US Navy and the like) are not.
/// </summary>
/// <remarks>
/// <para>
/// Only entries marked <c>"virtual": true</c> are read. An entry missing its <c>icao</c>,
/// <c>name</c> or <c>callsign</c> - or with one that is not text, or blank - is skipped. Whether an
/// entry can become a card (a three-letter designator, a telephony with a letter or digit) is the
/// Telephony sub-service's call, not this parser's.
/// </para>
/// <para>
/// <see cref="Parse"/> also reads the one copy FE-Buddy 3.0.0-beta.4 and earlier kept
/// (<see cref="TelephonyFiles.OlderVatsimRadarAirlinesFileName"/>): beta.4's merged list from
/// <c>data.vatsim-radar.com</c>, a JSON object whose <c>virtual</c> array holds the same entries - or,
/// from beta.3 and earlier, this same GitHub array.
/// </para>
/// </remarks>
public static class VatsimRadarAirlineParser
{
	/// <summary>The array of virtual airlines in beta.4's merged list.</summary>
	private const string VirtualAirlinesProperty = "virtual";

	/// <summary>
	/// Reads the virtual airlines from a copy of VATSIM-Radar's GitHub list, as a fresh download is
	/// checked: it has to be the JSON array, with at least one virtual airline.
	/// </summary>
	/// <param name="path">The copy's path.</param>
	/// <returns>Every virtual airline with all three values, trimmed, in the list's order.</returns>
	/// <exception cref="JsonException">The file is not JSON.</exception>
	/// <exception cref="InvalidDataException">The file is JSON, but not the list, or has no virtual airline.</exception>
	public static IReadOnlyList<VatsimRadarAirline> ParseGitHubList(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		using FileStream stream = File.OpenRead(path);
		using JsonDocument document = JsonDocument.Parse(stream);

		if (document.RootElement.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidDataException($"'{path}' is not VATSIM-Radar's airline list: it is not a JSON array.");
		}

		IReadOnlyList<VatsimRadarAirline> airlines = Read(document.RootElement);

		return airlines.Count > 0
			? airlines
			: throw new InvalidDataException($"'{path}' has no virtual airlines.");
	}

	/// <summary>
	/// Reads the virtual airlines from a kept copy: VATSIM-Radar's GitHub list, or the older single copy
	/// beta.4 and earlier kept.
	/// </summary>
	/// <param name="path">The copy's path.</param>
	/// <returns>Every virtual airline with all three values, trimmed, in the list's order.</returns>
	/// <exception cref="JsonException">The file is not JSON.</exception>
	/// <exception cref="InvalidDataException">The file is JSON, but not the airline list.</exception>
	public static IReadOnlyList<VatsimRadarAirline> Parse(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		using FileStream stream = File.OpenRead(path);
		using JsonDocument document = JsonDocument.Parse(stream);

		JsonElement root = document.RootElement;
		JsonElement entries = root.ValueKind == JsonValueKind.Array ? root
			: root.ValueKind == JsonValueKind.Object
				&& root.TryGetProperty(VirtualAirlinesProperty, out JsonElement virtualAirlines)
				&& virtualAirlines.ValueKind == JsonValueKind.Array ? virtualAirlines
			: throw new InvalidDataException($"'{path}' is not the VATSIM-Radar airline list: it has no array of virtual airlines.");

		return Read(entries);
	}

	/// <summary>The virtual airlines in an array of entries.</summary>
	private static List<VatsimRadarAirline> Read(JsonElement entries)
	{
		List<VatsimRadarAirline> airlines = [];

		foreach (JsonElement entry in entries.EnumerateArray())
		{
			if (entry.ValueKind == JsonValueKind.Object
				&& entry.TryGetProperty("virtual", out JsonElement isVirtual) && isVirtual.ValueKind == JsonValueKind.True
				&& AirlineJson.Text(entry, "icao") is { } icao
				&& AirlineJson.Text(entry, "name") is { } name
				&& AirlineJson.Text(entry, "callsign") is { } callsign)
			{
				airlines.Add(new VatsimRadarAirline(icao, name, callsign));
			}
		}

		return airlines;
	}
}
