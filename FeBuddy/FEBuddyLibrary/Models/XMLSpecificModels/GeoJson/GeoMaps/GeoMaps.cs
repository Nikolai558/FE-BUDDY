using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;

[XmlRoot(ElementName = "GeoMaps")]
public class GeoMaps
{
	[XmlElement(ElementName = "GeoMap")]
	public List<GeoMap> GeoMap { get; set; }
}
