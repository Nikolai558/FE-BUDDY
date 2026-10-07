using System.Text.Json;

using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.Core.Infrastructure.Telephony.Parsers;

/// <summary>
/// Reads VATSIM-Radar's airline list (<see cref="TelephonyFiles.VatsimRadarAirlinesUrl"/>): a JSON
/// object whose <c>virtual</c> array holds the virtual airlines, each
/// <c>{ "icao", "name", "callsign", "virtual": true }</c>. Its <c>airlines</c> array, the real
/// airlines, is not read: the FAA register already has them.
/// </summary>
/// <remarks>
/// <para>
/// Only entries marked <c>"virtual": true</c> are read. An entry missing its <c>icao</c>,
/// <c>name</c> or <c>callsign</c> - or with one that is not text, or blank - is skipped. Whether an
/// entry can become a card (a three-letter designator, a telephony with a letter or digit) is the
/// Telephony sub-service's call, not this parser's.
/// </para>
/// <para>
/// A copy kept by FE-Buddy 3.0.0-beta.3 or earlier is the list FE-Buddy used to download, GitHub's
/// <c>custom-data/airlines.json</c>: one JSON array of the same entries. It is read the same way
/// until the next download replaces it, so an offline run still has its virtual airlines.
/// </para>
/// </remarks>
public static class VatsimRadarAirlineParser
{
	/// <summary>The array of virtual airlines in the list's JSON object.</summary>
	private const string VirtualAirlinesProperty = "virtual";

	/// <summary>Reads the virtual airlines from a copy of the list.</summary>
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

		List<VatsimRadarAirline> airlines = [];

		foreach (JsonElement entry in entries.EnumerateArray())
		{
			if (entry.ValueKind == JsonValueKind.Object
				&& entry.TryGetProperty("virtual", out JsonElement isVirtual) && isVirtual.ValueKind == JsonValueKind.True
				&& Text(entry, "icao") is { } icao
				&& Text(entry, "name") is { } name
				&& Text(entry, "callsign") is { } callsign)
			{
				airlines.Add(new VatsimRadarAirline(icao, name, callsign));
			}
		}

		return airlines;
	}

	/// <summary>A property's trimmed text, or <see langword="null"/> when it is missing, not text, or blank.</summary>
	private static string? Text(JsonElement entry, string property) =>
		entry.TryGetProperty(property, out JsonElement value)
		&& value.ValueKind == JsonValueKind.String
		&& value.GetString()?.Trim() is { Length: > 0 } text
			? text
			: null;
}
