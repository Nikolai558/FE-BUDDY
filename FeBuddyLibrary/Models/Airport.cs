using System.Xml.Serialization;

namespace FeBuddyLibrary.Models
{
    public class Airport
    {
        [XmlAttribute]
        public string ID { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Elevation { get; set; }

        [XmlAttribute]
        public string MagVar { get; set; }

        [XmlAttribute]
        public string Frequency { get; set; }

        [XmlElement]
        public Location Location { get; set; }

        [XmlElement]
        public Runways Runways { get; set; }
    }
}
