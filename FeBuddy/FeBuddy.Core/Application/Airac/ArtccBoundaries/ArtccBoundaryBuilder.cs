using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.ArtccBoundaries;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using static FeBuddy.Core.Infrastructure.Nasr.Models.ArbCsvDataModel;

namespace FeBuddy.Core.Application.Airac.ArtccBoundaries;

/// <summary>
/// Reads every <see cref="ArtccBoundaryRing"/> from the parsed NASR <c>ARB</c> data.
/// </summary>
/// <remarks>
/// Everything is read once, for the whole database, before any filtering: the settings-driven
/// <c>LocationFilter</c> (<see cref="ArtccBoundaryFilter"/>) and the ROI (clipped per ring by
/// <c>ArtccBoundaryGeojsonWriter</c>) both apply downstream.
/// </remarks>
public static class ArtccBoundaryBuilder
{
	private const string LogSource = "ArtccBoundaryBuilder";

	/// <summary>
	/// Reads every ARTCC boundary ring from <paramref name="allNasrCsvData"/>.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Arb</c> must not be null.</param>
	/// <returns>Every ring, ordered by LocationId, then altitude, then ring order, plus any messages collected.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Arb has not been parsed.</exception>
	/// <remarks>
	/// <para>
	/// Locations come from <c>ARB_BASE</c>, keyed by <c>LOCATION_ID</c> (trimmed, upper-cased;
	/// the first row wins for a duplicate ID). <c>ARB_SEG</c> rows are then walked in file order,
	/// grouped by (LocationId, Altitude): within a group, a new ring starts at the group's first
	/// row and again wherever <c>POINT_SEQ</c> is not greater than the previous row's - the low
	/// value marks the start of a new feature (e.g. ZAK's UNLIMITED group is a CTA ring followed
	/// by a FIR ring). A ring's <see cref="ArtccBoundaryRing.Type"/> is its first row's
	/// <c>TYPE</c>, trimmed.
	/// </para>
	/// <para>
	/// A ring is closed by appending its first point again when NASR's own last point differs
	/// from it, then skipped (with an <see cref="LogLevel.Info"/> message) if it still has fewer
	/// than two distinct points. An <c>ALTITUDE</c> other than HIGH, LOW or UNLIMITED gets one
	/// warning per distinct value, and its rows are skipped. A LocationId with <c>ARB_SEG</c> rows
	/// but no <c>ARB_BASE</c> row gets one warning, and its location is built from
	/// <c>ARB_SEG.LOCATION_NAME</c> alone, every other field left blank.
	/// </para>
	/// </remarks>
	public static ArtccBoundaryBuildAllResult Read(NasrCsvDataCollection allNasrCsvData)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		if (allNasrCsvData.Arb is null)
		{
			throw new InvalidOperationException(
				"ARTCC boundary data (ARB) has not been parsed. The ARTCC Boundaries sub-service cannot run without it.");
		}

		List<ServiceMessage> messages = [];

		Dictionary<string, ArtccBoundaryLocation> locations =
			allNasrCsvData.Arb.ArbBase
				.Where(row => !string.IsNullOrWhiteSpace(row.LocationId))
				.GroupBy(row => row.LocationId.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
				.ToDictionary(
					group => group.Key,
					group => ToLocation(group.Key, group.First()),
					StringComparer.OrdinalIgnoreCase);

		// Groups preserve each (LocationId, Altitude) key's rows in file order; groupOrder records
		// the order keys were first seen, since the final ordering is applied afterward.
		Dictionary<(string LocationId, ArtccBoundaryAltitude Altitude), List<ArbSeg>> groups = [];
		List<(string LocationId, ArtccBoundaryAltitude Altitude)> groupOrder = [];
		HashSet<string> warnedAltitudeValues = new(StringComparer.OrdinalIgnoreCase);

		foreach (ArbSeg row in allNasrCsvData.Arb.ArbSeg.Where(row => !string.IsNullOrWhiteSpace(row.LocationId)))
		{
			string locationId = row.LocationId.Trim().ToUpperInvariant();

			if (!ArtccBoundaryAltitudes.TryParse(row.Altitude, out ArtccBoundaryAltitude altitude))
			{
				string rawAltitude = (row.Altitude ?? string.Empty).Trim();

				if (warnedAltitudeValues.Add(rawAltitude))
				{
					messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
						$"ARTCC boundary altitude '{rawAltitude}' is not HIGH, LOW or UNLIMITED. Rows with this value were skipped."));
				}

				continue;
			}

			if (!locations.ContainsKey(locationId))
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"ARTCC boundary '{locationId}' has ARB_SEG rows but no ARB_BASE row. Its location fields other than the name are blank."));

				locations[locationId] = new ArtccBoundaryLocation
				{
					LocationId = locationId,
					LocationName = (row.LocationName ?? string.Empty).Trim(),
				};
			}

			(string, ArtccBoundaryAltitude) key = (locationId, altitude);

			if (!groups.TryGetValue(key, out List<ArbSeg>? rows))
			{
				rows = [];
				groups[key] = rows;
				groupOrder.Add(key);
			}

			rows.Add(row);
		}

		List<ArtccBoundaryRing> rings = [];

		foreach ((string LocationId, ArtccBoundaryAltitude Altitude) key in groupOrder)
		{
			ArtccBoundaryLocation location = locations[key.LocationId];

			foreach (List<ArbSeg> ringRows in SplitIntoRings(groups[key]))
			{
				string type = (ringRows[0].Type ?? string.Empty).Trim();
				List<ArtccBoundaryPoint> points = [.. ringRows.Select(row => new ArtccBoundaryPoint(row.SegLatDecimal, row.SegLongDecimal))];

				ArtccBoundaryPoint first = points[0];

				if (points[^1] != first)
				{
					points.Add(first);
				}

				if (points.Distinct().Count() < 2)
				{
					messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
						$"ARTCC boundary '{key.LocationId}' {ArtccBoundaryAltitudes.NasrToken(key.Altitude)} {type} ring has fewer than two distinct points and was skipped."));
					continue;
				}

				rings.Add(new ArtccBoundaryRing
				{
					Location = location,
					Altitude = key.Altitude,
					Type = type,
					Points = points,
				});
			}
		}

		// A stable sort keeps each (LocationId, Altitude) group's rings in the order they were
		// built (i.e. file order), so this alone gives "LocationId, then altitude, then ring order".
		List<ArtccBoundaryRing> ordered =
			[.. rings
				.OrderBy(ring => ring.Location.LocationId, StringComparer.OrdinalIgnoreCase)
				.ThenBy(ring => ring.Altitude)];

		return new ArtccBoundaryBuildAllResult(ordered, messages);
	}

	/// <summary>
	/// Splits one (LocationId, Altitude) group's rows, in file order, into rings: a new ring
	/// starts at the first row and again wherever <c>POINT_SEQ</c> is not greater than the
	/// previous row's.
	/// </summary>
	private static IEnumerable<List<ArbSeg>> SplitIntoRings(IReadOnlyList<ArbSeg> rows)
	{
		List<ArbSeg> current = [];
		int previousSeq = int.MinValue;

		foreach (ArbSeg row in rows)
		{
			if (current.Count > 0 && row.PointSeq <= previousSeq)
			{
				yield return current;
				current = [];
			}

			current.Add(row);
			previousSeq = row.PointSeq;
		}

		if (current.Count > 0)
		{
			yield return current;
		}
	}

	private static ArtccBoundaryLocation ToLocation(string locationId, ArbBase row) => new()
	{
		LocationId = locationId,
		LocationName = (row.LocationName ?? string.Empty).Trim(),
		ComputerId = (row.ComputerId ?? string.Empty).Trim(),
		IcaoId = (row.IcaoId ?? string.Empty).Trim(),
		LocationType = (row.LocationType ?? string.Empty).Trim(),
		City = (row.City ?? string.Empty).Trim(),
		CountryCode = (row.CountryCode ?? string.Empty).Trim(),
	};
}
