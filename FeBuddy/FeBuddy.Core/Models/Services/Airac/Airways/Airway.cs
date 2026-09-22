using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Models.Services.Airac.Airways;

/// <summary>
/// One fully-built airway: its identity, classification, resolved waypoints, and rendered
/// geometry. One <see cref="Airway"/> always corresponds to exactly one GeoJSON Feature in
/// the Lines output (see the "one airway = one Feature" rule in the build plan's ground
/// rules).
/// </summary>
public sealed class Airway
{
	/// <summary>The airway identifier (<c>AWY_BASE.AWY_ID</c>), e.g. "J3".</summary>
	public required string AwyId { get; init; }

	/// <summary>
	/// The airway designation, derived from the leading letters of <see cref="AwyId"/> before
	/// the first digit, upper-cased (<c>J3</c> -&gt; <c>J</c>, <c>AT1</c> -&gt; <c>AT</c>). See
	/// <c>AirwayClassifier.DeriveDesignation</c>. Never taken from
	/// <c>AWY_BASE.AWY_DESIGNATION</c>. Feeds file naming, the exclusion filter, and the GUI
	/// toggle list.
	/// </summary>
	public required string Designation { get; init; }

	/// <summary>The airway location code: A (Alaska), H (Hawaii), or C (U.S. Contiguous).</summary>
	public required string AwyLocation { get; init; }

	/// <summary>The highest <c>MAX_AUTH_ALT</c> found across this airway's segments, if any.</summary>
	public int? MaxAuthAlt { get; init; }

	/// <summary>This airway's computed High/Low/Other classification.</summary>
	public required AirwayAltitudeClass AltitudeClass { get; init; }

	/// <summary>The normalized point-to-point segments that make up this airway, in order.</summary>
	public required IReadOnlyList<AirwaySegment> Segments { get; init; }

	/// <summary>
	/// Every waypoint on this airway, ordered and de-duplicated (a waypoint that is both the
	/// end of one segment and the start of the next appears only once).
	/// </summary>
	public required IReadOnlyList<AirwayPoint> Points { get; init; }

	/// <summary>
	/// The airway's rendered geometry: a <see cref="LineString"/> when the airway is a single
	/// continuous run, or a <see cref="MultiLineString"/> when it contains gaps,
	/// discontinuities, an antimeridian split, or waypoint buffering.
	/// </summary>
	public required Geometry Geometry { get; init; }

	/// <summary>
	/// Whether this airway's geometry crosses the Region of Interest. Always
	/// <see langword="true"/> when no ROI is set.
	/// </summary>
	/// <remarks>
	/// An airway outside the ROI is still built, because the alias file's <c>All</c> scope
	/// promises every FAA airway whatever the region. It is never drawn: <c>AirwayService</c>
	/// hands only the airways with this flag set to the GeoJSON output, and for those
	/// <see cref="Geometry"/> is already clipped to the ROI. For an airway outside the ROI,
	/// <see cref="Geometry"/> is the unclipped, unbuffered line and is not used.
	/// </remarks>
	public bool CrossesRoi { get; init; } = true;

	/// <summary>
	/// Non-fatal problems encountered while building this specific airway (e.g. an
	/// unresolvable mid-airway waypoint that forced the airway to stop early). Empty when
	/// nothing went wrong.
	/// </summary>
	public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
