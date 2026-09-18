using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.VideoMaps;

/// <summary>
/// Represents a collection of colors used within a VideoMap.
/// This class contains a list of named colors that can be applied to various elements.
/// </summary>
[XmlRoot(ElementName = "Colors")]
public class Colors
{
	/// <summary>
	/// Gets or sets the list of named colors.
	/// Represents individual color definitions used for styling VideoMap elements.
	/// </summary>
	[XmlElement(ElementName = "NamedColor")]
	public List<NamedColor> NamedColor { get; set; }
}
