using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;

[XmlRoot(ElementName = "TextDefaults", IsNullable = true)]
public class TextDefaults
{
	[XmlAttribute(AttributeName = "Bcg")]
	public int Bcg { get; set; }

	[XmlAttribute(AttributeName = "Filters")]
	public string Filters { get; set; }

	[XmlAttribute(AttributeName = "Size")]
	public int Size { get; set; }

	[XmlAttribute(AttributeName = "Underline")]
	public bool Underline { get; set; }

	[XmlAttribute(AttributeName = "Opaque")]
	public bool Opaque { get; set; }

	[XmlAttribute(AttributeName = "XOffset")]
	public int XOffset { get; set; }
	[XmlAttribute(AttributeName = "YOffset")]
	public int YOffset { get; set; }
	public override string ToString()
	{
		return $"Text Defaults: BCG {Bcg}__Size {Size}__Underline {Underline}__Opaque {Opaque}__XOffset {XOffset}__YOffset {YOffset}";
	}
}
