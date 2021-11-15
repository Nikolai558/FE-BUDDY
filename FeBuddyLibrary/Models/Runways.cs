using System.Collections.Generic;
using System.Xml.Serialization;

namespace FeBuddyLibrary.Models
{
    public class Runways
    {
        [XmlElement]
        public List<Runway> Runway { get; set; }
    }
}
