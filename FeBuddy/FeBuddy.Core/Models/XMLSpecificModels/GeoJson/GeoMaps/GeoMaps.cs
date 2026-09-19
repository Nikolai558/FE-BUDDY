using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents a collection of <see cref="GeoMap"/> instances that can be serialized or deserialized using XML.
/// This class is marked as the root element in XML serialization and serves as a container for multiple geographic maps.
/// </summary>
[XmlRoot(ElementName = "GeoMaps")]
public class GeoMaps
{
	/// <summary>
	/// Gets or sets the list of <see cref="GeoMap"/> that belong to this collection.
	/// </summary>
	[XmlElement(ElementName = "GeoMap")]
	public List<GeoMap> GeoMap { get; set; }
}