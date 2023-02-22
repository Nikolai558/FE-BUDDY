namespace FeBuddyLibrary.Models
{
    public class VORModel: IDecLatLon
    {
        public string Id { get; set; }

        public string Freq { get; set; }

        public string Lat { get; set; }

        public string Lon { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string Dec_Lat { get; set; }

        public string Dec_Lon { get; set; }
    }
}
