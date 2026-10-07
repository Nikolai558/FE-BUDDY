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
/// type, ARTCC, longest runway, elevation, pattern altitude, FSS, CTAF, weather, attendance
/// hours, and the class airspace with its hours).
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
///     one physical line per command; CRC turns the escapes into a multi-line display, with a
///     blank line above the card and a line break after it (<see cref="EchoCards"/>).
///   </item>
/// </list>
/// <para>
/// The attendance hours (<c>ATNDCE HRS:</c>) and the airspace hours (<c>HRS:</c>, under
/// <c>AIRSPACE:</c>) can run to several lines: the first sits beside its label and the rest line
/// up under it. Both labels are written even when there are no hours - as for a field with no
/// tower, which is given no attendance hours.
/// </para>
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
		int commandCount = 0;

		// Every FAA ID is claimed before any ICAO command is written, so an airport's own FAA ID
		// always beats another airport's ICAO ID, whatever the sort order.
		Dictionary<string, Airport> ownerOf = new(StringComparer.OrdinalIgnoreCase);

		foreach (Airport airport in airports)
		{
			ownerOf.TryAdd(airport.FaaId, airport);
		}

		List<(string Identifier, Airport Skipped, Airport Owner)> clashes = [];

		foreach (Airport airport in airports.OrderBy(a => a.FaaId, StringComparer.OrdinalIgnoreCase))
		{
			string body = BuildCommandBody(airport);

			if (ReferenceEquals(ownerOf[airport.FaaId], airport))
			{
				Append(builder, airport.FaaId, body);
				commandCount++;
			}
			else
			{
				clashes.Add((airport.FaaId, airport, ownerOf[airport.FaaId]));
			}

			// The ICAO command repeats the identical body under the second identifier, so
			// .aptKSEA and .aptSEA both work and show the same card.
			if (DistinctIcaoId(airport) is not { } icaoId)
			{
				continue;
			}

			if (ownerOf.TryAdd(icaoId, airport))
			{
				Append(builder, icaoId, body);
				commandCount++;
			}
			else
			{
				clashes.Add((icaoId, airport, ownerOf[icaoId]));
			}
		}

		ReportClashes(clashes, messages);

		if (commandCount == 0)
		{
			return new AirportAliasGenerateResult(null, 0, messages);
		}

		string directory = AiracOutputPaths.AliasDirectory(settings.OutputDirectory);
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, settings.FileNames.FileName(AirportOutputFiles.Alias));

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
		string displayLine = DistinctIcaoId(airport) is { } icaoId
			? $"{airport.FaaId} - {icaoId}"
			: airport.FaaId;

		StringBuilder body = new();

		string tab2 = TabEscape + TabEscape;
		string tab3 = tab2 + TabEscape;
		string tab4 = tab3 + TabEscape;
		string space3 = SpaceEscape + SpaceEscape + SpaceEscape;

		body.Append(NewLineEscape);
		body.Append("APT:").Append(tab3).Append(displayLine).Append(NewLineEscape);
		body.Append(tab4).Append(airport.Name).Append(NewLineEscape);
		body.Append(tab4).Append(airport.FacilityType).Append(NewLineEscape);
		body.Append("FAC").Append(SpaceEscape).Append("TYPE:").Append(TabEscape).Append(space3).Append(airport.TowerType).Append(NewLineEscape);
		body.Append("ARTCC:").Append(tab2).Append(SpaceEscape).Append(SpaceEscape).Append(airport.RespArtccId).Append(NewLineEscape);
		body.Append("LONGEST").Append(SpaceEscape).Append("RWY:").Append(TabEscape).Append(FormatLongestRunway(airport)).Append(NewLineEscape);
		body.Append(tab4).Append(airport.LongestRunway?.SurfaceType ?? string.Empty).Append(NewLineEscape);
		body.Append("ELEV:").Append(tab2).Append(space3).Append(FormatFeet(airport.Elevation)).Append(NewLineEscape);
		body.Append("PTRN").Append(SpaceEscape).Append("ALT:").Append(TabEscape).Append(space3).Append(FormatFeet(airport.TrafficPatternAltitude)).Append(NewLineEscape);
		body.Append("FSS:").Append(tab3).Append(airport.FssId ?? string.Empty).Append(NewLineEscape);
		body.Append("CTAF:").Append(tab2).Append(space3).Append(airport.CtafFrequency ?? string.Empty).Append(NewLineEscape);
		body.Append("WX:").Append(tab3).Append(SpaceEscape).Append(FormatWeather(airport)).Append(NewLineEscape);
		AppendLines(body, "ATNDCE" + SpaceEscape + "HRS:" + TabEscape + SpaceEscape, airport.AttendanceHours, tab4);
		body.Append(NewLineEscape);
		body.Append("AIRSPACE:").Append(TabEscape).Append(space3).Append(airport.ClassAirspace ?? string.Empty).Append(NewLineEscape);
		AppendLines(body, TabEscape + SpaceEscape + "HRS:" + TabEscape + space3, airport.AirspaceHours, tab4);

		return ".ECHO " + EchoCards.Join([body.ToString()]);
	}

	/// <summary>
	/// Appends a label with the first of <paramref name="lines"/> beside it and each of the rest on
	/// a line of its own under it. The label is written even when there are no lines, so every card
	/// has the same rows. No line break follows the last line.
	/// </summary>
	/// <param name="body">The command body being built.</param>
	/// <param name="label">The label, escapes included, up to where its first value starts.</param>
	/// <param name="lines">The values, one per display line.</param>
	/// <param name="indent">What starts each line after the first, lining it up under the first value.</param>
	private static void AppendLines(StringBuilder body, string label, IReadOnlyList<string> lines, string indent)
	{
		body.Append(label).Append(lines.Count > 0 ? lines[0] : string.Empty);

		foreach (string line in lines.Skip(1))
		{
			body.Append(NewLineEscape).Append(indent).Append(line);
		}
	}

	/// <summary>
	/// The airport's ICAO ID when it is a second identifier, or <see langword="null"/>. Many
	/// airports outside the US have their ICAO ID as their FAA ID too (<c>CYAM</c>/<c>CYAM</c>),
	/// and those get one command and one identifier on the card.
	/// </summary>
	private static string? DistinctIcaoId(Airport airport) =>
		airport.IcaoId is { } icaoId && !string.Equals(icaoId, airport.FaaId, StringComparison.OrdinalIgnoreCase)
			? icaoId
			: null;

	private static void Append(StringBuilder builder, string identifier, string body) =>
		builder.Append(".apt").Append(identifier).Append(' ').Append(body).AppendLine();

	/// <summary>
	/// Reports identifiers two airports both claim as one warning, with each skipped command as a
	/// routine message under it. None occur in the FAA's data today; this keeps a future clash
	/// visible without filling the Review tab.
	/// </summary>
	private static void ReportClashes(List<(string Identifier, Airport Skipped, Airport Owner)> clashes, List<ServiceMessage> messages)
	{
		const int Listed = 5;

		if (clashes.Count == 0)
		{
			return;
		}

		IEnumerable<string> examples = clashes.Take(Listed).Select(c => $".apt{c.Identifier} (kept for {c.Owner.FaaId})");
		string list = string.Join(", ", examples) + (clashes.Count > Listed ? $" and {clashes.Count - Listed} more" : string.Empty);

		string summary = clashes.Count == 1
			? $"1 airport alias command was skipped because another airport already uses it: {list}."
			: $"{clashes.Count} airport alias commands were skipped because other airports already use them: {list}.";

		messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, summary));

		foreach ((string identifier, Airport skipped, Airport owner) in clashes)
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				$"Airport '{skipped.FaaId}': alias command '.apt{identifier}' is already used by airport '{owner.FaaId}', so it was skipped."));
		}
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
