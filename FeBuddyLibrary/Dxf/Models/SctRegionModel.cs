using System.Collections.Generic;

namespace FeBuddyLibrary.Dxf.Models
{
    public class SctRegionModel
    {
        public string RegionColorName { get; set; }
        public string Lat { get; set; }
        public string Lon { get; set; }

        public List<RegionPolygonPoints> AdditionalRegionInfo { get; set; }
    }

    public class RegionPolygonPoints
    {
        public string Lat { get; set; }
        public string Lon { get; set; }
    }
}