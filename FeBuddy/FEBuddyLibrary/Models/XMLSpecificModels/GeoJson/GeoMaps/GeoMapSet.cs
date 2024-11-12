using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.GeoMaps;
/// <summary>
/// Represents a set of geographic maps that can be serialized or deserialized using XML.
/// This class is marked as the root element in XML serialization and contains collections of maps, background menus, and filter menus.
/// </summary>
[XmlRoot(ElementName = "GeoMapSet")]
public class GeoMapSet
{
	/// <summary>
	/// Gets or sets the collection of background menus associated with the geographic map set.
	/// </summary>
	[XmlElement(ElementName = "BcgMenus")]
	public BcgMenus BcgMenus { get; set; }

	/// <summary>
	/// Gets or sets the collection of filter menus associated with the geographic map set.
	/// </summary>
	[XmlElement(ElementName = "FilterMenus")]
	public FilterMenus FilterMenus { get; set; }

	/// <summary>
	/// Gets or sets the collection of geographic maps contained in this map set.
	/// </summary>
	[XmlElement(ElementName = "GeoMaps")]
	public GeoMaps GeoMaps { get; set; }

	/// <summary>
	/// Gets or sets the XML schema instance (xsi) namespace attribute for the geographic map set.
	/// </summary>
	[XmlAttribute(AttributeName = "xsi")]
	public string Xsi { get; set; }

	/// <summary>
	/// Gets or sets the XML schema definition (xsd) namespace attribute for the geographic map set.
	/// </summary>
	[XmlAttribute(AttributeName = "xsd")]
	public string Xsd { get; set; }

	/// <summary>
	/// Gets or sets the default map identifier for the geographic map set.
	/// </summary>
	[XmlAttribute(AttributeName = "DefaultMap")]
	public string DefaultMap { get; set; }
}
