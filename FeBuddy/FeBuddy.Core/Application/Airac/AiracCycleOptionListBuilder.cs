using System.Runtime.CompilerServices;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Domain.Airways;
using FeBuddy.Core.Domain.ArtccBoundaries;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Domain.Fixes;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// Works out the options the AIRAC Service's tabs offer for a cycle (<see cref="AiracCycleOptionLists"/>)
/// from its NASR data, by the same rules the builders apply to it: which values a table has, how
/// they are spelled, what is left out and in what order they are shown.
/// </summary>
/// <remarks>
/// A full cycle takes a noticeable fraction of a second (tens of thousands of fixes and airports),
/// so it runs off the UI thread, and once per cycle: <see cref="For"/> keeps the lists with the data.
/// </remarks>
public static class AiracCycleOptionListBuilder
{
	private static readonly ConditionalWeakTable<NasrCsvDataCollection, AiracCycleOptionLists> Built = new();

	/// <summary>A cycle's lists: built the first time they are asked for, then kept with its data.</summary>
	/// <param name="data">The cycle's parsed NASR data.</param>
	/// <returns>The lists.</returns>
	public static AiracCycleOptionLists For(NasrCsvDataCollection data)
	{
		ArgumentNullException.ThrowIfNull(data);
		return Built.GetValue(data, Build);
	}

	/// <summary>Builds a cycle's lists. A table <paramref name="data"/> doesn't hold gives an empty list.</summary>
	/// <param name="data">The cycle's parsed NASR data.</param>
	/// <returns>The lists.</returns>
	public static AiracCycleOptionLists Build(NasrCsvDataCollection data)
	{
		ArgumentNullException.ThrowIfNull(data);

		(IReadOnlyList<(string Token, string Name)> charts, bool hasFixOnNoChart) = Charts(data);
		(IReadOnlyDictionary<string, string> faaIds, IReadOnlyDictionary<string, AptCsvDataModel.AptBase> airports) = Airports(data);

		return new AiracCycleOptionLists
		{
			AirwayDesignations = Sorted((data.Awy?.AwyBase ?? []).Select(airway => AirwayClassifier.DeriveDesignation(airway.AwyId))),

			// STAR_BASE.ARTCC can name several centres, space-separated, for a STAR they share.
			StarArtccs = Sorted((data.Star?.StarBase ?? []).SelectMany(star => (star.Artcc ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))),
			DepartureArtccs = Sorted((data.Dp?.DpBase ?? []).Select(departure => departure.Artcc)),
			NavaidTypes = InKnownOrder(
				(data.Nav?.NavBase ?? []).Where(navaid => !NavaidBuilder.IsShutDown(navaid)).Select(navaid => Normalize(navaid.NavType)),
				Domain.Navaids.NavaidTypes.All),
			BoundaryLocations = Sorted((data.Arb?.ArbSeg ?? []).Select(segment => segment.LocationId)),
			BoundaryLocationAltitudes = BoundaryAltitudes(data),
			FixUseNames = InKnownOrder((data.Fix?.FixBase ?? []).Select(fix => FixUses.Name(fix.FixUseCode)), FixUses.All),
			FixCharts = charts,
			HasFixOnNoChart = hasFixOnNoChart,
			AirportFacilities = Sorted((data.Apt?.AptBase ?? []).Select(airport => airport.RespArtccId)),
			FacilityNames = FacilityNames(data),
			AirportFaaIdsByIdent = faaIds,
			AirportsByFaaId = airports,
		};
	}

	/// <summary>Each responsible ARTCC once, the first airport's spelling, with its name; by "name (ID)", ignoring case.</summary>
	private static (string ArtccId, string Name)[] FacilityNames(NasrCsvDataCollection data) =>
		[.. (data.Apt?.AptBase ?? [])
			.Where(airport => !string.IsNullOrWhiteSpace(airport.RespArtccId))
			.Select(airport => (ArtccId: airport.RespArtccId.Trim(),
				Name: string.IsNullOrWhiteSpace(airport.ArtccName) ? airport.RespArtccId.Trim() : airport.ArtccName.Trim()))
			.DistinctBy(facility => facility.ArtccId, StringComparer.OrdinalIgnoreCase)
			.OrderBy(facility => $"{facility.Name} ({facility.ArtccId})", StringComparer.OrdinalIgnoreCase)];

	/// <summary>Trimmed, upper-cased, blanks dropped, each once, sorted ignoring case.</summary>
	private static string[] Sorted(IEnumerable<string?> values) =>
		[.. values
			.Select(Normalize)
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Order(StringComparer.OrdinalIgnoreCase)];

	/// <summary>Each value once: those <paramref name="known"/> lists first, in its order, then the rest by name.</summary>
	private static string[] InKnownOrder(IEnumerable<string> values, IReadOnlyList<string> known) =>
		[.. values
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => KnownIndex(value, known))
			.ThenBy(value => value, StringComparer.OrdinalIgnoreCase)];

	private static int KnownIndex(string value, IReadOnlyList<string> known)
	{
		for (int i = 0; i < known.Count; i++)
		{
			if (known[i].Equals(value, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}

		return int.MaxValue;
	}

	private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

	private static (string LocationId, string Altitude)[] BoundaryAltitudes(NasrCsvDataCollection data)
	{
		HashSet<(string, ArtccBoundaryAltitude)> seen = [];
		List<(string LocationId, ArtccBoundaryAltitude Altitude)> pairs = [];

		foreach (ArbCsvDataModel.ArbSeg segment in data.Arb?.ArbSeg ?? [])
		{
			string location = Normalize(segment.LocationId);

			if (location.Length > 0
				&& ArtccBoundaryAltitudes.TryParse(segment.Altitude, out ArtccBoundaryAltitude altitude)
				&& seen.Add((location, altitude)))
			{
				pairs.Add((location, altitude));
			}
		}

		return [.. pairs
			.OrderBy(pair => pair.LocationId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(pair => pair.Altitude)
			.Select(pair => (pair.LocationId, ArtccBoundaryAltitudes.NasrToken(pair.Altitude)))];
	}

	private static ((string Token, string Name)[] Charts, bool HasFixOnNoChart) Charts(NasrCsvDataCollection data)
	{
		Dictionary<string, string> nameByToken = new(StringComparer.OrdinalIgnoreCase);
		bool hasFixOnNoChart = false;

		foreach (FixCsvDataModel.FixBase fix in data.Fix?.FixBase ?? [])
		{
			IReadOnlyList<string> charts = FixCharts.Parse(fix.Charts);

			if (charts.Count == 0)
			{
				hasFixOnNoChart = true;
				continue;
			}

			foreach (string chart in charts)
			{
				nameByToken.TryAdd(FixCharts.Token(chart), chart);
			}
		}

		return ([.. nameByToken.OrderBy(chart => chart.Value, StringComparer.OrdinalIgnoreCase).Select(chart => (chart.Key, chart.Value))], hasFixOnNoChart);
	}

	private static (Dictionary<string, string> FaaIdsByIdent, Dictionary<string, AptCsvDataModel.AptBase> ByFaaId) Airports(NasrCsvDataCollection data)
	{
		Dictionary<string, string> faaIdsByIdent = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, AptCsvDataModel.AptBase> byFaaId = new(StringComparer.OrdinalIgnoreCase);

		foreach (AptCsvDataModel.AptBase airport in data.Apt?.AptBase ?? [])
		{
			string faaId = (airport.ArptId ?? string.Empty).Trim();

			if (faaId.Length == 0)
			{
				continue;
			}

			faaIdsByIdent.TryAdd(faaId, faaId);
			byFaaId.TryAdd(faaId, airport);

			string icao = (airport.IcaoId ?? string.Empty).Trim();

			if (icao.Length > 0)
			{
				faaIdsByIdent.TryAdd(icao, faaId);
			}
		}

		return (faaIdsByIdent, byFaaId);
	}
}
