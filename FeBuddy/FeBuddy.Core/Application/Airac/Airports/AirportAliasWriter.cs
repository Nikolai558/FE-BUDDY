using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Airports;

/// <summary>
/// Generates the <c>Airports.txt</c> alias file: an <c>.ECHO</c> command per airport that
/// prints a single-screen summary of the field in CRC (identifiers, name, facility and tower
/// type, ARTCC, longest runway, elevation, pattern altitude, airspace, FSS, CTAF and weather).
/// </summary>
/// <remarks>
/// <para>
/// Two details of CRC's own alias parser shape this file, and both are easy to break by
/// "tidying" the output:
/// </para>
/// <list type="bullet">
///   <item>
///     CRC tokenizes an alias's replacement text on whitespace and rejoins it with single
///     spaces, so a run of literal spaces collapses to one. Column alignment therefore comes
///     from <c>\t</c> escapes - two characters, a backslash and a <c>t</c> - never from padding
///     with spaces.
///   </item>
///   <item>
///     For the same reason the line breaks are literal <c>\n</c> escapes. The file itself is
///     one physical line per command; CRC turns the escapes into a multi-line display.
///   </item>
/// </list>
/// <para>
/// The file covers the entire airport database. Unlike the GeoJSON output it is deliberately
/// never ROI-filtered: a controller typing <c>.aptXXX</c> expects an answer for any field, not
/// only the ones near their own airspace.
/// </para>
/// </remarks>
public static class AirportAliasWriter
{
	private const string LogSource = "AirportAliasWriter";

	/// <summary>The literal two-character escape CRC expands into a line break.</summary>
	private const string NewLineEscape = @"\n";

	/// <summary>The literal two-character escape CRC expands into a tab stop.</summary>
	private const string TabEscape = @"\t";

	/// <summary>
	/// The literal two-character escape CRC expands into a single space.
	/// </summary>
	/// <remarks>
	/// Every space that has to survive into the display is written this way. CRC tokenizes an
	/// alias's replacement text on whitespace and rejoins it with single spaces, so real spaces
	/// cannot be relied on to hold a column - even inside a label like <c>FAC TYPE:</c>.
	/// </remarks>
	private const string SpaceEscape = @"\s";

	/// <summary>The prime symbol used as the feet marker (U+2032), written as UTF-8.</summary>
	private const string FeetMark = "′";

	/// <summary>
	/// Writes the alias file for every built airport.
	/// </summary>
	/// <param name="airports">Every airport built for this cycle, ROI-independent.</param>
	/// <param name="settings">The parsed Airports settings.</param>
	/// <returns>The path written (or <see langword="null"/> when there was nothing to write), the command count, and any messages.</returns>
	public static AirportAliasGenerateResult Generate(IReadOnlyList<Airport> airports, AirportSettings settings)
	{
		ArgumentNullException.ThrowIfNull(airports);
		ArgumentNullException.ThrowIfNull(settings);

		List<ServiceMessage> messages = [];

		if (airports.Count == 0)
		{
			return new AirportAliasGenerateResult(null, 0, messages);
		}

		StringBuilder builder = new();
		HashSet<string> writtenCommands = new(StringComparer.OrdinalIgnoreCase);
		int commandCount = 0;

		foreach (Airport airport in airports.OrderBy(a => a.FaaId, StringComparer.OrdinalIgnoreCase))
		{
			string body = BuildCommandBody(airport);

			if (TryAppend(builder, writtenCommands, airport.FaaId, body, airport, messages))
			{
				commandCount++;
			}

			// The ICAO command repeats the identical body under the second identifier, so
			// .aptKSEA and .aptSEA both work and show the same card.
			if (airport.IcaoId is { } icaoId
				&& TryAppend(builder, writtenCommands, icaoId, body, airport, messages))
			{
				commandCount++;
			}
		}

		if (commandCount == 0)
		{
			return new AirportAliasGenerateResult(null, 0, messages);
		}

		// The output folder itself, or Upload_to_vNAS when the user marked the file for vNAS.
		string directory = AiracOutputPaths.FileDirectory(
			settings.OutputDirectory, isGeojson: false, settings.Vnas.IsUploaded(AirportOutputFiles.Alias));
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, AirportOutputFiles.Alias);

		// UTF-8 without a BOM: CRC reads the file with File.ReadAllLines, which decodes UTF-8
		// by default, and the feet marker is non-ASCII.
		File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		return new AirportAliasGenerateResult(path, commandCount, messages);
	}

	/// <summary>
	/// Builds the replacement text shared by an airport's FAA and ICAO commands.
	/// </summary>
	/// <param name="airport">The airport to describe.</param>
	/// <returns>The <c>.ECHO</c> body, escapes included.</returns>
	internal static string BuildCommandBody(Airport airport)
	{
		string displayLine = airport.IcaoId is { } icaoId
			? $"{airport.FaaId} - {icaoId}"
			: airport.FaaId;

		StringBuilder body = new();

		string tab2 = TabEscape + TabEscape;
		string tab3 = tab2 + TabEscape;
		string tab4 = tab3 + TabEscape;
		string space3 = SpaceEscape + SpaceEscape + SpaceEscape;

		body.Append(".ECHO ").Append(NewLineEscape);
		body.Append("APT:").Append(tab3).Append(displayLine).Append(NewLineEscape);
		body.Append(tab4).Append(airport.Name).Append(NewLineEscape);
		body.Append(tab4).Append(airport.FacilityType).Append(NewLineEscape);
		body.Append("FAC").Append(SpaceEscape).Append("TYPE:").Append(TabEscape).Append(space3).Append(airport.TowerType).Append(NewLineEscape);
		body.Append("ARTCC:").Append(tab2).Append(SpaceEscape).Append(SpaceEscape).Append(airport.RespArtccId).Append(NewLineEscape);
		body.Append("LONGEST").Append(SpaceEscape).Append("RWY:").Append(TabEscape).Append(FormatLongestRunway(airport)).Append(NewLineEscape);
		body.Append(tab4).Append(airport.LongestRunway?.SurfaceType ?? string.Empty).Append(NewLineEscape);
		body.Append("ELEV:").Append(tab2).Append(space3).Append(FormatFeet(airport.Elevation)).Append(NewLineEscape);
		body.Append("PTRN").Append(SpaceEscape).Append("ALT:").Append(TabEscape).Append(space3).Append(FormatFeet(airport.TrafficPatternAltitude)).Append(NewLineEscape);
		body.Append("AIRSPACE:").Append(TabEscape).Append(space3).Append(airport.ClassAirspace ?? string.Empty).Append(NewLineEscape);
		body.Append("FSS:").Append(tab3).Append(airport.FssId ?? string.Empty).Append(NewLineEscape);
		body.Append("CTAF:").Append(tab2).Append(space3).Append(airport.CtafFrequency ?? string.Empty).Append(NewLineEscape);
		body.Append("WX:").Append(tab3).Append(SpaceEscape).Append(FormatWeather(airport));

		return body.ToString();
	}

	private static bool TryAppend(
		StringBuilder builder,
		HashSet<string> writtenCommands,
		string identifier,
		string body,
		Airport airport,
		List<ServiceMessage> messages)
	{
		string command = $".apt{identifier}";

		if (!writtenCommands.Add(command))
		{
			// Not expected to occur in real NASR data; caught so a future identifier clash
			// surfaces as a message instead of two aliases silently fighting over one command.
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"Airport '{airport.FaaId}': alias command '{command}' was already written by another airport and was skipped."));
			return false;
		}

		builder.Append(command).Append(' ').Append(body).AppendLine();
		return true;
	}

	/// <summary>
	/// Formats the longest-runway line, e.g. <c>16L/34R (11901&#x2032;)</c>. The length and its
	/// parentheses are dropped entirely when the airport has no runway, rather than printing
	/// empty brackets.
	/// </summary>
	/// <param name="airport">The airport.</param>
	/// <returns>The formatted value, or an empty string.</returns>
	private static string FormatLongestRunway(Airport airport)
	{
		if (airport.LongestRunway is not { } runway)
		{
			return string.Empty;
		}

		return $"{runway.RunwayId} ({runway.Length.ToString(CultureInfo.InvariantCulture)}{FeetMark})";
	}

	/// <summary>
	/// Formats the weather frequency and its use, e.g. <c>135.075 (ASOS)</c>. With no frequency
	/// the whole value is empty; with a frequency but no use, the parentheses are dropped.
	/// </summary>
	/// <param name="airport">The airport.</param>
	/// <returns>The formatted value, or an empty string.</returns>
	private static string FormatWeather(Airport airport)
	{
		if (airport.WeatherFrequency is not { } frequency)
		{
			return string.Empty;
		}

		return airport.WeatherFrequencyUse is { } use
			? $"{frequency} ({use})"
			: frequency;
	}

	/// <summary>Formats a value in feet with its marker, or an empty string when absent.</summary>
	/// <param name="feet">The value, or <see langword="null"/>.</param>
	/// <returns>e.g. <c>433&#x2032;</c>, or an empty string - never a bare marker.</returns>
	private static string FormatFeet(double? feet) =>
		feet is { } value
			? value.ToString("0.#", CultureInfo.InvariantCulture) + FeetMark
			: string.Empty;
}
