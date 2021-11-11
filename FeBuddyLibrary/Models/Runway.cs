using System.Xml.Serialization;

namespace FeBuddyLibrary.Models
{
    // TODO - Can "public class Runway" be Internal/not public?
    public class Runway
    {
        [XmlAttribute]
        public string ID { get; set; }

        [XmlAttribute]
        public string Heading { get; set; }

        [XmlAttribute]
        public string Length { get; set; }

        [XmlAttribute]
        public string Width { get; set; }

        [XmlElement]
        public StartLoc StartLoc { get; set; }

        [XmlElement]
        public EndLoc EndLoc { get; set; }
    }
}
