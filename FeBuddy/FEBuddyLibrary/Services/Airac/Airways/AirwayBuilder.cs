using FEBuddyLibrary.Handlers.CSV;
using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.General;

using NetTopologySuite.Geometries;

namespace FEBuddyLibrary.Services.Airac.Airways;

/// <summary>
/// Builds the full list of <see cref="Airway"/> domain objects from parsed NASR CSV data:
/// resolving segments, geometry, classification, and (when configured) antimeridian
/// splitting, ROI clipping, and waypoint buffering.
/// </summary>
public static class AirwayBuilder
{
	/// <summary>
	/// Builds every airway found in <paramref name="allNasrCsvData"/>, applying the geometry
	/// transforms <paramref name="settings"/> requests.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Awy</c> must not be null.</param>
	/// <param name="settings">The parsed Airways settings.</param>
	/// <returns>Every buildable airway, plus every non-fatal warning collected.</returns>
	/// <remarks>
	/// Pipeline per airway: normalize segments -&gt; resolve the ordered waypoint list -&gt;
	/// build LineString geometry -&gt; (optional) split at the antimeridian -&gt; (optional)
	/// clip to the ROI -&gt; (optional) buffer away from waypoints -&gt; classify by altitude.
	/// An airway that ends up with no usable geometry (never resolved, or entirely outside
	/// the ROI, or every leg dropped as too short to buffer) is excluded from the result.
	/// </remarks>
	public static AirwayBuildAllResult BuildAll(NasrCsvDataCollection allNasrCsvData, AirwaySettings settings)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(settings);

		if (allNasrCsvData.Awy is null)
		{
			throw new InvalidOperationException("AWY NASR CSV data has not been parsed.");
		}

		List<ServiceMessage> messages = new();
		List<Airway> airways = new();
		List<string> excludedAirwayIds = new();
		bool warnedUnknownDesignation = false;

		/*
		 * Build a dictionary of all airway IDs from AWY_BASE.
		 * GroupBy is used so duplicate AWY_BASE records with the same AwyId do not cause
		 * ToDictionary() to throw an exception; the first occurrence wins.
		 */
		var airwayBaseRecords =
			allNasrCsvData.Awy.AwyBase
				.Where(x => !string.IsNullOrWhiteSpace(x.AwyId))
				.GroupBy(x => x.AwyId.Trim(), StringComparer.OrdinalIgnoreCase)
				.ToDictionary(
					group => group.Key,
					group => group.First(),
					StringComparer.OrdinalIgnoreCase);

		ILookup<string, AwyCsvDataModel.AwySegAlt> airwaySegmentLookup =
			allNasrCsvData.Awy.AwySegAlt
				.Where(x => !string.IsNullOrWhiteSpace(x.AwyId))
				.ToLookup(x => x.AwyId.Trim(), StringComparer.OrdinalIgnoreCase);

		foreach (KeyValuePair<string, AwyCsvDataModel.AwyBase> entry in airwayBaseRecords)
		{
			string awyId = entry.Key;
			AwyCsvDataModel.AwyBase baseRecord = entry.Value;

			string designation = AirwayClassifier.DeriveDesignation(awyId);

			if (designation == AirwayClassifier.UnknownDesignation && !warnedUnknownDesignation)
			{
				warnedUnknownDesignation = true;
				messages.Add(new ServiceMessage(LogLevel.Warning, "AirwayBuilder", $"Airway '{awyId}': its ID has no leading letters; grouped under '{AirwayClassifier.UnknownDesignation}'."));
			}

			// Drop excluded designations before any geometry work, so GeoJSON and the alias
			// file agree and no time is wasted building geometry that is thrown away
			// (remediation plan 3.3).
			if (settings.ExcludedDesignations.Contains(designation))
			{
				continue;
			}

			List<AwyCsvDataModel.AwySegAlt> rawSegments =
				airwaySegmentLookup[awyId].OrderBy(x => x.PointSeq).ToList();

			List<AirwaySegment> normalizedSegments = AirwayNormalizer.Normalize(rawSegments);

			List<AirwayPoint> points = ResolveOrderedPoints(allNasrCsvData, rawSegments);

			AirwayGeometryBuildResult geometryResult =
				AirwayGeometryBuilder.Build(allNasrCsvData, awyId, normalizedSegments);

			messages.AddRange(geometryResult.Messages);

			// An airway with any genuinely unresolvable waypoint is excluded entirely, so a
			// half-built airway never misleads the user (remediation plan 3.2a). Border
			// crossings are normalized away upstream and never land here.
			if (geometryResult.UnresolvedWaypointIds.Count > 0)
			{
				string[] distinctIds = geometryResult.UnresolvedWaypointIds
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToArray();

				messages.Add(new ServiceMessage(LogLevel.Warning, "AirwayBuilder",
					$"Airway '{awyId}': excluded from all output - {distinctIds.Length} waypoint(s) " +
					$"could not be resolved ({string.Join(", ", distinctIds)})."));
				excludedAirwayIds.Add(awyId);
				continue;
			}

			// No usable segment geometry means there is nothing to write for this airway.
			IReadOnlyList<LineString> lineStrings = geometryResult.LineStrings;

			if (lineStrings.Count == 0)
			{
				continue;
			}

			if (settings.SplitAtAntimeridian)
			{
				lineStrings = AntimeridianHandler.Split(lineStrings, AirwayGeometryBuilder.GeometryFactory);
			}

			if (settings.Roi is not null)
			{
				Geometry combined = Combine(lineStrings);

				Geometry? clipped =
					RoiFilter.ClipLineGeometry(combined, settings.Roi, AirwayGeometryBuilder.GeometryFactory);

				if (clipped is null)
				{
					// Airway does not intersect the ROI at all - excluded entirely.
					continue;
				}

				lineStrings = ExtractLineStrings(clipped);

				if (lineStrings.Count == 0)
				{
					continue;
				}
			}

			if (settings.BufferAirwayWaypoints)
			{
				AirwayBufferResult bufferResult =
					AirwayWaypointBuffer.Buffer(lineStrings, points, AirwayGeometryBuilder.GeometryFactory, awyId);

				messages.AddRange(bufferResult.Messages);

				lineStrings = bufferResult.LineStrings;

				if (lineStrings.Count == 0)
				{
					// Every leg was shorter than its combined buffer radius.
					continue;
				}
			}

			(AirwayAltitudeClass altitudeClass, int? maxAuthAlt) =
				AirwayClassifier.Classify(normalizedSegments);

			Airway airway = new()
			{
				AwyId = awyId,
				Designation = designation,
				AwyLocation = (baseRecord.AwyLocation ?? string.Empty).Trim(),
				MaxAuthAlt = maxAuthAlt,
				AltitudeClass = altitudeClass,
				Segments = normalizedSegments,
				Points = points,
				Geometry = Combine(lineStrings),
				Warnings = geometryResult.Warnings
			};

			airways.Add(airway);
		}

		return new AirwayBuildAllResult(airways, messages, excludedAirwayIds);
	}

	/// <summary>
	/// Resolves the ordered, de-duplicated list of every real (non-reference-only) waypoint
	/// on an airway, from its raw <c>AWY_SEG_ALT</c> records.
	/// </summary>
	/// <remarks>
	/// Every row's <c>FROM_POINT</c> carries an explicit <c>FROM_PT_TYPE</c>, but a row's
	/// <c>TO_POINT</c> does not - a point's type is only ever known from a row where that
	/// point appears as the FromPoint. The final point of an airway therefore never appears
	/// as a FromPoint and is added with a <see langword="null"/> <c>PointType</c>; symbol
	/// generation falls back to the "airwayIntersections" style for it (see
	/// <c>AirwayGeojsonService</c>).
	/// </remarks>
	private static List<AirwayPoint> ResolveOrderedPoints(
		NasrCsvDataCollection allNasrCsvData,
		IReadOnlyList<AwyCsvDataModel.AwySegAlt> rawSegments)
	{
		List<AirwayPoint> points = new();
		HashSet<string> seenIds = new(StringComparer.OrdinalIgnoreCase);

		foreach (AwyCsvDataModel.AwySegAlt segment in rawSegments)
		{
			// Reference-only points (e.g. border-crossing markers) are never real waypoints.
			if (AirwayReferenceOnlyPoints.IsReferenceOnlyRow(segment))
			{
				continue;
			}

			TryAddPoint(allNasrCsvData, segment.FromPoint, segment.FromPtType, points, seenIds);
		}

		foreach (AwyCsvDataModel.AwySegAlt segment in rawSegments)
		{
			TryAddPoint(allNasrCsvData, segment.ToPoint, pointType: null, points, seenIds);
		}

		return points;
	}

	private static void TryAddPoint(
		NasrCsvDataCollection allNasrCsvData,
		string? pointId,
		string? pointType,
		List<AirwayPoint> points,
		HashSet<string> seenIds)
	{
		if (string.IsNullOrWhiteSpace(pointId))
		{
			return;
		}

		string trimmedId = pointId.Trim();

		if (!seenIds.Add(trimmedId))
		{
			return;
		}

		var coordinates = FindWaypointCoordinates.GetCoordinates(allNasrCsvData, trimmedId);

		if (!coordinates.HasValue)
		{
			// Unresolvable waypoints are already surfaced as warnings by
			// AirwayGeometryBuilder when they affect line geometry; silently omitting them
			// here (rather than raising a second, largely-duplicate warning) keeps a single
			// clear signal per underlying data problem.
			return;
		}

		points.Add(new AirwayPoint(
			trimmedId,
			pointType,
			coordinates.Value.waypointLat,
			coordinates.Value.waypointLon,
			coordinates.Value.foundIn));
	}

	/// <summary>
	/// Combines one or more LineStrings into a single Geometry: the LineString itself when
	/// there is exactly one, otherwise a MultiLineString.
	/// </summary>
	private static Geometry Combine(IReadOnlyList<LineString> lineStrings)
	{
		return lineStrings.Count == 1
			? lineStrings[0]
			: AirwayGeometryBuilder.GeometryFactory.CreateMultiLineString(lineStrings.ToArray());
	}

	/// <summary>
	/// Extracts the LineString components of a Geometry (a LineString, MultiLineString, or
	/// the line components of a GeometryCollection produced by a clip).
	/// </summary>
	private static List<LineString> ExtractLineStrings(Geometry geometry)
	{
		List<LineString> result = new();

		switch (geometry)
		{
			case LineString lineString:
				result.Add(lineString);
				break;

			case MultiLineString multiLineString:
				for (int i = 0; i < multiLineString.NumGeometries; i++)
				{
					result.Add((LineString)multiLineString.GetGeometryN(i));
				}
				break;
		}

		return result;
	}
}
