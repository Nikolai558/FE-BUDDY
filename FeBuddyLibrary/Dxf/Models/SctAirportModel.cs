namespace FeBuddyLibrary.Dxf.Models
{
    public class SctAirportModel
    {
        public string Id { get; set; }
        public string Frequency { get; set; }
        public string Lat { get; set; }
        public string Lon { get; set; }
        public string Airspace { get; set; }
        public string Comments { get; set; }

        public string AllInfo
        {
            get
            {
                string output = $"{Id} {Frequency} {Lat} {Lon} {Airspace}";

                if (!string.IsNullOrEmpty(Comments))
                {
                    output += $" {Comments}";
                }

                return output;
            }
        }
    }
}
