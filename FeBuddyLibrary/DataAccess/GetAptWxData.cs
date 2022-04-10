using FeBuddyLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyLibrary.DataAccess
{
    public class GetAptWxData
    {
        Dictionary<string, AptWxModel> AllAptWxModels = new Dictionary<string, AptWxModel>();


        public Dictionary<string, AptWxModel> GetAptWxDataMain(string effectiveDate)
        {
            ParseAptWxData(effectiveDate);
            return AllAptWxModels;
        }

        private void ParseAptWxData(string effectiveDate)
        {
            foreach (string line in File.ReadLines($"{GlobalConfig.tempPath}\\{effectiveDate}_AWOS\\AWOS.txt"))
            {
                // Check to make sure we are grabbing the AWOS1 stuff

                if (line.Substring(0, 5).Contains("AWOS1"))
                {
                    AptWxModel tempModel = new AptWxModel
                    {
                        SensorIdent = line.Substring(5, 4).Trim(),
                        SensorType = line.Substring(9, 10).Trim(),
                        CommissioningStatus = line.Substring(19, 1).Trim(),
                        NavaidFlag = line.Substring(30, 1).Trim(),
                        StationLat = line.Substring(31, 14).Trim(),
                        StationLon = line.Substring(45, 15).Trim(),
                        Elevation = line.Substring(60, 7).Trim(),
                        StationFreq = line.Substring(68, 7).Trim(),
                        SecondStationFreq = line.Substring(75, 7).Trim()
                    };

                    // Grab the Sensor Type UP to the '-' if it has one.
                    if (tempModel.SensorType.Contains('-'))
                    {
                        tempModel.SensorType = tempModel.SensorType.Split('-')[0];
                    }


                    if (AllAptWxModels.ContainsKey(tempModel.SensorIdent))
                    {
                        if (tempModel.CommissioningStatus == "Y")
                        {
                            if (!string.IsNullOrWhiteSpace(tempModel.StationFreq))
                            {
                                // If the station does not have a frequency do not add to alias command.
                                AllAptWxModels[tempModel.SensorIdent] = tempModel;
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(tempModel.StationFreq))
                        {
                            // If the station does not have a frequency do not add to alias command.
                            AllAptWxModels[tempModel.SensorIdent] = tempModel;
                        }
                    }
                }
                else
                {
                    continue;
                }
            }
        }
    }
}
