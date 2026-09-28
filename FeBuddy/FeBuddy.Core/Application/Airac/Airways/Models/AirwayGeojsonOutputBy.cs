namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// Controls how the Airways GeoJSON service groups airways into output files.
/// </summary>
public enum AirwayGeojsonOutputBy
{
	/// <summary>No GeoJSON files are generated from airway data.</summary>
	None,

	/// <summary>
	/// Airways are grouped into High and Low files by their designation: each designation goes in
	/// the High file, the Low file, or both, as <see cref="AirwaySettings.DesignationStrata"/> says.
	/// </summary>
	HighLow,

	/// <summary>
	/// Airways are grouped by their shared <c>AWY_BASE.AWY_DESIGNATION</c>
	/// (e.g. all "J" airways in one file, all "V" airways in another).
	/// </summary>
	Designation
}
