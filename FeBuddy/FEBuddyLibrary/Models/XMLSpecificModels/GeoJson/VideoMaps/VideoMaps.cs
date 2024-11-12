using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.VideoMaps;
/// <summary>
/// Represents a collection of VideoMaps, providing a structure for multiple VideoMap instances.
/// This class is the container for storing a list of VideoMap objects and relevant XML schema information.
/// </summary>
[XmlRoot(ElementName = "VideoMaps")]
public class VideoMaps
{
	/// <summary>
	/// Gets or sets the list of VideoMap instances.
	/// Represents the individual VideoMap configurations contained within this collection.
	/// </summary>
	[XmlElement(ElementName = "VideoMap")]
	public List<VideoMap> VideoMap { get; set; }

	/// <summary>
	/// Gets or sets the XML Schema Instance (xsi) attribute.
	/// Typically used to specify the XML namespace for schema instance definitions.
	/// </summary>
	[XmlAttribute(AttributeName = "xsi")]
	public string Xsi { get; set; }

	/// <summary>
	/// Gets or sets the XML Schema Definition (xsd) attribute.
	/// Typically used to reference the XML schema that defines the structure of this XML document.
	/// </summary>
	[XmlAttribute(AttributeName = "xsd")]
	public string Xsd { get; set; }
}
