using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FEBuddyLibrary.Models.XMLSpecificModels.GeoJson.VideoMaps;

/// <summary>
/// Represents a collection of elements within a VideoMap.
/// This class contains a list of individual VideoMap elements, each representing a distinct visual component.
/// </summary>
[XmlRoot(ElementName = "Elements")]
public class VideoMapElements
{
	/// <summary>
	/// Gets or sets the list of VideoMapElement instances.
	/// Represents the specific visual components contained within the VideoMap.
	/// </summary>
	[XmlElement(ElementName = "Element")]
	public List<VideoMapElement> Element { get; set; }
}