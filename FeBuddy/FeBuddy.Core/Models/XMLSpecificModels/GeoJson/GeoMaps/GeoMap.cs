using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents a geographic map that can be serialized or deserialized using XML.
/// This class is marked as the root element in XML serialization and contains metadata attributes
/// as well as a collection of geographic objects represented by <see cref="GeoMapObjects"/>.
/// </summary>
[XmlRoot(ElementName = "GeoMap")]
public class GeoMap
{
	/// <summary>
	/// Gets or sets the collection of geographic objects for the map.
	/// </summary>
	[XmlElement(ElementName = "Objects")]
	public GeoMapObjects Objects { get; set; }

	/// <summary>
	/// Gets or sets the name of the geographic map.
	/// </summary>
	[XmlAttribute(AttributeName = "Name")]
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the first label line for the geographic map.
	/// </summary>
	[XmlAttribute(AttributeName = "LabelLine1")]
	public string LabelLine1 { get; set; }

	/// <summary>
	/// Gets or sets the second label line for the geographic map.
	/// </summary>
	[XmlAttribute(AttributeName = "LabelLine2")]
	public string LabelLine2 { get; set; }

	/// <summary>
	/// Gets or sets the name used for the background menu related to the geographic map.
	/// </summary>
	[XmlAttribute(AttributeName = "BcgMenuName")]
	public string BcgMenuName { get; set; }

	/// <summary>
	/// Gets or sets the name used for the filter menu related to the geographic map.
	/// </summary>
	[XmlAttribute(AttributeName = "FilterMenuName")]
	public string FilterMenuName { get; set; }
}
