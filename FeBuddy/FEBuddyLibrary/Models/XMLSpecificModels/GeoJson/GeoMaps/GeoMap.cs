using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;

[XmlRoot(ElementName = "GeoMap")]
public class GeoMap
{
	[XmlElement(ElementName = "Objects")]
	public Objects Objects { get; set; }

	[XmlAttribute(AttributeName = "Name")]
	public string Name { get; set; }

	[XmlAttribute(AttributeName = "LabelLine1")]
	public string LabelLine1 { get; set; }

	[XmlAttribute(AttributeName = "LabelLine2")]
	public string LabelLine2 { get; set; }

	[XmlAttribute(AttributeName = "BcgMenuName")]
	public string BcgMenuName { get; set; }

	[XmlAttribute(AttributeName = "FilterMenuName")]
	public string FilterMenuName { get; set; }

}
