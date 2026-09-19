using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddy.Core.Models.XMLSpecificModels.GeoJson.VideoMaps;

/// <summary>
/// Represents a geographic point defined by longitude and latitude.
/// This class is used to specify individual coordinates for VideoMap elements.
/// </summary>
[XmlRoot(ElementName = "WorldPoint")]
public class WorldPoint
{
	/// <summary>
	/// Gets or sets the longitude of the point.
	/// Represents the geographic east-west position in decimal degrees.
	/// </summary>
	[XmlAttribute(AttributeName = "Lon")]
	public double Lon { get; set; }

	/// <summary>
	/// Gets or sets the latitude of the point.
	/// Represents the geographic north-south position in decimal degrees.
	/// </summary>
	[XmlAttribute(AttributeName = "Lat")]
	public double Lat { get; set; }
}
