using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeBuddyLibrary.Models;
using Newtonsoft.Json;


namespace FeBuddyLibrary.DataAccess
{
    public class AwyDmeCutoffXml
    {
        private readonly string _highAwyFile;
        private readonly string _lowAwyFile;
        private readonly string _outHighAwyFile;
        private readonly string _outLowAwyFile;
        StringBuilder AwyHighSB;
        StringBuilder AwyLowSB;

        public AwyDmeCutoffXml(string highAwyFile, string lowAwyFile)
        {
            AwyHighSB = new StringBuilder();
            AwyLowSB = new StringBuilder();

            AwyHighSB.AppendLine("        <GeoMapObject Description=\"HI AIRWAYS DME\" TdmOnly=\"false\">");
            AwyHighSB.AppendLine("          <LineDefaults Bcg=\"5\" Filters=\"5\" Style=\"Solid\" Thickness=\"1\" />");
            AwyHighSB.AppendLine("          <Elements>");

            AwyLowSB.AppendLine("        <GeoMapObject Description=\"LO AIRWAYS DME\" TdmOnly=\"false\">");
            AwyLowSB.AppendLine("          <LineDefaults Bcg=\"15\" Filters=\"15\" Style=\"Solid\" Thickness=\"1\" />");
            AwyLowSB.AppendLine("          <Elements>");
            _highAwyFile = highAwyFile;
            _lowAwyFile = lowAwyFile;
            _outHighAwyFile = Path.Combine(GlobalConfig.outputDirectory, "VERAM", "AWY-HI_(DME Cutoff).xml");
            _outLowAwyFile = Path.Combine(GlobalConfig.outputDirectory, "VERAM", "AWY-LO_(DME Cutoff).xml");
        }

        public void writeXML()
        {
            ReadGeoJsonFile();

            AwyHighSB.AppendLine("          </Elements>");
            AwyHighSB.AppendLine("        </GeoMapObject>");

            AwyLowSB.AppendLine("          </Elements>");
            AwyLowSB.AppendLine("        </GeoMapObject>");

            File.WriteAllText(_outHighAwyFile, AwyHighSB.ToString());
            File.WriteAllText(_outLowAwyFile, AwyLowSB.ToString());
        }

        private void ReadGeoJsonFile()
        {
            FeatureCollection highAwyFeatures = JsonConvert.DeserializeObject<FeatureCollection>(File.ReadAllText(_highAwyFile));
            FeatureCollection lowAwyFeatures = JsonConvert.DeserializeObject<FeatureCollection>(File.ReadAllText(_lowAwyFile));

            foreach (FeatureCollection awyFeatures in new[] { highAwyFeatures, lowAwyFeatures })
            {
                foreach (Feature itemFeature in awyFeatures.features)
                {
                    if (itemFeature.geometry.coordinates.Count() != 2)
                    {
                        string message = $"The Geojson for {(awyFeatures == highAwyFeatures ? "High" : "Low")} Airways DME Cutoff contains data that I cannot properly process. Problem: Feature Coordinates count is {(itemFeature.geometry.coordinates.Count() > 2 ? ">" : "not")} 2. {itemFeature.geometry.coordinates}";

                        throw new InvalidDataException(message);
                    }

                    var startLat = itemFeature.geometry.coordinates[0][1];
                    var startLon = itemFeature.geometry.coordinates[0][0];
                    var endLat = itemFeature.geometry.coordinates[1][1];
                    var endLon = itemFeature.geometry.coordinates[1][0];

                    string line = $"            <Element xsi:type=\"Line\" Filters=\"\" StartLat=\"{startLat}\" StartLon=\"{startLon}\" EndLat=\"{endLat}\" EndLon=\"{endLon}\" />";
                    if (awyFeatures == highAwyFeatures)
                    {
                        AwyHighSB.AppendLine(line);
                    }
                    else
                    {
                        AwyLowSB.AppendLine(line);
                    }
                }
            }
        }







    }
}
