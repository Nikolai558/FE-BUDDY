using System.Collections.Generic;
using System.Text;

namespace FeBuddyLibrary.Dxf.Models
{
    public class SctRegionModel
    {
        public string RegionColorName { get; set; }
        public string Lat { get; set; }
        public string Lon { get; set; }

        public List<RegionPolygonPoints> AdditionalRegionInfo { get; set; }

        public string AllInfo
        {
            get
            {
                StringBuilder output = new StringBuilder();
                output.Append($"{RegionColorName.PadRight(26)}{Lat} {Lon}");
                if (AdditionalRegionInfo.Count >= 1)
                {
                    output.Append("\n");
                    foreach (var item in AdditionalRegionInfo)
                    {
                        output.Append($"{" ".PadRight(26)}{item.Lat} {item.Lon}\n");
                    }
                    output.Remove(output.Length - 1, 1);
                }
                return output.ToString();
            }
        }
    }

    public class RegionPolygonPoints
    {
        public string Lat { get; set; }
        public string Lon { get; set; }
    }
}
