using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;
[XmlRoot(ElementName = "Elements")]
public class Elements
{
	[XmlElement(ElementName = "Element")]
	public List<Element> Element { get; set; }
}
