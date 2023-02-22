namespace FeBuddyLibrary.Models
{
    public class FixModel: IDecLatLon
    {
        public string Id { get; set; }

        public string Lat { get; set; }

        public string Lon { get; set; }

        public string Catagory { get; set; }

        public string Use { get; set; }

        public string HiArtcc { get; set; }

        public string LoArtcc { get; set; }

        public string Dec_Lat { get; set; }

        public string Dec_Lon { get; set; }
    }
}
