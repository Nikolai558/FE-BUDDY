namespace FeBuddy.Core.Models.Services.Airac.Departures;

/// <summary>
/// The FE-Buddy custom properties a departure Feature can carry. The user picks which ones they
/// want; each selected value is written as <c>"feb.&lt;name&gt;"</c>.
/// </summary>
public enum DepartureFebProperty
{
	/// <summary><c>feb.dpName</c> - the procedure's published name, e.g. <c>DOTSS</c> or <c>SALT LAKE</c>.</summary>
	DpName = 0,

	/// <summary><c>feb.arptId</c> - the FAA identifier of the airport this copy of the procedure is for.</summary>
	ArptId = 1,

	/// <summary><c>feb.artcc</c> - the ARTCC the procedure belongs to.</summary>
	Artcc = 2,

	/// <summary><c>feb.amendmentNo</c> - the amendment, spelled out as NASR publishes it (e.g. <c>TWO</c>).</summary>
	AmendmentNo = 3,

	/// <summary><c>feb.amendEffDate</c> - the first effective date of the current amendment (<c>YYYY/MM/DD</c>).</summary>
	AmendEffDate = 4,

	/// <summary><c>feb.waypoints</c> - every point in this copy of the procedure, once each.</summary>
	Waypoints = 5,
}
