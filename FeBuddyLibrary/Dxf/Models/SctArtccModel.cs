using FeBuddyLibrary.DataAccess;

namespace FeBuddyLibrary.Dxf.Models
{
    public class SctArtccModel : IStartEndLat
    {
        public string Name { get; set; }
        public string StartLat { get; set; }
        public string StartLon { get; set; }
        public string EndLat { get; set; }
        public string EndLon { get; set; }
        public string Comments { get; set; }

        public string AllInfo
        {
            get
            {
                string output = $"{Name} {StartLat} {StartLon} {EndLat} {EndLon}";

                if (!string.IsNullOrEmpty(Comments))
                {
                    output += $" {Comments}";
                }

                return output;
            }
        }
    }
}
