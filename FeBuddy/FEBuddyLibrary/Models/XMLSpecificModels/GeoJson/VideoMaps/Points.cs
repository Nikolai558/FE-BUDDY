using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.VideoMaps;
/// <summary>
/// Represents a collection of points used to define the shape of a VideoMap element.
/// This class contains a list of WorldPoint instances, which are the coordinates for constructing the element.
/// </summary>
[XmlRoot(ElementName = "Points")]
public class Points
{
	/// <summary>
	/// Gets or sets the list of WorldPoint instances.
	/// Represents individual geographic points that form part of the shape or path of a VideoMap element.
	/// </summary>
	[XmlElement(ElementName = "WorldPoint", IsNullable = true)]
	public List<WorldPoint> WorldPoint { get; set; }
}
