using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents the default settings for a text element that can be serialized or deserialized using XML.
/// This class is marked as the root element in XML serialization and contains attributes that define the visual properties of a text element.
/// </summary>
[XmlRoot(ElementName = "TextDefaults", IsNullable = true)]
public class TextDefaults
{
	/// <summary>
	/// Gets or sets the background color identifier for the text.
	/// </summary>
	[XmlAttribute(AttributeName = "Bcg")]
	public int Bcg { get; set; }

	/// <summary>
	/// Gets or sets the filter settings for the text.
	/// </summary>
	[XmlAttribute(AttributeName = "Filters")]
	public string Filters { get; set; }

	/// <summary>
	/// Gets or sets the size of the text.
	/// </summary>
	[XmlAttribute(AttributeName = "Size")]
	public int Size { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the text should be underlined.
	/// </summary>
	[XmlAttribute(AttributeName = "Underline")]
	public bool Underline { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the text should be opaque.
	/// </summary>
	[XmlAttribute(AttributeName = "Opaque")]
	public bool Opaque { get; set; }

	/// <summary>
	/// Gets or sets the X-axis offset for the text.
	/// </summary>
	[XmlAttribute(AttributeName = "XOffset")]
	public int XOffset { get; set; }

	/// <summary>
	/// Gets or sets the Y-axis offset for the text.
	/// </summary>
	[XmlAttribute(AttributeName = "YOffset")]
	public int YOffset { get; set; }

	/// <summary>
	/// Returns a string representation of the text defaults, including background color, size, underline, opacity, and offsets.
	/// </summary>
	/// <returns>A formatted string describing the text default settings.</returns>
	public override string ToString()
	{
		return $"Text Defaults: BCG {Bcg}__Size {Size}__Underline {Underline}__Opaque {Opaque}__XOffset {XOffset}__YOffset {YOffset}";
	}
}
