using System.Runtime.CompilerServices;

using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Infrastructure.Dtpp.Models;

using DtppAirport = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Airport;
using DtppRecord = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Record;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// A cycle's d-TPP Metafile indexed for the Procedure Charts tab: its airports by FAA and ICAO ID,
/// and its departures and arrivals by name, by root name and by airport, as the cards that list
/// procedures by name need them.
/// </summary>
/// <remarks>
/// A metafile has thousands of airports and up to twenty thousand charts, each name run through
/// <see cref="ProcedureNaming"/>, so the index is built off the UI thread, and once per metafile
/// (<see cref="For"/>).
/// </remarks>
public sealed class ProcedureNameIndex
{
	private static readonly ConditionalWeakTable<DtppMetafileDataCollection, ProcedureNameIndex> Built = new();

	private ProcedureNameIndex(
		Dictionary<string, DtppAirport> airportsByIdent,
		Dictionary<string, IReadOnlyList<string>> namesByAirport,
		Dictionary<string, string> namesByCanonical,
		Dictionary<string, IReadOnlyList<string>> namesByRoot,
		int airportCount,
		int procedureCount)
	{
		AirportsByIdent = airportsByIdent;
		NamesByAirport = namesByAirport;
		NamesByCanonical = namesByCanonical;
		NamesByRoot = namesByRoot;
		NameOptions = [.. namesByCanonical.Values.Order(StringComparer.OrdinalIgnoreCase)];
		AirportCount = airportCount;
		ProcedureCount = procedureCount;
	}

	/// <summary>An index of no metafile: every lookup misses.</summary>
	public static ProcedureNameIndex Empty { get; } = new([], [], [], [], 0, 0);

	/// <summary>Each airport, by its FAA ID and by its ICAO ID, ignoring case.</summary>
	public IReadOnlyDictionary<string, DtppAirport> AirportsByIdent { get; }

	/// <summary>Each airport's departure and arrival names (without a continuation's suffix), in chart order, by its FAA ID.</summary>
	public IReadOnlyDictionary<string, IReadOnlyList<string>> NamesByAirport { get; }

	/// <summary>Each departure and arrival name as the metafile spells it, by that name ignoring case.</summary>
	public IReadOnlyDictionary<string, string> NamesByCanonical { get; }

	/// <summary>Each root name's departures and arrivals (<c>GRUUB</c> for <c>GRUUB3</c>), as the metafile spells them.</summary>
	public IReadOnlyDictionary<string, IReadOnlyList<string>> NamesByRoot { get; }

	/// <summary>Every departure and arrival name, sorted, for the Add Procedure box's suggestions.</summary>
	public IReadOnlyList<string> NameOptions { get; }

	/// <summary>How many airports the metafile lists.</summary>
	public int AirportCount { get; }

	/// <summary>How many charts the metafile lists, a continuation page not counted.</summary>
	public int ProcedureCount { get; }

	/// <summary>
	/// The root name a typed or picked departure or arrival is listed by: from its full name
	/// (<c>BRWNZ FOUR (RNAV)</c>) or the root itself (<c>BRWNZ</c>), spelled as the FAA does - the way the
	/// run matches a pick (<see cref="ProcedureNaming.RootName"/>).
	/// </summary>
	/// <param name="typed">The text typed or picked.</param>
	/// <returns>The root name, or <see langword="null"/> when the metafile has no departure or arrival by it.</returns>
	public string? ResolveRoot(string? typed)
	{
		string trimmed = typed?.Trim() ?? string.Empty;

		if (trimmed.Length == 0)
		{
			return null;
		}

		string root = ProcedureNaming.RootName(NamesByCanonical.TryGetValue(trimmed, out string? canonical) ? canonical : trimmed);

		return NamesByRoot.TryGetValue(root, out IReadOnlyList<string>? names) ? ProcedureNaming.RootName(names[0]) : null;
	}

	/// <summary>The departures and arrivals with this root name, at one airport or at any: what a pick by it brings in.</summary>
	/// <param name="root">The root name.</param>
	/// <param name="airport">The airport (FAA or ICAO identifier), or <see langword="null"/> for any.</param>
	/// <returns>Their current names, in chart order at one airport or alphabetical at any.</returns>
	public IReadOnlyList<string> NamesFor(string root, string? airport)
	{
		ArgumentNullException.ThrowIfNull(root);

		if (airport is null)
		{
			return NamesByRoot.TryGetValue(root, out IReadOnlyList<string>? names) ? [.. names.Order(StringComparer.OrdinalIgnoreCase)] : [];
		}

		return AirportsByIdent.TryGetValue(airport, out DtppAirport? metafileAirport)
			&& NamesByAirport.TryGetValue(metafileAirport.AptIdent, out IReadOnlyList<string>? atAirport)
				? [.. atAirport.Where(name => ProcedureNaming.RootName(name).Equals(root, StringComparison.OrdinalIgnoreCase))]
				: [];
	}

	/// <summary>A metafile's index: built the first time it is asked for, then kept with the metafile.</summary>
	/// <param name="metafile">The parsed metafile.</param>
	/// <returns>The index.</returns>
	public static ProcedureNameIndex For(DtppMetafileDataCollection metafile)
	{
		ArgumentNullException.ThrowIfNull(metafile);
		return Built.GetValue(metafile, Build);
	}

	/// <summary>Builds a metafile's index.</summary>
	/// <param name="metafile">The parsed metafile.</param>
	/// <returns>The index.</returns>
	public static ProcedureNameIndex Build(DtppMetafileDataCollection metafile)
	{
		ArgumentNullException.ThrowIfNull(metafile);

		Dictionary<string, DtppAirport> airportsByIdent = new(StringComparer.OrdinalIgnoreCase);

		foreach (DtppAirport airport in metafile.Airports)
		{
			airportsByIdent.TryAdd(airport.AptIdent, airport);

			if (!string.IsNullOrWhiteSpace(airport.IcaoIdent))
			{
				airportsByIdent.TryAdd(airport.IcaoIdent!, airport);
			}
		}

		Dictionary<string, IReadOnlyList<string>> namesByAirport = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> namesByCanonical = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<string>> namesByRoot = new(StringComparer.OrdinalIgnoreCase);

		// The cards that list procedures by name take departures and arrivals only.
		foreach (IGrouping<string, DtppRecord> group in metafile.Records
			.Where(record => !ProcedureNaming.IsContinuation(record.ChartName) && ProcedureChartTypes.IsDepartureOrArrival(record.ChartCode))
			.GroupBy(record => record.AptIdent, StringComparer.OrdinalIgnoreCase))
		{
			List<string> names = [];
			HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

			foreach (DtppRecord record in group.OrderBy(record => record.ChartSeq))
			{
				string baseName = ProcedureNaming.BaseName(record.ChartName);

				if (!seen.Add(baseName))
				{
					continue;
				}

				names.Add(baseName);

				if (namesByCanonical.TryAdd(baseName, baseName))
				{
					string root = ProcedureNaming.RootName(baseName);

					if (!namesByRoot.TryGetValue(root, out List<string>? withRoot))
					{
						namesByRoot[root] = withRoot = [];
					}

					withRoot.Add(baseName);
				}
			}

			namesByAirport[group.Key] = names;
		}

		int procedureCount = metafile.Records.Count(record => !ProcedureNaming.IsContinuation(record.ChartName));

		return new ProcedureNameIndex(
			airportsByIdent,
			namesByAirport,
			namesByCanonical,
			namesByRoot.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value, StringComparer.OrdinalIgnoreCase),
			metafile.Airports.Count,
			procedureCount);
	}
}
