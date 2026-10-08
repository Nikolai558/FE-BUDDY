using System.Text.Json;

using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony;
using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Parsers;

namespace FeBuddy.Core.Application.Airac.Telephony;

/// <summary>
/// The virtual airline list - GNG's fictional airlines with VATSIM-Radar's own added, the list
/// VATSIM-Radar shows - as Telephony writes it when the user includes it, and as the Telephony tab
/// checks a new virtual airline against it.
/// </summary>
/// <remarks>
/// <para>
/// The two parts are merged the way VATSIM-Radar merges them (<see cref="Merge"/>). A virtual airline
/// on the list is then written only when it could have been one of the user's own
/// (<see cref="TelephonySettingsParser.VirtualAirlineProblem"/>): a 3LD of three letters, a telephony
/// with a letter or digit, and a virtual organization. The rest - <c>C</c>, <c>PHENX</c>, ... - are
/// left out, and the run names them.
/// </para>
/// <para>
/// Each is listed once, sorted by 3LD, telephony and virtual organization, so a list that only
/// changes order writes the same file; the same 3LD, telephony and virtual organization, ignoring
/// case, is the same virtual airline (<see cref="VirtualAirline.IsSameAs"/>). The run then writes
/// only the first operator for each 3LD and telephony (see <see cref="TelephonyBuilder"/>), so one
/// with a real operator's, or the user's own, is left out there.
/// </para>
/// </remarks>
public static class VatsimRadarVirtualAirlines
{
	/// <summary>What the Telephony tab and the run call the list.</summary>
	public const string ListName = "Virtual Airline List (GNG + VATSIM-Radar)";

	/// <summary>
	/// Merges the two parts as VATSIM-Radar does (<c>server/routes/airlines/all.ts</c>): every GNG row,
	/// then each of VATSIM-Radar's virtual airlines - taking over the name and telephony of the first GNG
	/// row with its 3LD (trimmed, ignoring case), or added at the end when GNG has none.
	/// </summary>
	/// <param name="gng">GNG's rows, in order, as <see cref="GngAirlineParser"/> reads them; duplicates and all.</param>
	/// <param name="vatsimRadar">VATSIM-Radar's virtual airlines, as <see cref="VatsimRadarAirlineParser"/> reads them.</param>
	/// <returns>The merged list.</returns>
	public static List<VatsimRadarAirline> Merge(IEnumerable<VatsimRadarAirline> gng, IEnumerable<VatsimRadarAirline> vatsimRadar)
	{
		ArgumentNullException.ThrowIfNull(gng);
		ArgumentNullException.ThrowIfNull(vatsimRadar);

		List<VatsimRadarAirline> merged = [.. gng];
		int gngCount = merged.Count;

		foreach (VatsimRadarAirline airline in vatsimRadar)
		{
			int index = merged.FindIndex(0, gngCount, row => row.Icao.Trim().Equals(airline.Icao.Trim(), StringComparison.OrdinalIgnoreCase));

			if (index >= 0)
			{
				merged[index] = merged[index] with { Name = airline.Name, Callsign = airline.Callsign };
			}
			else
			{
				merged.Add(airline);
			}
		}

		return merged;
	}

	/// <summary>The virtual airlines on the list that can be written, each once and in order, and the 3LDs of those that cannot.</summary>
	/// <param name="list">The merged list (<see cref="Merge"/>).</param>
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
	/// The virtual airline in <paramref name="list"/> with the same 3LD and telephony as the ones given,
	/// whatever its virtual organization - the one the run would write only once.
	/// </summary>
	/// <param name="list">The virtual airlines to look through.</param>
	/// <param name="designator">The 3LD.</param>
	/// <param name="telephony">The telephony.</param>
	/// <returns>The first match, or <see langword="null"/>.</returns>
	public static VirtualAirline? FindSameCallSign(IEnumerable<VirtualAirline> list, string designator, string telephony)
	{
		ArgumentNullException.ThrowIfNull(list);

		return list.FirstOrDefault(virtualAirline => virtualAirline.HasSameCallSign(designator, telephony));
	}

	/// <summary>
	/// Reads FE-Buddy's kept copies of the list, for the Telephony tab: what a run would write from them,
	/// and when they were downloaded.
	/// </summary>
	/// <returns>The list, or <see langword="null"/> when there is no copy that can be read.</returns>
	public static VatsimRadarCopy? ReadKeptCopy() =>
		ReadKeptCopy(TelephonyFiles.GngAirlinesFilePath, TelephonyFiles.VatsimRadarAirlinesFilePath, TelephonyFiles.OlderVatsimRadarAirlinesFilePath);

	/// <summary>
	/// Same as <see cref="ReadKeptCopy()"/>, with the copies' paths supplied. Each part is read on its
	/// own; one that is missing or can't be read is left out. Only while neither part can be read is the
	/// older single copy read instead.
	/// </summary>
	/// <param name="gngPath">GNG's part, or <see langword="null"/> for none.</param>
	/// <param name="vatsimRadarPath">VATSIM-Radar's part, or <see langword="null"/> for none.</param>
	/// <param name="olderCopyPath">The older single copy, or <see langword="null"/> for none.</param>
	/// <returns>The list, or <see langword="null"/> when there is no copy that can be read.</returns>
	public static VatsimRadarCopy? ReadKeptCopy(string? gngPath, string? vatsimRadarPath, string? olderCopyPath)
	{
		(IReadOnlyList<VatsimRadarAirline> Airlines, DateTime DownloadedUtc)? gng = TryRead(gngPath, GngAirlineParser.Parse);
		(IReadOnlyList<VatsimRadarAirline> Airlines, DateTime DownloadedUtc)? vatsimRadar = TryRead(vatsimRadarPath, VatsimRadarAirlineParser.Parse);

		if (gng is not null || vatsimRadar is not null)
		{
			VatsimRadarSelection selection = Select(Merge(gng?.Airlines ?? [], vatsimRadar?.Airlines ?? []));
			DateTime oldest = new[] { gng?.DownloadedUtc, vatsimRadar?.DownloadedUtc }.OfType<DateTime>().Min();

			return new VatsimRadarCopy(selection.VirtualAirlines, oldest)
			{
				GngDownloadedUtc = gng?.DownloadedUtc,
				VatsimRadarDownloadedUtc = vatsimRadar?.DownloadedUtc,
			};
		}

		return TryRead(olderCopyPath, VatsimRadarAirlineParser.Parse) is { } older
			? new VatsimRadarCopy(Select(older.Airlines).VirtualAirlines, older.DownloadedUtc) { IsOlderCopy = true }
			: null;
	}

	/// <summary>A kept copy's airlines and download time, or <see langword="null"/> when there is none, or it can't be read.</summary>
	private static (IReadOnlyList<VatsimRadarAirline> Airlines, DateTime DownloadedUtc)? TryRead(
		string? path,
		Func<string, IReadOnlyList<VatsimRadarAirline>> parse)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			return null;
		}

		try
		{
			return (parse(path), File.GetLastWriteTimeUtc(path));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
		{
			return null;
		}
	}
}
