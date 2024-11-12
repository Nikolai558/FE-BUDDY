using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;

[XmlRoot(ElementName = "GeoMapObject")]
public class GeoMapObjects
{
	[XmlElement(ElementName = "GeoMapObject")]
	public List<GeoMapObject> GeoMapObject { get; set; }
}
