using System.Collections.Generic;

namespace FeBuddyLibrary.Models
{
    public class AptModel
    {
        public string Type { get; set; }

        // FAA ID 3-LD
        public string Id { get; set; }

        public string Name { get; set; }

        public string Lat { get; set; }

        public string Lon { get; set; }

        public string Elv { get; set; }

        public string ResArtcc { get; set; }

        // Facility Status. ie. "O"  - OPERATIONAL, "CP" - CLOSED PERMANENTLY, "CI" - CLOSED INDEFINITELY
        public string Status { get; set; }

        public string Twr { get; set; }

        public string Ctaf { get; set; }

        public string Icao { get; set; }

        public string Lat_Dec { get; set; }

        public string Lon_Dec { get; set; }

        public string magVariation { get; set; }

        public List<RunwayModel> Runways { get; set; }
    }
}
