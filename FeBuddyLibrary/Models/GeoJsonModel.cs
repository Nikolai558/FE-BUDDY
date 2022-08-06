using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyLibrary.Models
{
    public class FeatureCollection
    {
        public string type { get; set; } = "FeatureCollection";

        public List<Feature> features { get; set; } = new List<Feature>();
    }

    public class Feature
    {
        public string type { get; set; } = "Feature";
        public Geometry geometry { get; set; } = new Geometry();
        public Properties properties { get; set; } = new Properties();
    }

    public class Geometry
    {
        public string type { get; set; }
        public List<dynamic> corrdinates { get; set; } = new List<dynamic>();
    }

    public class Properties
    {
        // [ALL]
        public string color { get; set; } // Hex color String 
        // [ALL]
        public int bcg { get; set; } // Brightnes Control Group, Int in the range of 1-20
        // [ALL]
        public int[] filters { get; set; } // Array of int in the range of 0-20, ERAM Filter
        // [ALL]
        public int zIndex { get; set; } // Any positive Integer, higher int get shown ontop
        
        // [LINE, SYMBOL] 
        public string style { get; set; } // solid, shortDashed, longDashed, or longDashShortDash, obstruction1, obstruction2, heliport, nuclear, emergencyAirport, radar, iaf, rnavOnlyWaypoint, rnav, airwayIntersections, ndb, vor, otherWaypoints, airport, satelliteAirport, or tacan

        // [LINE]
        public int thickness { get; set; } // any positive int 

        // [Polygon]
        public string asdex { get; set; } // runway, taxiway, apron, or structure 

        //[Symbol, Text]
        public int size { get; set; } // int in the range of 1-4 for text it is 0-5

        // [Text
        public string[] text { get; set; } // Each string represents a line of text
        // [Text]
        public bool underline { get; set; }
        // [Text]
        public bool opaque { get; set; }
        // [Text]
        public int xOffset { get; set; }
        // [Text]
        public int yOffset { get; set; }
    }
}
