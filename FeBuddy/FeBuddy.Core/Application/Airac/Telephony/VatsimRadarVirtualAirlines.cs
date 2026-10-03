using System.Text.Json;

using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;

namespace FeBuddy.Core.Application.Airac.Telephony;

/// <summary>
/// The VATSIM-Radar Virtual Airline List as Telephony writes it - when the user includes it - and as
/// the Telephony tab checks a new virtual airline against it.
/// </summary>
/// <remarks>
/// <para>
/// A virtual airline on the list is written only when it could have been one of the user's own
/// (<see cref="TelephonySettingsParser.VirtualAirlineProblem"/>): a 3LD of three letters, a telephony
/// with a letter or digit, and a virtual organization. The rest - <c>C</c>, <c>PHENX</c>, ... - are
/// left out, and the run names them.
/// </para>
/// <para>
/// Each is written once, sorted by 3LD, telephony and virtual organization, so a list that only
/// changes order writes the same file. The same virtual airline means the same 3LD, telephony and
/// virtual organization, ignoring case (<see cref="VirtualAirline.IsSameAs"/>); one that differs in
/// any of the three is another virtual airline, with a card of its own.
/// </para>
/// </remarks>
public static class VatsimRadarVirtualAirlines
{
	/// <summary>What the Telephony tab and the run call the list.</summary>
	public const string ListName = "VATSIM-Radar Virtual Airline List";

	/// <summary>The virtual airlines on the list that can be written, each once and in order, and the 3LDs of those that cannot.</summary>
	/// <param name="list">The list, as <see cref="VatsimRadarAirlineParser"/> reads it.</param>
	/// <returns>What to write, and what was left out.</returns>
	public static VatsimRadarSelection Select(IEnumerable<VatsimRadarAirline> list)
	{
		ArgumentNullException.ThrowIfNull(list);

		List<VirtualAirline> usable = [];
		List<string> leftOut = [];

		foreach (VatsimRadarAirline airline in list
			.OrderBy(airline => airline.Icao, StringComparer.OrdinalIgnoreCase)
			.ThenBy(airline => airline.Callsign, StringComparer.OrdinalIgnoreCase)
			.ThenBy(airline => airline.Name, StringComparer.OrdinalIgnoreCase))
		{
			if (TelephonySettingsParser.VirtualAirlineProblem(airline.Icao, airline.Callsign, airline.Name) is not null)
			{
				leftOut.Add(airline.Icao);
			}
			else if (FindSame(usable, airline.Icao, airline.Callsign, airline.Name) is null)
			{
				usable.Add(new VirtualAirline(airline.Icao, airline.Callsign, airline.Name));
			}
		}

		return new VatsimRadarSelection(usable, leftOut);
	}

	/// <summary>The virtual airline in <paramref name="list"/> that is the same as the one given, if there is one.</summary>
	/// <param name="list">The virtual airlines to look through.</param>
	/// <param name="designator">The 3LD.</param>
	/// <param name="telephony">The telephony.</param>
	/// <param name="organization">The virtual organization.</param>
	/// <returns>The match, or <see langword="null"/>.</returns>
	public static VirtualAirline? FindSame(IEnumerable<VirtualAirline> list, string designator, string telephony, string organization)
	{
		ArgumentNullException.ThrowIfNull(list);

		return list.FirstOrDefault(virtualAirline => virtualAirline.IsSameAs(designator, telephony, organization));
	}

	/// <summary>
	/// Reads a kept copy of the list, for the Telephony tab: what a run would write from it, and when
	/// it was downloaded.
	/// </summary>
	/// <param name="path">The copy's path, e.g. <c>TelephonyFiles.VatsimRadarAirlinesFilePath</c>.</param>
	/// <returns>The copy, or <see langword="null"/> when there is none, or it cannot be read.</returns>
	public static VatsimRadarCopy? ReadKeptCopy(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		if (!File.Exists(path))
		{
			return null;
		}

		try
		{
			VatsimRadarSelection selection = Select(VatsimRadarAirlineParser.Parse(path));
			return new VatsimRadarCopy(selection.VirtualAirlines, File.GetLastWriteTimeUtc(path));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
		{
			return null;
		}
	}
}
