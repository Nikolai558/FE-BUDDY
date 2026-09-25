using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;

/// <summary>
/// Builds small, synthetic <see cref="NasrCsvDataCollection"/> instances for Airways tests,
/// so tests never depend on real FAA CSV files.
/// </summary>
internal static class AirwayTestDataBuilder
{
	/// <summary>
	/// Builds a <see cref="NasrCsvDataCollection"/> containing only the given fixes,
	/// NAVAIDs, and (optionally) one airway's AWY_BASE/AWY_SEG_ALT records.
	/// </summary>
	public static NasrCsvDataCollection Build(
		IEnumerable<(string Id, double Lat, double Lon)>? fixes = null,
		IEnumerable<(string Id, double Lat, double Lon)>? navaids = null,
		string? awyId = null,
		string? awyDesignation = null,
		string? awyLocation = null,
		IReadOnlyList<AwyCsvDataModel.AwySegAlt>? segments = null)
	{
		FixCsvDataCollection fixCollection = new();

		foreach (var (Id, Lat, Lon) in fixes ?? [])
		{
			fixCollection.FixBase.Add(new FixCsvDataModel.FixBase
			{
				FixId = Id,
				LatDecimal = Lat,
				LongDecimal = Lon
			});
		}

		NavCsvDataCollection navCollection = new();

		foreach (var (Id, Lat, Lon) in navaids ?? [])
		{
			navCollection.NavBase.Add(new NavCsvDataModel.NavBase
			{
				NavId = Id,
				LatDecimal = Lat,
				LongDecimal = Lon
			});
		}

		AwyCsvDataCollection awyCollection = new();

		if (awyId is not null)
		{
			awyCollection.AwyBase.Add(new AwyCsvDataModel.AwyBase
			{
				AwyId = awyId,
				AwyDesignation = awyDesignation ?? "J",
				AwyLocation = awyLocation ?? "C"
			});
		}

		foreach (AwyCsvDataModel.AwySegAlt segment in segments ?? Enumerable.Empty<AwyCsvDataModel.AwySegAlt>())
		{
			awyCollection.AwySegAlt.Add(segment);
		}

		return new NasrCsvDataCollection
		{
			Fix = fixCollection,
			Nav = navCollection,
			Apt = new AptCsvDataCollection(),
			Awy = awyCollection
		};
	}

	/// <summary>
	/// Builds one AWY_SEG_ALT record for <paramref name="awyId"/>.
	/// </summary>
	public static AwyCsvDataModel.AwySegAlt Segment(
		string awyId, int seq, string fromPoint, string? fromPtType, string? toPoint,
		string gapFlag = "N", int? maxAuthAlt = null) =>
		new()
		{
			AwyId = awyId,
			PointSeq = seq,
			FromPoint = fromPoint,
			FromPtType = fromPtType,
			ToPoint = toPoint,
			AwySegGapFlag = gapFlag,
			MaxAuthAlt = maxAuthAlt
		};
}
