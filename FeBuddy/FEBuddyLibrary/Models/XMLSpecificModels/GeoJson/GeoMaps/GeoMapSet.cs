using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;

[XmlRoot(ElementName = "GeoMapSet")]
public class GeoMapSet
{
	[XmlElement(ElementName = "BcgMenus")]
	public BcgMenus BcgMenus { get; set; }

	[XmlElement(ElementName = "FilterMenus")]
	public FilterMenus FilterMenus { get; set; }

	[XmlElement(ElementName = "GeoMaps")]
	public GeoMaps GeoMaps { get; set; }

	[XmlAttribute(AttributeName = "xsi")]
	public string Xsi { get; set; }

	[XmlAttribute(AttributeName = "xsd")]
	public string Xsd { get; set; }

	[XmlAttribute(AttributeName = "DefaultMap")]
	public string DefaultMap { get; set; }
}
