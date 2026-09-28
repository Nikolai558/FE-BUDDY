namespace FeBuddy.Core.Application.Conversions.EramToGeojson.Models;

/// <summary>How the ERAM to GeoJSON conversion lays out its files.</summary>
public enum EramOutputLayout
{
	/// <summary>
	/// "Object Type and Map Group": a folder per GeoMap and a file per object, named
	/// <c>&lt;MapObjectType&gt;_&lt;MapGroupId&gt;</c>. Objects sharing a name and defaults share a file.
	/// </summary>
	ByObject,

	/// <summary>
	/// "Filter Index and Similar Attributes": a folder per GeoMap, then a file per filter, kind
	/// and look, so everything in a file draws the same way.
	/// </summary>
	ByFilter,
}
