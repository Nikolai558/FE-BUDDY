using System.Text.Json;

using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.Core.Infrastructure.Telephony.Parsers;

/// <summary>
/// Reads VATSIM-Radar's <c>airlines.json</c> (<see cref="TelephonyFiles.VatsimRadarAirlinesUrl"/>):
/// a JSON array of airlines, each <c>{ "icao", "name", "callsign", "virtual", "country" }</c>.
/// </summary>
/// <remarks>
/// Only the virtual airlines (<c>"virtual": true</c>) are read; the list also holds real airlines
/// VATSIM pilots fly as, which the FAA register already has. An entry missing its <c>icao</c>,
/// <c>name</c> or <c>callsign</c> - or with one that is not text, or blank - is skipped. Whether an
/// entry can become a card (a three-letter designator, a telephony with a letter or digit) is the
/// Telephony sub-service's call, not this parser's.
/// </remarks>
public static class VatsimRadarAirlineParser
{
	/// <summary>Reads the virtual airlines from a copy of the list.</summary>
	/// <param name="path">The copy's path.</param>
	/// <returns>Every virtual airline with all three values, trimmed, in the list's order.</returns>
	/// <exception cref="JsonException">The file is not JSON.</exception>
	/// <exception cref="InvalidDataException">The file is JSON, but not an array of airlines.</exception>
	public static IReadOnlyList<VatsimRadarAirline> Parse(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		using FileStream stream = File.OpenRead(path);
		using JsonDocument document = JsonDocument.Parse(stream);

		if (document.RootElement.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidDataException($"'{path}' is not the VATSIM-Radar airline list: it is not a JSON array.");
		}

		List<VatsimRadarAirline> airlines = [];

		foreach (JsonElement entry in document.RootElement.EnumerateArray())
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
