using System.Xml.Serialization;


/// <summary>
/// These three classes are in one file because they are all the same. However, we need three of them because the way XML Serializes them.
/// If just one class was used, then the output files that require XML Serialization would be incorrect and cause a bunch of errors.
/// </summary>

namespace FeBuddyLibrary.Models
{
    public class Location
    {
        [XmlAttribute]
        public string Lon { get; set; }

        [XmlAttribute]
        public string Lat { get; set; }
    }

    public class StartLoc
    {
        [XmlAttribute]
        public string Lon { get; set; }

        [XmlAttribute]
        public string Lat { get; set; }
    }

    public class EndLoc
    {
        [XmlAttribute]
        public string Lon { get; set; }

        [XmlAttribute]
        public string Lat { get; set; }
    }
}
