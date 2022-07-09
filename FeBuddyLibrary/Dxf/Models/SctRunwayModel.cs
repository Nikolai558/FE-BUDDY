namespace FeBuddyLibrary.Dxf.Models
{
    public class SctRunwayModel
    {
        public string RunwayNumber { get; set; }
        public string OppositeRunwayNumber { get; set; }
        public string MagRunwayHeading { get; set; }
        public string OppositeMagRunwayHeading { get; set; }
        public string StartLat { get; set; }
        public string StartLon { get; set; }
        public string EndLat { get; set; }
        public string EndLon { get; set; }
        public string Comments { get; set; }

        public string AllInfo
        {
            get
            {
                string output = $"{RunwayNumber} {OppositeRunwayNumber} {MagRunwayHeading} {OppositeMagRunwayHeading} {StartLat} {StartLon} {EndLat} {EndLon}";

                if (!string.IsNullOrEmpty(Comments))
                {
                    output += $" {Comments}";
                }

                return output;
            }
        }
    }
}
