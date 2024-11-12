using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;

[XmlRoot(ElementName = "LineDefaults", IsNullable = true)]
public class LineDefaults
{
	[XmlAttribute(AttributeName = "Bcg")]
	public int Bcg { get; set; }

	[XmlAttribute(AttributeName = "Filters")]
	public string Filters { get; set; }

	[XmlAttribute(AttributeName = "Style")]
	public string Style { get; set; }

	[XmlAttribute(AttributeName = "Thickness")]
	public int Thickness { get; set; }

	public override string ToString()
	{
		return $"Line Defaults: BCG {Bcg}__Style {Style}__Thickness {Thickness}";
	}
}
