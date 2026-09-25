namespace FeBuddy.Core.Application.Conversions.VeramToGeojson.Models;

/// <summary>How the vERAM to GeoJSON conversion lays out its files.</summary>
public enum VeramOutputLayout
{
	/// <summary>
	/// "GeoMapObject Description": a folder per GeoMap and a file per object, named after its
	/// description. Objects sharing a description and defaults share a file.
	/// </summary>
	ByObject,

	/// <summary>
	/// "Filter Index and Similar Attributes": a folder per GeoMap, then a file per filter, TDM
	/// setting, kind and look, so everything in a file draws the same way.
	/// </summary>
	ByFilter,
}
