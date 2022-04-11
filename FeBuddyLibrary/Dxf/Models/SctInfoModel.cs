using System.Collections.Generic;

namespace FeBuddyLibrary.Dxf.Models
{
    public class SctInfoModel
    {
        public string Header { get; set; }

        public string SctFileName { get; set; }
        public string DefaultCallsign { get; set; }
        public string DefaultAirport { get; set; }
        public string CenterLat { get; set; }
        public string CenterLon { get; set; }
        public string NMPerLat { get; set; }
        public string NMPerLon { get; set; }
        public string MagneticVariation { get; set; }
        public string SctScale { get; set; }
        public string[] AdditionalLines { get; set; }

        public string AllInfo { 
            get
            {
                string output = $"{Header}\n{SctFileName}\n{DefaultCallsign}\n{DefaultAirport}\n{CenterLat}\n{CenterLon}\n{NMPerLat}\n{NMPerLon}\n{MagneticVariation}\n{SctScale}\n";

                if (AdditionalLines?.Length > 0)
                {
                    foreach (string line in AdditionalLines)
                    {
                        output += $"{line}\n";
                    }
                }

                return output;
            }
        }

    }
}