using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;
[XmlRoot(ElementName = "GeoMapObject")]
public class GeoMapObject
{
	[XmlElement(ElementName = "LineDefaults", IsNullable = true)]
	public LineDefaults LineDefaults { get; set; }

	[XmlElement(ElementName = "SymbolDefaults", IsNullable = true)]
	public SymbolDefaults SymbolDefaults { get; set; }

	[XmlElement(ElementName = "TextDefaults", IsNullable = true)]
	public TextDefaults TextDefaults { get; set; }

	[XmlElement(ElementName = "Elements")]
	public Elements Elements { get; set; }

	[XmlAttribute(AttributeName = "Description")]
	public string Description { get; set; }

	[XmlAttribute(AttributeName = "TdmOnly")]
	public bool TdmOnly { get; set; }
}
