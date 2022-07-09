namespace FeBuddyLibrary.Models
{
    public class AptWxModel
    {
        public string SensorIdent { get; set; }

        // ASOS = Upgraded verson of AWOS
        // POSIBLE SENSOR TYPES ARE: ASOS, AWOS... etc....
        public string SensorType { get; set; }

        public string CommissioningStatus { get; set; }
        public string NavaidFlag { get; set; }
        public string StationLat { get; set; }
        public string StationLon { get; set; }
        public string Elevation { get; set; }
        public string StationFreq { get; set; }
        public string SecondStationFreq { get; set; }




    }
}
