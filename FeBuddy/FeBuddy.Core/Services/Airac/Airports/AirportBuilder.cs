using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Airports;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Core.Services.Airac.Airports;

/// <summary>
/// Assembles every <see cref="Airport"/> the Airports sub-service works with, pulling from
/// <c>APT_BASE</c>, <c>APT_RWY</c>, <c>APT_RWY_END</c>, <c>FRQ</c> and <c>CLS_ARSP</c>.
/// </summary>
/// <remarks>
/// <para>
/// Everything is built once, for the whole database, before any filtering: the GeoJSON output
/// is ROI-filtered downstream, but the alias file deliberately covers every airport, so the
/// builder must not know about the ROI at all.
/// </para>
/// <para>
/// Permanently closed airports (<c>ARPT_STATUS</c> of <c>CP</c>) are the one exclusion, and it
/// applies to every output.
/// </para>
/// </remarks>
public static class AirportBuilder
{
	private const string LogSource = "AirportBuilder";
	private const string PermanentlyClosedStatus = "CP";

	/// <summary>
	/// Builds every eligible airport from the parsed NASR data.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Apt</c> must not be null.</param>
	/// <returns>The airports, ordered by FAA identifier, plus any messages collected.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Apt has not been parsed.</exception>
	public static AirportBuildAllResult BuildAll(NasrCsvDataCollection allNasrCsvData)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		if (allNasrCsvData.Apt is null)
		{
			throw new InvalidOperationException(
				"Airport data (APT) has not been parsed. The Airports sub-service cannot run without it.");
		}

		List<ServiceMessage> messages = new();

		// One row per airport. NASR keys APT_BASE on SITE_NO, not ARPT_ID, so a duplicated
		// identifier is possible in principle - and would collide in the alias file, where the
		// command IS the identifier. Keep the first and say so.
		Dictionary<string, AptCsvDataModel.AptBase> baseRows = new(StringComparer.OrdinalIgnoreCase);

		foreach (AptCsvDataModel.AptBase row in allNasrCsvData.Apt.AptBase)
		{
			if (IsPermanentlyClosed(row))
			{
				continue;
			}

			string id = row.ArptId?.Trim() ?? string.Empty;

			if (id.Length == 0)
			{
				continue;
			}

			if (!baseRows.TryAdd(id, row))
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"Airport '{id}': more than one APT_BASE record uses this identifier. The first was kept and the rest ignored."));
			}
		}

		ILookup<string, AptCsvDataModel.AptRwy> runwaysByAirport = allNasrCsvData.Apt.AptRwy
			.Where(r => IsTrueRunway(r.RwyRwyId))
			.ToLookup(r => r.ArptId?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

		ILookup<string, AptCsvDataModel.AptRwyEnd> endsByAirportAndRunway = allNasrCsvData.Apt.AptRwyEnd
			.ToLookup(e => RunwayKey(e.ArptId, e.RwyEndRwyId), StringComparer.OrdinalIgnoreCase);

		ILookup<string, FrqCsvDataModel.Frq> frequenciesByAirport = BuildFrequencyLookup(allNasrCsvData.Frq?.Frq ?? new());

		Dictionary<string, ClsArspCsvDataModel.ClsArsp> airspaceByAirport = new(StringComparer.OrdinalIgnoreCase);

		foreach (ClsArspCsvDataModel.ClsArsp row in allNasrCsvData.ClsArsp?.ClsArsp ?? new())
		{
			airspaceByAirport.TryAdd(row.ArptId?.Trim() ?? string.Empty, row);
		}

		List<Airport> airports = new(baseRows.Count);

		foreach ((string id, AptCsvDataModel.AptBase row) in baseRows)
		{
			IReadOnlyList<AirportRunway> runways = BuildRunways(id, runwaysByAirport[id], endsByAirportAndRunway, messages);

			FrqCsvDataModel.Frq? ctaf = FindFrequency(frequenciesByAirport[id], "CTAF");
			FrqCsvDataModel.Frq? weather = FindFrequency(frequenciesByAirport[id], "AWOS", "ASOS");

			airspaceByAirport.TryGetValue(id, out ClsArspCsvDataModel.ClsArsp? airspaceRow);

			airports.Add(new Airport
			{
				FaaId = id,
				IcaoId = Normalize(row.IcaoId),
				Name = row.ArptName?.Trim() ?? string.Empty,
				Latitude = row.BaseLatDecimal,
				Longitude = row.BaseLongDecimal,
				Elevation = row.Elev,
				RespArtccId = row.RespArtccId?.Trim() ?? string.Empty,
				TrafficPatternAltitude = row.Tpa,
				FssId = Normalize(row.FssId),
				TowerType = AirportFieldMaps.MapTowerType(row.TwrTypeCode),
				FacilityType = AirportFieldMaps.MapFacilityType(row.SiteTypeCode),
				CtafFrequency = Normalize(ctaf?.Freq),
				WeatherFrequency = Normalize(weather?.Freq),
				WeatherFrequencyUse = Normalize(weather?.FreqUse),
				ClassAirspace = AirportFieldMaps.BuildClassAirspace(airspaceRow),
				Runways = runways,
				LongestRunway = SelectLongestRunway(runways)
			});
		}

		airports.Sort((left, right) => string.Compare(left.FaaId, right.FaaId, StringComparison.OrdinalIgnoreCase));

		return new AirportBuildAllResult(airports, messages);
	}

	/// <summary>
	/// Whether a <c>RWY_ID</c> names a true runway. NASR lists helipads and other single-point
	/// surfaces in the same table; only a real runway carries both ends in its identifier, so
	/// the <c>/</c> is what separates them (e.g. <c>16L/34R</c> versus <c>H1</c>).
	/// </summary>
	/// <param name="runwayId">The raw <c>RWY_ID</c>.</param>
	/// <returns><see langword="true"/> for a true runway.</returns>
	public static bool IsTrueRunway(string? runwayId) => runwayId is not null && runwayId.Contains('/');

	/// <summary>
	/// Picks an airport's longest runway. Ties break on runway identifier so a cycle-to-cycle
	/// diff of the alias file never churns on two equally long runways swapping places.
	/// </summary>
	/// <param name="runways">The airport's runways.</param>
	/// <returns>The longest runway, or <see langword="null"/> when the airport has none.</returns>
	public static AirportRunway? SelectLongestRunway(IReadOnlyList<AirportRunway> runways) =>
		runways
			.OrderByDescending(r => r.Length)
			.ThenBy(r => r.RunwayId, StringComparer.Ordinal)
			.FirstOrDefault();

	private static bool IsPermanentlyClosed(AptCsvDataModel.AptBase row) =>
		(row.ArptStatus?.Trim() ?? string.Empty).Equals(PermanentlyClosedStatus, StringComparison.OrdinalIgnoreCase);

	private static IReadOnlyList<AirportRunway> BuildRunways(
		string airportId,
		IEnumerable<AptCsvDataModel.AptRwy> rows,
		ILookup<string, AptCsvDataModel.AptRwyEnd> endsByAirportAndRunway,
		List<ServiceMessage> messages)
	{
		List<AirportRunway> runways = new();

		foreach (AptCsvDataModel.AptRwy row in rows)
		{
			string runwayId = row.RwyRwyId?.Trim() ?? string.Empty;

			if (runwayId.Length == 0)
			{
				continue;
			}

			// Two rows per runway, one per end, told apart by RWY_END_ID. Ordering by that ID
			// makes "first" and "second" deterministic rather than file-order dependent.
			List<AptCsvDataModel.AptRwyEnd> endRows = endsByAirportAndRunway[RunwayKey(airportId, runwayId)]
				.OrderBy(e => e.RwyEndRwyEndId, StringComparer.Ordinal)
				.ToList();

			List<AirportRunwayEnd> ends = endRows
				.Select(ToRunwayEnd)
				.OfType<AirportRunwayEnd>()
				.ToList();

			AirportRunway runway = new()
			{
				RunwayId = runwayId,
				Length = row.RwyLen,
				SurfaceType = Normalize(row.SurfaceTypeCode),
				FirstEnd = ends.Count > 0 ? ends[0] : null,
				SecondEnd = ends.Count > 1 ? ends[1] : null
			};

			if (!runway.HasGeometry)
			{
				// Routine NASR gap, not a fault in the data we produce: the runway still counts
				// for the alias file's longest-runway line, it simply cannot be drawn. The two
				// causes are named separately because they point at different things - no rows
				// at all is a missing APT_RWY_END record, blank coordinates are an unsurveyed
				// field.
				string reason = endRows.Count == 0
					? "has no APT_RWY_END records"
					: "has APT_RWY_END records with no published coordinates";

				messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
					$"Airport '{airportId}': runway '{runwayId}' {reason} and was not drawn."));
			}

			runways.Add(runway);
		}

		return runways;
	}

	private static AirportRunwayEnd? ToRunwayEnd(AptCsvDataModel.AptRwyEnd row)
	{
		if (row.RwyEndLatDecimal is not { } latitude || row.RwyEndLongDecimal is not { } longitude)
		{
			return null;
		}

		return new AirportRunwayEnd(row.RwyEndRwyEndId?.Trim() ?? string.Empty, latitude, longitude);
	}

	/// <summary>
	/// Indexes the <c>FRQ</c> rows by the airport they belong to.
	/// </summary>
	/// <param name="rows">Every <c>FRQ</c> row in the cycle.</param>
	/// <returns>The rows, keyed by airport identifier.</returns>
	/// <remarks>
	/// <c>SERVICED_FACILITY</c> is the key, not <c>FACILITY</c>. NASR leaves <c>FACILITY</c>
	/// null for exactly the facility types this sub-service cares about - CTAF, UNICOM, GCO and
	/// AFIS all publish the airport in <c>SERVICED_FACILITY</c> instead, which the FAA documents
	/// as always populated. Keying on <c>FACILITY</c> instead would find no CTAF for any airport
	/// in the database.
	/// <para>
	/// A row whose <c>FACILITY</c> differs from its <c>SERVICED_FACILITY</c> (a tower serving a
	/// satellite field, say) is indexed under both, so nothing is lost either way.
	/// </para>
	/// </remarks>
	private static ILookup<string, FrqCsvDataModel.Frq> BuildFrequencyLookup(IReadOnlyList<FrqCsvDataModel.Frq> rows)
	{
		List<(string Key, FrqCsvDataModel.Frq Row)> indexed = new();

		foreach (FrqCsvDataModel.Frq row in rows)
		{
			string serviced = row.ServicedFacility?.Trim() ?? string.Empty;
			string facility = row.Facility?.Trim() ?? string.Empty;

			if (serviced.Length > 0)
			{
				indexed.Add((serviced, row));
			}

			if (facility.Length > 0 && !facility.Equals(serviced, StringComparison.OrdinalIgnoreCase))
			{
				indexed.Add((facility, row));
			}
		}

		return indexed.ToLookup(entry => entry.Key, entry => entry.Row, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Finds the first frequency whose <c>FREQ_USE</c> mentions any of <paramref name="uses"/>.
	/// </summary>
	/// <param name="rows">Every <c>FRQ</c> row for the airport.</param>
	/// <param name="uses">The use tokens to look for, e.g. <c>CTAF</c>, or <c>AWOS</c> and <c>ASOS</c>.</param>
	/// <returns>The first matching row, or <see langword="null"/>.</returns>
	/// <remarks>
	/// An airport routinely publishes the same use on several rows; NASR gives no ranking, so
	/// the first is as good as any and keeps the output stable.
	/// </remarks>
	private static FrqCsvDataModel.Frq? FindFrequency(IEnumerable<FrqCsvDataModel.Frq> rows, params string[] uses)
	{
		foreach (FrqCsvDataModel.Frq row in rows)
		{
			string use = row.FreqUse ?? string.Empty;

			foreach (string candidate in uses)
			{
				if (use.Contains(candidate, StringComparison.OrdinalIgnoreCase))
				{
					return row;
				}
			}
		}

		return null;
	}

	private static string RunwayKey(string? airportId, string? runwayId) =>
		$"{airportId?.Trim()}␟{runwayId?.Trim()}";

	private static string? Normalize(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
