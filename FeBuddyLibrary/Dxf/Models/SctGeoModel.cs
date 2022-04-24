namespace FeBuddyLibrary.Dxf.Models
{
    public class SctGeoModel
    {
        public string StartLat { get; set; }
        public string StartLon { get; set; }
        public string EndLat { get; set; }
        public string EndLon { get; set; }
        public string Color { get; set; }

        public string AllInfo
        {
            get
            {
                string output = $"{StartLat} {StartLon} {EndLat} {EndLon} {Color}";
                return output;
            }
        }
    }
}
