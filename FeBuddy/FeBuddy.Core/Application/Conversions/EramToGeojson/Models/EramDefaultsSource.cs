namespace FeBuddy.Core.Application.Conversions.EramToGeojson.Models;

/// <summary>Where the ERAM to GeoJSON conversion takes each file's CRC defaults from.</summary>
public enum EramDefaultsSource
{
	/// <summary>
	/// Carry over as much as possible from the XML: each object's own Line / Symbol / Text
	/// defaults, and each element's own overrides.
	/// </summary>
	Xml,

	/// <summary>
	/// As <see cref="Xml"/>, but an object with no usable defaults of a kind takes the CRC
	/// defaults set on the tab.
	/// </summary>
	XmlThenCard,

	/// <summary>Ignore the XML's display properties and use the CRC defaults set on the tab for everything.</summary>
	Card,
}
