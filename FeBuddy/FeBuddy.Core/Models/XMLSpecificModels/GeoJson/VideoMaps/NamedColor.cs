using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.VideoMaps;

/// <summary>
/// Represents a named color defined by its RGB components and a name.
/// This class is used to define individual colors that can be applied to VideoMap elements.
/// </summary>
[XmlRoot(ElementName = "NamedColor")]
public class NamedColor
{
	/// <summary>
	/// Gets or sets the red component of the color.
	/// Represents the intensity of the red color channel, with a value range from 0 to 255.
	/// </summary>
	[XmlAttribute(AttributeName = "Red")]
	public int Red { get; set; }

	/// <summary>
	/// Gets or sets the green component of the color.
	/// Represents the intensity of the green color channel, with a value range from 0 to 255.
	/// </summary>
	[XmlAttribute(AttributeName = "Green")]
	public int Green { get; set; }

	/// <summary>
	/// Gets or sets the blue component of the color.
	/// Represents the intensity of the blue color channel, with a value range from 0 to 255.
	/// </summary>
	[XmlAttribute(AttributeName = "Blue")]
	public int Blue { get; set; }

	/// <summary>
	/// Gets or sets the name of the color.
	/// Provides a descriptive identifier for the color, such as "SkyBlue" or "ForestGreen".
	/// </summary>
	[XmlAttribute(AttributeName = "Name")]
	public string Name { get; set; }
}
