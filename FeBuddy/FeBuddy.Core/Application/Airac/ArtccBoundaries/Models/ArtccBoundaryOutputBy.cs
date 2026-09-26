namespace FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;

/// <summary>How the ARTCC Boundaries sub-service groups its GeoJSON output into files.</summary>
public enum ArtccBoundaryOutputBy
{
	/// <summary>
	/// One High file (every HIGH and every UNLIMITED ring) and one Low file (every LOW and every
	/// UNLIMITED ring) - an UNLIMITED ring appears in both.
	/// </summary>
	HighLow = 0,

	/// <summary>One file each for HIGH, LOW and UNLIMITED rings.</summary>
	HighLowUnlimited = 1,

	/// <summary>One file per LocationId and altitude present, e.g. <c>ZOB-HIGH</c>.</summary>
	ArtccAltitude = 2,
}
