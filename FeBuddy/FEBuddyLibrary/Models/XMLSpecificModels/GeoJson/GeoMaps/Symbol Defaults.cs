using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;


[XmlRoot(ElementName = "SymbolDefaults", IsNullable = true)]
public class Symbol_Defaults
{
	[XmlAttribute(AttributeName = "Bcg")]
	public int Bcg { get; set; }

	[XmlAttribute(AttributeName = "Filters")]
	public string Filters { get; set; }

	[XmlAttribute(AttributeName = "Style")]
	public string Style { get; set; }

	[XmlAttribute(AttributeName = "Size")]
	public int Size { get; set; }

	public override string ToString()
	{
		return $"Symbol Defaults: BCG {Bcg}__Style {Style}__Size {Size}";
	}
}
