using System.Collections.Generic;
using System.Xml.Serialization;

namespace FeBuddyLibrary.Models
{
    // TODO - Can "public class Runways" be Internal/not public?
    public class Runways
    {
        [XmlElement]
        public List<Runway> Runway { get; set; }
    }
}
