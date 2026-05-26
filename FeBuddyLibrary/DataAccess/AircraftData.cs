using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models;
using Newtonsoft.Json;

namespace FeBuddyLibrary.DataAccess
{
    public class AircraftData
    {
        AircraftDataRootObject AllAircraftData;

        public void CreateAircraftDataAlias(string outputFilePath)
        {
            AllAircraftData = GetACDataAsync().Result;
            WriteACData(outputFilePath);
        }

        private async Task<AircraftDataRootObject> GetACDataAsync()
        {
            HttpClient client = new HttpClient();

            var url = "https://www4.icao.int/doc8643/External/AircraftTypes";
            var values = new Dictionary<string, string>
            {
                {"Connection", "keep-alive" },
                {"Content-Length", "0" },
                {"Pragma", "no-cache" },
                {"Cache-Control", "no-cache" },
                {"Content-Type", "application/json" }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(url, content);

            string responseStringJson = await response.Content.ReadAsStringAsync();

            responseStringJson = "{\"AllAircraftData\":" + responseStringJson + "}";

            AircraftDataRootObject output = JsonConvert.DeserializeObject<AircraftDataRootObject>(responseStringJson);

            return output;
        }

        private void WriteACData(string outputFilePath)
        {
            string command;
            StringBuilder aliasFileSB = new StringBuilder();

            Dictionary<string, AircraftDataInformation> uniqueACData = getUniqueACData(AllAircraftData);

            Dictionary<string, string> weights = new Dictionary<string, string>()
            {
                {"L", "Light" },
                {"M", "Medium" },
                {"H", "Heavy" },
                {"L/M", "Light/Medium" },
                {"J", "Super" }
            };

            foreach (string acDesignator in uniqueACData.Keys)
            {
                AircraftDataInformation currentAircraftData = uniqueACData[acDesignator];
                // TODO - Figure out SRS and C/D
                try
                {
                    command = $".ACINFO{acDesignator} .ECHO ACINFO\n [CODE] {acDesignator}\n [MAKE] {currentAircraftData.ManufacturerCode}\n [MODEL] {currentAircraftData.ModelFullName}\n [ENGINE] {currentAircraftData.EngineCount}/{currentAircraftData.EngineType}\n [WEIGHT] {weights[currentAircraftData.WTC]}\n [C/D] ???/???\n [SRS] ???";
                }
                catch (KeyNotFoundException e)
                {
                    Logger.LogMessage("WARNING", e.Message);
                    command = $".ACINFO{acDesignator} .ECHO ACINFO\n [CODE] {acDesignator}\n [MAKE] {currentAircraftData.ManufacturerCode}\n [MODEL] {currentAircraftData.ModelFullName}\n [ENGINE] {currentAircraftData.EngineCount}/{currentAircraftData.EngineType}\n [WEIGHT] {currentAircraftData.WTC}\n [C/D] ???/???\n [SRS] ???";
                }


                aliasFileSB.AppendLine(command);
            }

            File.WriteAllText(outputFilePath, aliasFileSB.ToString());
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\ALIAS\\AliasTestFile.txt", aliasFileSB.ToString());
        }

        private Dictionary<string, AircraftDataInformation> getUniqueACData(AircraftDataRootObject aircraftData)
        {
            Dictionary<string, AircraftDataInformation> uniqueAircraftData = new Dictionary<string, AircraftDataInformation>();

            foreach (AircraftDataInformation acData in aircraftData.AllAircraftData)
            {
                if (uniqueAircraftData.ContainsKey(acData.Designator))
                {
                    AircraftDataInformation tempAircraftData = uniqueAircraftData[acData.Designator];

                    PropertyInfo[] properties = typeof(AircraftDataInformation).GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        bool acDataIsNull = property.GetValue(acData) is null;
                        bool tempAircraftDataIsNull = property.GetValue(tempAircraftData) is null;

                        if (!acDataIsNull && !tempAircraftDataIsNull)
                        {
                            if (property.GetValue(tempAircraftData).ToString().Contains(","))
                            {
                                string[] propertyContains = property.GetValue(tempAircraftData).ToString().Split(",");

                                if (propertyContains.Length > 0 && propertyContains.Contains(property.GetValue(acData).ToString()))
                                {
                                    continue;
                                }
                            }

                            if (property.GetValue(acData).ToString() != property.GetValue(tempAircraftData).ToString())
                            {
                                property.SetValue(uniqueAircraftData[acData.Designator], property.GetValue(acData).ToString() + ", " + property.GetValue(tempAircraftData).ToString());
                            }
                        }
                        else if (acDataIsNull && !tempAircraftDataIsNull)
                        {
                            property.SetValue(uniqueAircraftData[acData.Designator], property.GetValue(tempAircraftData).ToString());
                        }
                        else if (!acDataIsNull && tempAircraftDataIsNull)
                        {
                            property.SetValue(uniqueAircraftData[acData.Designator], property.GetValue(acData).ToString());
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    uniqueAircraftData[acData.Designator] = acData;
                }
            }

            return uniqueAircraftData;
        }
    }
}
