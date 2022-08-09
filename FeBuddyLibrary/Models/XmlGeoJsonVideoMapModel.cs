using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FeBuddyLibrary.Models
{
    [XmlRoot(ElementName = "NamedColor")]
    public class NamedColor
    {

        [XmlAttribute(AttributeName = "Red")]
        public int Red { get; set; }

        [XmlAttribute(AttributeName = "Green")]
        public int Green { get; set; }

        [XmlAttribute(AttributeName = "Blue")]
        public int Blue { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "Colors")]
    public class Colors
    {

        [XmlElement(ElementName = "NamedColor")]
        public List<NamedColor> NamedColor { get; set; }
    }

    [XmlRoot(ElementName = "Element")]
    public class vmElement
    {

        [XmlAttribute(AttributeName = "xsi-type")]
        public string XsiType { get; set; }

        [XmlAttribute(AttributeName = "Color")]
        public string Color { get; set; }

        [XmlAttribute(AttributeName = "StartLon")]
        public double StartLon { get; set; }

        [XmlAttribute(AttributeName = "StartLat")]
        public double StartLat { get; set; }

        [XmlAttribute(AttributeName = "EndLon")]
        public double EndLon { get; set; }

        [XmlAttribute(AttributeName = "EndLat")]
        public double EndLat { get; set; }

        [XmlAttribute(AttributeName = "Style")]
        public string Style { get; set; }

        [XmlAttribute(AttributeName = "Thickness")]
        public int Thickness { get; set; }

        [XmlAttribute(AttributeName = "Closed")]
        public bool Closed { get; set; }

        [XmlAttribute(AttributeName = "Filled")]
        public bool Filled { get; set; }

        [XmlElement(ElementName = "Points", IsNullable = true)]
        public Points Points { get; set; }
    }

    [XmlRoot(ElementName = "Points", IsNullable = true)]
    public class Points
    {
        [XmlElement(ElementName = "WorldPoint", IsNullable = true)]
        public List<WorldPoint> WorldPoint { get; set; }
    }

    [XmlRoot(ElementName = "WorldPoint", IsNullable = true)]
    public class WorldPoint
    {
        [XmlAttribute(AttributeName = "Lon")]
        public double Lon { get; set; }

        [XmlAttribute(AttributeName = "Lat")]
        public double Lat { get; set; }
    }

    [XmlRoot(ElementName = "Elements")]
    public class vmElements
    {

        [XmlElement(ElementName = "Element")]
        public List<vmElement> Element { get; set; }
    }

    [XmlRoot(ElementName = "VideoMap")]
    public class VideoMap
    {

        [XmlElement(ElementName = "Colors")]
        public Colors Colors { get; set; }

        [XmlElement(ElementName = "Elements")]
        public vmElements Elements { get; set; }

        [XmlAttribute(AttributeName = "ShortName")]
        public string ShortName { get; set; }

        [XmlAttribute(AttributeName = "LongName")]
        public string LongName { get; set; }

        [XmlAttribute(AttributeName = "STARSGroup")]
        public string STARSGroup { get; set; }

        [XmlAttribute(AttributeName = "STARSTDMOnly")]
        public bool STARSTDMOnly { get; set; }

        [XmlAttribute(AttributeName = "VisibleInList")]
        public bool VisibleInList { get; set; }
    }

    [XmlRoot(ElementName = "VideoMaps")]
    public class VideoMaps
    {

        [XmlElement(ElementName = "VideoMap")]
        public List<VideoMap> VideoMap { get; set; }

        [XmlAttribute(AttributeName = "xsi")]
        public string Xsi { get; set; }

        [XmlAttribute(AttributeName = "xsd")]
        public string Xsd { get; set; }
    }

}
