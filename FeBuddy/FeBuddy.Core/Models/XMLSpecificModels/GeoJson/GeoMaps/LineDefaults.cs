using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents the default settings for a line element that can be serialized or deserialized using XML.
/// This class is marked as the root element in XML serialization and contains attributes that define the visual properties of a line.
/// </summary>
[XmlRoot(ElementName = "LineDefaults", IsNullable = true)]
public class LineDefaults
{
	/// <summary>
	/// Gets or sets the background color identifier for the line.
	/// </summary>
	[XmlAttribute(AttributeName = "Bcg")]
	public int Bcg { get; set; }

	/// <summary>
	/// Gets or sets the filter settings for the line.
	/// </summary>
	[XmlAttribute(AttributeName = "Filters")]
	public string Filters { get; set; }

	/// <summary>
	/// Gets or sets the style of the line, which could define its visual appearance (e.g., solid, dashed).
	/// </summary>
	[XmlAttribute(AttributeName = "Style")]
	public string Style { get; set; }

	/// <summary>
	/// Gets or sets the thickness of the line.
	/// </summary>
	[XmlAttribute(AttributeName = "Thickness")]
	public int Thickness { get; set; }

	/// <summary>
	/// Returns a string representation of the line defaults, including the background color, style, and thickness.
	/// </summary>
	/// <returns>A formatted string describing the line default settings.</returns>
	public override string ToString()
	{
		return $"Line Defaults: BCG {Bcg}__Style {Style}__Thickness {Thickness}";
	}
}
