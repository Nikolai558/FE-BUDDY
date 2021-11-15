using System.Xml.Serialization;

namespace FeBuddyLibrary.Models
{
    class Waypoint
    {
        [XmlAttribute]
        public string Type { get; set; }

        [XmlAttribute]
        public string ID { get; set; }

        [XmlElement]
        public Location Location { get; set; }
    }
}
