using System.Collections.Generic;
using FeBuddyLibrary.Helpers;

namespace FeBuddyLibrary.Models
{
    public class StarAndDpModel
    {
        public string Type { get; set; }

        public string SeqNumber { get; set; }

        public string PointCode { get; set; }

        public string PointId { get; set; }

        public string ComputerCode { get; set; }

        public string Lat_N_S { get; set; }

        public string Lat_Deg { get; set; }

        public string Lat_Min { get; set; }

        public string Lat_Sec { get; set; }

        public string Lat_MS { get; set; }

        public string Lat { get { return $"{Lat_N_S}{Lat_Deg}.{Lat_Min}.{Lat_Sec}.{Lat_MS}"; } }

        public double Dec_Lat { get { return double.Parse(LatLonHelpers.CreateDecFormat(Lat, false)); } }

        public string Lon_E_W { get; set; }

        public string Lon_Deg { get; set; }

        public string Lon_Min { get; set; }

        public string Lon_Sec { get; set; }

        public string Lon_MS { get; set; }

        public string Lon { get { return $"{Lon_E_W}{Lon_Deg}.{Lon_Min}.{Lon_Sec}.{Lon_MS}"; } }

        public double Dec_Lon { get { return LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(Lon, false))); } }


        public List<string> AirpotsThisPointServes { get; set; } = new List<string>();
    }
}
