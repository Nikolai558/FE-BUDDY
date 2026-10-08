using System.Text.Json;

using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.Core.Infrastructure.Telephony.Parsers;

/// <summary>
/// Reads GNG's fictional airlines (<see cref="TelephonyFiles.GngAirlinesUrl"/>): a JSON object whose
/// <c>rows</c> array holds every airline, each
/// <c>{ "icao", "airline", "callsign", "country", "rl_exists", "in_use", "proof", "addedbyvacc", "prim" }</c>,
/// and whose <c>records</c> says how many there are. Only <c>icao</c>, <c>airline</c> (the virtual
/// organization) and <c>callsign</c> (the telephony) are read; every row is a virtual airline.
/// </summary>
/// <remarks>
/// <para>
/// The whole list comes on one page, so a reply whose <c>rows</c> don't number its <c>records</c> -
/// one page of several, or cut off - is refused, and so is one of fewer than <see cref="MinimumRows"/>
/// rows: GNG has hundreds. Either way the download counts as failed and the last good copy is kept.
/// </para>
/// <para>
/// GNG lists some 3LDs more than once (<c>SKA</c> three times, each another airline): every row is
/// kept. A row missing its <c>icao</c>, <c>airline</c> or <c>callsign</c> - or with one that is not
/// text, or blank - is skipped. Whether a row can become a card is the Telephony sub-service's call.
/// </para>
/// </remarks>
public static class GngAirlineParser
{
	/// <summary>The fewest rows a copy may have: GNG has hundreds, so fewer means something went wrong.</summary>
	public const int MinimumRows = 100;

	/// <summary>Reads the airlines from a copy of GNG's list.</summary>
	/// <param name="path">The copy's path.</param>
	/// <returns>Every row with all three values, trimmed, in the list's order.</returns>
	/// <exception cref="JsonException">The file is not JSON.</exception>
	/// <exception cref="InvalidDataException">The file is JSON, but not the whole of GNG's list.</exception>
	public static IReadOnlyList<VatsimRadarAirline> Parse(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		using FileStream stream = File.OpenRead(path);
		using JsonDocument document = JsonDocument.Parse(stream);

		JsonElement root = document.RootElement;

		if (root.ValueKind != JsonValueKind.Object
			|| !root.TryGetProperty("rows", out JsonElement rows) || rows.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidDataException($"'{path}' is not GNG's airline list: it has no array of rows.");
		}

		int count = rows.GetArrayLength();

		if (!root.TryGetProperty("records", out JsonElement records)
			|| records.ValueKind != JsonValueKind.Number
			|| !records.TryGetInt32(out int recordCount)
			|| recordCount != count)
		{
			throw new InvalidDataException($"'{path}' is not the whole of GNG's airline list: it has {count:N0} rows where it says it has {Describe(records)}.");
		}

		if (count < MinimumRows)
		{
			throw new InvalidDataException($"'{path}' has only {count:N0} airlines, where GNG's list has hundreds.");
		}

		List<VatsimRadarAirline> airlines = [];

		foreach (JsonElement row in rows.EnumerateArray())
		{
			if (row.ValueKind == JsonValueKind.Object
				&& AirlineJson.Text(row, "icao") is { } icao
				&& AirlineJson.Text(row, "airline") is { } name
				&& AirlineJson.Text(row, "callsign") is { } callsign)
			{
				airlines.Add(new VatsimRadarAirline(icao, name, callsign));
			}
		}

		return airlines;
	}

	/// <summary>What <c>records</c> holds, for the message: a number, or that there is none.</summary>
	private static string Describe(JsonElement records) =>
		records.ValueKind == JsonValueKind.Number ? records.GetRawText() : "no number of records";
}
