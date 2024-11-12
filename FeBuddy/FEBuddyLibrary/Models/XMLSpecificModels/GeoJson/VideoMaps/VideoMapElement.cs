using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.VideoMaps;
/// <summary>
/// Represents an individual element within a VideoMap.
/// This class defines properties for visual attributes, positioning, and drawing styles of a specific VideoMap element.
/// </summary>
[XmlRoot(ElementName = "Element")]
public class VideoMapElement
{
	/// <summary>
	/// Gets or sets the XML schema instance type for the element.
	/// Used to specify the type of the element for XML serialization purposes.
	/// </summary>
	[XmlAttribute(AttributeName = "xsi-type")]
	public string XsiType { get; set; }

	/// <summary>
	/// Gets or sets the color of the element.
	/// Defines the color used to visually represent this element.
	/// </summary>
	[XmlAttribute(AttributeName = "Color")]
	public string Color { get; set; }

	/// <summary>
	/// Gets or sets the starting longitude of the element.
	/// Represents the longitude coordinate where this element begins.
	/// </summary>
	[XmlAttribute(AttributeName = "StartLon")]
	public double StartLon { get; set; }

	/// <summary>
	/// Gets or sets the starting latitude of the element.
	/// Represents the latitude coordinate where this element begins.
	/// </summary>
	[XmlAttribute(AttributeName = "StartLat")]
	public double StartLat { get; set; }

	/// <summary>
	/// Gets or sets the ending longitude of the element.
	/// Represents the longitude coordinate where this element ends.
	/// </summary>
	[XmlAttribute(AttributeName = "EndLon")]
	public double EndLon { get; set; }

	/// <summary>
	/// Gets or sets the ending latitude of the element.
	/// Represents the latitude coordinate where this element ends.
	/// </summary>
	[XmlAttribute(AttributeName = "EndLat")]
	public double EndLat { get; set; }

	/// <summary>
	/// Gets or sets the style of the element.
	/// Defines the line style or appearance for drawing this element (e.g., dashed, solid).
	/// </summary>
	[XmlAttribute(AttributeName = "Style")]
	public string Style { get; set; }

	/// <summary>
	/// Gets or sets the thickness of the element.
	/// Specifies the thickness of the line used to draw this element.
	/// </summary>
	[XmlAttribute(AttributeName = "Thickness")]
	public int Thickness { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the element is closed.
	/// If true, the element forms a closed shape by connecting the start and end points.
	/// </summary>
	[XmlAttribute(AttributeName = "Closed")]
	public bool Closed { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the element is filled.
	/// If true, the interior of the closed element is filled with color.
	/// </summary>
	[XmlAttribute(AttributeName = "Filled")]
	public bool Filled { get; set; }

	/// <summary>
	/// Gets or sets the points that define the shape of the element.
	/// Represents additional points for defining complex shapes or paths within the element.
	/// </summary>
	[XmlElement(ElementName = "Points", IsNullable = true)]
	public Points Points { get; set; }
}