using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents a collection of <see cref="GeoMapObject"/> instances that can be serialized or deserialized using XML.
/// This class is marked as the root element in XML serialization and serves as a container for multiple geographic map objects.
/// </summary>
[XmlRoot(ElementName = "GeoMapObject")]
public class GeoMapObjects
{
	/// <summary>
	/// Gets or sets the list of <see cref="GeoMapObject"/> that belong to this collection.
	/// </summary>
	[XmlElement(ElementName = "GeoMapObject")]
	public List<GeoMapObject> GeoMapObject { get; set; }
}
