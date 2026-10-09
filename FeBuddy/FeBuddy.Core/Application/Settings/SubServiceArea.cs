namespace FeBuddy.Core.Application.Settings;

/// <summary>
/// Which one of a sub-service's area filters a run uses: the <c>Area</c> setting, chosen on each
/// tab's Area card. Only one applies; the others are kept but not used.
/// </summary>
/// <remarks>
/// Which a sub-service offers: ARTCC Boundaries, Arrivals and Departures offer
/// <see cref="Artccs"/>, <see cref="Roi"/> and <see cref="Everything"/>; Airports, Airways,
/// NAVAIDs, Fixes and Wx Stations <see cref="Roi"/> and <see cref="Everything"/>; Procedures all
/// four, its <see cref="Artccs"/> being the facilities. Telephony has no area.
/// </remarks>
public enum SubServiceArea
{
	/// <summary>No area filter: everything in the cycle.</summary>
	Everything = 0,

	/// <summary>Only what is in the ARTCCs listed (Procedures: the facilities).</summary>
	Artccs = 1,

	/// <summary>Only what is inside the Region of Interest.</summary>
	Roi = 2,

	/// <summary>Procedures only: no airport as a whole, just the airports and procedures listed.</summary>
	None = 3,
}
