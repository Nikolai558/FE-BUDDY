using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.VideoMaps;

[XmlRoot(ElementName = "VideoMap")]
public class VideoMap
{
	/// <summary>
	/// Gets or sets the Colors used in the VideoMap.
	/// This property defines the color scheme used for visual elements.
	/// </summary>
	[XmlElement(ElementName = "Colors")]
	public Colors Colors { get; set; }

	/// <summary>
	/// Gets or sets the visual elements of the VideoMap.
	/// Defines specific components or items included in the VideoMap configuration.
	/// </summary>
	[XmlElement(ElementName = "Elements")]
	public VideoMapElements Elements { get; set; }

	/// <summary>
	/// Gets or sets the short name of the VideoMap.
	/// This name is a brief identifier used for display or referencing purposes.
	/// </summary>
	[XmlAttribute(AttributeName = "ShortName")]
	public string ShortName { get; set; }

	/// <summary>
	/// Gets or sets the long name of the VideoMap.
	/// This name provides a more detailed or descriptive identifier for the VideoMap.
	/// </summary>
	[XmlAttribute(AttributeName = "LongName")]
	public string LongName { get; set; }

	/// <summary>
	/// Gets or sets the STARS group associated with the VideoMap.
	/// Indicates the group classification for use within STARS systems.
	/// </summary>
	[XmlAttribute(AttributeName = "STARSGroup")]
	public string STARSGroup { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the VideoMap is only for use in TDM STARS.
	/// Specifies whether this VideoMap is restricted to TDM (Terminal Doppler Weather Radar) configurations.
	/// </summary>
	[XmlAttribute(AttributeName = "STARSTDMOnly")]
	public bool STARSTDMOnly { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the VideoMap is visible in lists.
	/// Determines if the VideoMap should be displayed in user interfaces or configuration lists.
	/// </summary>
	[XmlAttribute(AttributeName = "VisibleInList")]
	public bool VisibleInList { get; set; }
}