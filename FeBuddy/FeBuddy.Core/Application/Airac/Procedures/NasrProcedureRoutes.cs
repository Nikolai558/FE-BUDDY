using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// The first route source for the procedures-with-a-fix-in-my-facility rule (Feature #367): NASR's
/// departures (<c>DP_BASE</c>, <c>DP_APT</c>, <c>DP_RTE</c>) and arrivals (<c>STAR_BASE</c>,
/// <c>STAR_APT</c>, <c>STAR_RTE</c>), each as a <see cref="ProcedureRoute"/>.
/// </summary>
/// <remarks>
/// A departure is known by its computer code and name together - obstacle departures with no code
/// all share <c>NOT ASSIGNED</c> - and an arrival by its code alone, which <c>STAR_RTE</c> carries
/// without the name. Either kind serves the airports its <c>SERVED_ARPT</c> lists and those its
/// <c>_APT</c> rows name. NASR has no approach routes: they will come from the FAA CIFP, as another
/// source of the same <see cref="ProcedureRoute"/>s.
/// </remarks>
public static class NasrProcedureRoutes
{
	private const string NotAssigned = "NOT ASSIGNED";

	private static readonly string[] DepartureChartCodes = [ProcedureChartTypes.Dp, ProcedureChartTypes.Odp];
	private static readonly string[] ArrivalChartCodes = [ProcedureChartTypes.Star];

	/// <summary>Every departure and arrival in <paramref name="nasr"/>, with its route.</summary>
	/// <param name="nasr">Parsed NASR data with its <c>DP</c> and <c>STAR</c> groups.</param>
	/// <returns>The departures, then the arrivals, in file order.</returns>
	/// <exception cref="InvalidOperationException">The <c>DP</c> or <c>STAR</c> group was not parsed.</exception>
	public static IReadOnlyList<ProcedureRoute> Read(NasrCsvDataCollection nasr)
	{
		ArgumentNullException.ThrowIfNull(nasr);

		DpCsvDataCollection dp = nasr.RequireDp();
		StarCsvDataCollection star = nasr.RequireStar();
		List<ProcedureRoute> routes = new(dp.DpBase.Count + star.StarBase.Count);

		ILookup<(string, string), DpCsvDataModel.DpRte> departurePoints = dp.DpRte.ToLookup(row => DepartureKey(row.DpComputerCode, row.DpName));
		ILookup<(string, string), DpCsvDataModel.DpApt> departureAirports = dp.DpApt.ToLookup(row => DepartureKey(row.DpComputerCode, row.DpName));

		foreach (DpCsvDataModel.DpBase departure in dp.DpBase)
		{
			(string, string) key = DepartureKey(departure.DpComputerCode, departure.DpName);

			routes.Add(new ProcedureRoute(
				DepartureChartCodes,
				ComputerCode(departure.DpComputerCode),
				Trim(departure.DpName),
				Airports(departure.ServedArpt, departureAirports[key].Select(row => row.ArptId)),
				[.. departurePoints[key].Select(row => Point(row.Point, row.PointType, row.IcaoRegionCode))]));
		}

		ILookup<string, StarCsvDataModel.StarRte> arrivalPoints = star.StarRte.ToLookup(row => Trim(row.StarComputerCode), StringComparer.OrdinalIgnoreCase);
		ILookup<string, StarCsvDataModel.StarApt> arrivalAirports = star.StarApt.ToLookup(row => Trim(row.StarComputerCode), StringComparer.OrdinalIgnoreCase);

		foreach (StarCsvDataModel.StarBase arrival in star.StarBase)
		{
			string key = Trim(arrival.StarComputerCode);

			routes.Add(new ProcedureRoute(
				ArrivalChartCodes,
				ComputerCode(arrival.StarComputerCode),
				Trim(arrival.ArrivalName),
				Airports(arrival.ServedArpt, arrivalAirports[key].Select(row => row.ArptId)),
				[.. arrivalPoints[key].Select(row => Point(row.Point, row.PointType, row.IcaoRegionCode))]));
		}

		return routes;
	}

	private static (string Code, string Name) DepartureKey(string? code, string? name) =>
		(Trim(code).ToUpperInvariant(), Trim(name).ToUpperInvariant());

	/// <summary>The computer code, or <see langword="null"/> for <c>NOT ASSIGNED</c> or blank.</summary>
	private static string? ComputerCode(string? code) =>
		Trim(code) is { Length: > 0 } trimmed && !trimmed.Contains(NotAssigned, StringComparison.OrdinalIgnoreCase) ? trimmed : null;

	/// <summary><c>SERVED_ARPT</c>'s airports (comma- or space-separated) and the <c>_APT</c> rows' together, each once.</summary>
	private static HashSet<string> Airports(string? servedAirports, IEnumerable<string?> aptRows)
	{
		HashSet<string> airports = new(
			(servedAirports ?? string.Empty).Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
			StringComparer.OrdinalIgnoreCase);

		airports.UnionWith(aptRows.Select(Trim).Where(id => id.Length > 0));
		return airports;
	}

	private static ProcedureRoutePoint Point(string? id, string? type, string? region) =>
		new(Trim(id), Trim(type), string.IsNullOrWhiteSpace(region) ? null : region.Trim());

	private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}
