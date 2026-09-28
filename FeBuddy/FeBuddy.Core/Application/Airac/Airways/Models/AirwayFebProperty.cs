namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// The FE-Buddy custom properties an airway Feature can carry. The user picks which ones they
/// want; each selected value is written as <c>"feb.&lt;name&gt;"</c>, in the order below.
/// </summary>
/// <remarks>
/// Every property describes the Feature it is on, so two of them are written only where they
/// make sense: <see cref="PointId"/> on the per-point Symbols Features, and
/// <see cref="Waypoints"/> on the Lines Feature that is the whole airway.
/// </remarks>
public enum AirwayFebProperty
{
	/// <summary>
	/// <c>feb.awyId</c> - the airway's identifier, e.g. <c>J3</c>. On Lines it is that airway's
	/// ID; on Symbols and Text, where a waypoint is written once per file even when several
	/// airways in the file share it, it is every airway in that file that uses the point.
	/// </summary>
	AwyId = 0,

	/// <summary>
	/// <c>feb.pointId</c> - the identifier of the point this Feature is, e.g. <c>DOTSS</c>.
	/// Symbols only; a Lines Feature is the whole airway, and a Text Feature's label already is
	/// the point's ID.
	/// </summary>
	PointId = 1,

	/// <summary>
	/// <c>feb.waypoints</c> - the airway's point IDs, in order. Lines only; each Symbols and
	/// Text Feature is a single point.
	/// </summary>
	Waypoints = 2,
}
