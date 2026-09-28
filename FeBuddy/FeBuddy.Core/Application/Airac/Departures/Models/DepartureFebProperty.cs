namespace FeBuddy.Core.Application.Airac.Departures.Models;

/// <summary>
/// The FE-Buddy custom properties a departure Feature can carry. The user picks which ones they
/// want; each selected value is written as <c>"feb.&lt;name&gt;"</c>, in the order below.
/// </summary>
/// <remarks>
/// Every property describes the Feature it is on, so two of them are written only where they
/// make sense: <see cref="PointId"/> on the per-point Symbols and Text Features, and
/// <see cref="Waypoints"/> on the one Lines Feature that is the whole procedure.
/// </remarks>
public enum DepartureFebProperty
{
	/// <summary><c>feb.dpName</c> - the procedure's published name, e.g. <c>DOTSS</c> or <c>SALT LAKE</c>.</summary>
	DpName = 0,

	/// <summary>
	/// <c>feb.pointId</c> - the identifier of the point this Feature is, e.g. <c>DOTSS</c>.
	/// Symbols and Text only; a Lines Feature is the whole procedure, not one point.
	/// </summary>
	PointId = 1,

	/// <summary><c>feb.arptId</c> - the FAA identifier of the airport this copy of the procedure is for.</summary>
	ArptId = 2,

	/// <summary><c>feb.artcc</c> - the ARTCC the procedure belongs to.</summary>
	Artcc = 3,

	/// <summary><c>feb.amendmentNo</c> - the amendment, spelled out as NASR publishes it (e.g. <c>TWO</c>).</summary>
	AmendmentNo = 4,

	/// <summary><c>feb.amendEffDate</c> - the first effective date of the current amendment (<c>YYYY/MM/DD</c>).</summary>
	AmendEffDate = 5,

	/// <summary>
	/// <c>feb.waypoints</c> - every point in this copy of the procedure, once each. Lines only;
	/// each Symbols and Text Feature is a single point and carries <see cref="PointId"/> instead.
	/// </summary>
	Waypoints = 6,
}
