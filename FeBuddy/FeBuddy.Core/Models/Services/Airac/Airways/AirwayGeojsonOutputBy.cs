namespace FeBuddy.Core.Models.Services.Airac.Airways;

/// <summary>
/// Controls how the Airways GeoJSON service groups airways into output files.
/// </summary>
public enum AirwayGeojsonOutputBy
{
	/// <summary>No GeoJSON files are generated from airway data.</summary>
	None,

	/// <summary>
	/// Airways are grouped into High, Low, and Other files based on
	/// <see cref="AirwayAltitudeClass"/>.
	/// </summary>
	HighLow,

	/// <summary>
	/// Airways are grouped by their shared <c>AWY_BASE.AWY_DESIGNATION</c>
	/// (e.g. all "J" airways in one file, all "V" airways in another).
	/// </summary>
	Designation
}
