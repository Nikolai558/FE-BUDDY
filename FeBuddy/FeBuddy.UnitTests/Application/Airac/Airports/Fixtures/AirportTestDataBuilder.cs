using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Application.Airac.Airports.Fixtures;

/// <summary>
/// Builds small, synthetic <see cref="NasrCsvDataCollection"/> instances - and the already-built
/// <see cref="Airport"/> objects the alias tests need - for Airports tests, so tests never
/// depend on real FAA CSV files.
/// </summary>
internal static class AirportTestDataBuilder
{
	/// <summary>
	/// Builds a <see cref="NasrCsvDataCollection"/> holding only the given APT, FRQ and
	/// CLS_ARSP rows.
	/// </summary>
	public static NasrCsvDataCollection Build(
		IEnumerable<AptCsvDataModel.AptBase>? airports = null,
		IEnumerable<AptCsvDataModel.AptRwy>? runways = null,
		IEnumerable<AptCsvDataModel.AptRwyEnd>? runwayEnds = null,
		IEnumerable<FrqCsvDataModel.Frq>? frequencies = null,
		IEnumerable<ClsArspCsvDataModel.ClsArsp>? classAirspace = null)
	{
		AptCsvDataCollection aptCollection = new();
		aptCollection.AptBase.AddRange(airports ?? []);
		aptCollection.AptRwy.AddRange(runways ?? []);
		aptCollection.AptRwyEnd.AddRange(runwayEnds ?? []);

		FrqCsvDataCollection frqCollection = new();
		frqCollection.Frq.AddRange(frequencies ?? []);

		ClsArspCsvDataCollection clsArspCollection = new();
		clsArspCollection.ClsArsp.AddRange(classAirspace ?? []);

		return new NasrCsvDataCollection
		{
			Apt = aptCollection,
			Frq = frqCollection,
			ClsArsp = clsArspCollection
		};
	}

	/// <summary>Builds one APT_BASE row.</summary>
	public static AptCsvDataModel.AptBase Base(
		string arptId,
		string? icaoId = null,
		string name = "TEST FIELD",
		double latitude = 47.449,
		double longitude = -122.309,
		double? elevation = 433,
		string respArtccId = "ZSE",
		int? trafficPatternAltitude = null,
		string? fssId = null,
		string towerTypeCode = "ATCT",
		string siteTypeCode = "A",
		string arptStatus = "O") =>
		new()
		{
			ArptId = arptId,
			IcaoId = icaoId,
			ArptName = name,
			BaseLatDecimal = latitude,
			BaseLongDecimal = longitude,
			Elev = elevation,
			RespArtccId = respArtccId,
			Tpa = trafficPatternAltitude,
			FssId = fssId!,
			TwrTypeCode = towerTypeCode,
			SiteTypeCode = siteTypeCode,
			ArptStatus = arptStatus
		};

	/// <summary>Builds one APT_RWY row.</summary>
	public static AptCsvDataModel.AptRwy Runway(
		string arptId,
		string runwayId,
		int length,
		string? surfaceType = null) =>
		new()
		{
			ArptId = arptId,
			RwyRwyId = runwayId,
			RwyLen = length,
			SurfaceTypeCode = surfaceType
		};

	/// <summary>
	/// Builds one APT_RWY_END row. Pass <see langword="null"/> coordinates for the NASR gap
	/// where an end is published without a position.
	/// </summary>
	public static AptCsvDataModel.AptRwyEnd RunwayEnd(
		string arptId,
		string runwayId,
		string endId,
		double? latitude,
		double? longitude) =>
		new()
		{
			ArptId = arptId,
			RwyEndRwyId = runwayId,
			RwyEndRwyEndId = endId,
			RwyEndLatDecimal = latitude,
			RwyEndLongDecimal = longitude
		};

	/// <summary>
	/// Builds one FRQ row. <paramref name="facility"/> is NASR's FACILITY column, which is null
	/// for CTAF, UNICOM, GCO and AFIS rows; SERVICED_FACILITY is the airport and is always
	/// populated, so it defaults to the same value here and can be set independently to model a
	/// real CTAF row.
	/// </summary>
	/// <param name="facility">FACILITY, or <see langword="null"/> as NASR publishes it for CTAF.</param>
	/// <param name="frequency">The frequency value.</param>
	/// <param name="frequencyUse">FREQ_USE, e.g. <c>CTAF</c> or <c>ASOS</c>.</param>
	/// <param name="servicedFacility">SERVICED_FACILITY; defaults to <paramref name="facility"/>.</param>
	/// <returns>The row.</returns>
	public static FrqCsvDataModel.Frq Frequency(
		string? facility,
		string frequency,
		string frequencyUse,
		string? servicedFacility = null) =>
		new()
		{
			Facility = facility,
			ServicedFacility = servicedFacility ?? facility ?? string.Empty,
			Freq = frequency,
			FreqUse = frequencyUse
		};

	/// <summary>Builds one CLS_ARSP row; pass <c>"Y"</c> for each class the airport underlies.</summary>
	public static ClsArspCsvDataModel.ClsArsp ClassAirspaceRow(
		string arptId,
		string? classB = null,
		string? classC = null,
		string? classD = null,
		string? classE = null) =>
		new()
		{
			ArptId = arptId,
			ClassBAirspace = classB,
			ClassCAirspace = classC,
			ClassDAirspace = classD,
			ClassEAirspace = classE
		};

	/// <summary>
	/// Builds an already-assembled <see cref="Airport"/>, bypassing the NASR layer, for the
	/// alias tests. <see cref="Airport.LongestRunway"/> is selected exactly as the builder
	/// would select it.
	/// </summary>
	public static Airport BuiltAirport(
		string faaId = "SEA",
		string? icaoId = null,
		string name = "SEATTLE TACOMA INTL",
		double? elevation = 433,
		string respArtccId = "ZSE",
		int? trafficPatternAltitude = null,
		string? fssId = null,
		string towerType = "TWR",
		string facilityType = "AIRPORT",
		string? ctafFrequency = null,
		string? weatherFrequency = null,
		string? weatherFrequencyUse = null,
		string? classAirspace = null,
		IReadOnlyList<AirportRunway>? runways = null)
	{
		IReadOnlyList<AirportRunway> resolvedRunways = runways ?? [];

		return new Airport
		{
			FaaId = faaId,
			IcaoId = icaoId,
			Name = name,
			Latitude = 47.449,
			Longitude = -122.309,
			Elevation = elevation,
			RespArtccId = respArtccId,
			TrafficPatternAltitude = trafficPatternAltitude,
			FssId = fssId,
			TowerType = towerType,
			FacilityType = facilityType,
			CtafFrequency = ctafFrequency,
			WeatherFrequency = weatherFrequency,
			WeatherFrequencyUse = weatherFrequencyUse,
			ClassAirspace = classAirspace,
			Runways = resolvedRunways,
			LongestRunway = AirportBuilder.SelectLongestRunway(resolvedRunways)
		};
	}

	/// <summary>Builds one already-assembled <see cref="AirportRunway"/> with no geometry.</summary>
	public static AirportRunway BuiltRunway(string runwayId, int length, string? surfaceType = null) =>
		new()
		{
			RunwayId = runwayId,
			Length = length,
			SurfaceType = surfaceType
		};
}
