using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models;
using Newtonsoft.Json;

namespace FeBuddyLibrary.DataAccess
{
    public class GetAtsAwyData
    {
        List<atsAwyPointModel> allAtsAwyPoints = new List<atsAwyPointModel>();

        //// Create empty list to hold all of the Airways.
        List<AtsAirwayModel> allAtsAwy = new List<AtsAirwayModel>();

        Dictionary<string, List<string>> awyFixes = new Dictionary<string, List<string>>();

        /// <summary>
        /// Calls All the needed functions.
        /// </summary>
        /// <param name="effectiveDate">Format: YYYY-MM-DD</param>
        public void AWYQuarterbackFunc(string effectiveDate)
        {
            Logger.LogMessage("INFO", "STARTED AWY");

            ParseAtsData(effectiveDate);

            WriteGeojson();

            WriteAwySctData();
            WriteAwyAlias();
            Logger.LogMessage("INFO", "COMPLETED AWY");

        }

        private void WriteGeojson()
        {
            string awyHighLinesSplitFile = $"{GlobalConfig.outputDirectory}\\CRC\\AWY-HIGH_lines(DME Cutoff).geojson";
            string awyHighLinesFile = $"{GlobalConfig.outputDirectory}\\CRC\\AWY-HIGH_lines.geojson";
            string awyHighSymbolsFile = $"{GlobalConfig.outputDirectory}\\CRC\\AWY-HIGH_symbols.geojson";
            string awyHighTextFile = $"{GlobalConfig.outputDirectory}\\CRC\\AWY-HIGH_text.geojson";

            FeatureCollection awyHighLines = ReadJson(awyHighLinesFile);
            FeatureCollection awyHighSplitLines = ReadJson(awyHighLinesSplitFile);
            FeatureCollection awyHighSymbols = ReadJson(awyHighSymbolsFile);
            FeatureCollection awyHighText = ReadJson(awyHighTextFile);

            foreach (AtsAirwayModel airway in allAtsAwy)
            {
                foreach (atsAwyPointModel awyPoint in airway.atsAwyPoints)
                {
                    AddFeatures(awyPoint, awyHighText.features, awyHighSymbols.features);
                }

                AddLineFeatures(airway, awyHighLines.features);
                AddSplitAwyFeatures(airway, awyHighSplitLines.features);
            }


            SerializeToFile(awyHighSymbols, awyHighSymbolsFile);
            SerializeToFile(awyHighText, awyHighTextFile);
            SerializeToFile(awyHighLines, awyHighLinesFile);
            SerializeToFile(awyHighSplitLines, awyHighLinesSplitFile);
        }

        private void AddSplitAwyFeatures(AtsAirwayModel airway, List<Feature> features)
        {
            for (int i = 0; i < airway.atsAwyPoints.Count - 1; i++)
            {
                Tuple<double, double> start = Tuple.Create(double.Parse(airway.atsAwyPoints[i].Dec_Lat), LatLonHelpers.CorrectIlleagleLon(double.Parse(airway.atsAwyPoints[i].Dec_Lon)));
                Tuple<double, double> end = Tuple.Create(double.Parse(airway.atsAwyPoints[i + 1].Dec_Lat), LatLonHelpers.CorrectIlleagleLon(double.Parse(airway.atsAwyPoints[i + 1].Dec_Lon)));
                double startDistance = airway.atsAwyPoints[i].Type switch
                {
                    "NDB" or "NDB/DME" or "VOR" or "VOR/DME" or "VORTAC" or "DME" or "TACAN" => 5,
                    _ => 2,
                };
                double endDistance = airway.atsAwyPoints[i + 1].Type switch
                {
                    "NDB" or "NDB/DME" or "VOR" or "VOR/DME" or "VORTAC" or "DME" or "TACAN" => 5,
                    _ => 2,
                };

                var newStart = LatLonHelpers.GetNewPoint(start, end, startDistance);
                var newEnd = LatLonHelpers.GetNewPoint(end, start, endDistance);
                List<dynamic> coords;
                if (newStart != null && newEnd != null)
                {
                    coords = LatLonHelpers.CheckAMCrossing(newStart.Item1, newStart.Item2, newEnd.Item1, newEnd.Item2);
                }
                else if (newStart == null && newEnd != null)
                {
                    coords = LatLonHelpers.CheckAMCrossing(start.Item1, start.Item2, newEnd.Item1, newEnd.Item2);
                }
                else
                {
                    coords = LatLonHelpers.CheckAMCrossing(start.Item1, start.Item2, end.Item1, end.Item2);
                }

                bool crossesAM = (coords.Count == 4) ? true : false;
                Feature featureToAdd = new Feature
                {
                    type = "Feature",
                    geometry = new Geometry
                    {
                        type = "LineString",
                        coordinates = new List<dynamic>
                            {
                                coords[0],
                                coords[1]
                            }
                    }
                };
                if (crossesAM)
                {
                    features.Add(featureToAdd);
                    featureToAdd = new Feature
                    {
                        type = "Feature",
                        geometry = new Geometry
                        {
                            type = "LineString",
                            coordinates = new List<dynamic>
                            {
                                coords[2],
                                coords[3]
                            }
                        }
                    };
                    features.Add(featureToAdd);
                }
                else
                {
                    features.Add(featureToAdd);
                }

            }
        }

        private static void SerializeToFile(FeatureCollection feature, string path)
        {
            if (feature.features.Count >= 1)
            {
                string json = JsonConvert.SerializeObject(feature, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(path, json);
            }
        }

        private void AddLineFeatures(AtsAirwayModel airway, List<Feature> features)
        {
            Feature currentLineFeature = null;

            foreach (atsAwyPointModel awyPoint in airway.atsAwyPoints)
            {
                if (currentLineFeature == null)
                {
                    currentLineFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() } };
                    currentLineFeature.geometry.coordinates.Add(new List<double>() { LatLonHelpers.CorrectIlleagleLon(double.Parse(awyPoint.Dec_Lon)), double.Parse(awyPoint.Dec_Lat) });
                    continue;
                }

                double currentLat = double.Parse(awyPoint.Dec_Lat);
                double currentLon = LatLonHelpers.CorrectIlleagleLon(double.Parse(awyPoint.Dec_Lon));

                bool crossesAM = false;
                var coords = LatLonHelpers.CheckAMCrossing((double)currentLineFeature.geometry.coordinates.Last()[1], (double)currentLineFeature.geometry.coordinates.Last()[0], currentLat, currentLon);
                if (coords.Count() == 4) { crossesAM = true; }

                if (crossesAM)
                {
                    currentLineFeature.geometry.coordinates.Add(coords[1]);
                    features.Add(currentLineFeature);
                    currentLineFeature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() } };
                    currentLineFeature.geometry.coordinates.Add(coords[2]);
                    currentLineFeature.geometry.coordinates.Add(coords[3]);
                }
                else
                {
                    currentLineFeature.geometry.coordinates.Add(new List<double>() { currentLon, currentLat });
                }

                if (awyPoint.GapAfter || awyPoint.BorderAfter)
                {
                    features.Add(currentLineFeature);
                    currentLineFeature = null;
                }
            }

            if (currentLineFeature != null && currentLineFeature.geometry.coordinates.Count >= 1)
            {
                features.Add(currentLineFeature);
            }
        }

        private static void AddFeatures(atsAwyPointModel awyPoint, List<Feature> textFeatures, List<Feature> symbolFeatures)
        {
            var coordinates = new List<dynamic> { LatLonHelpers.CorrectIlleagleLon(double.Parse(awyPoint.Dec_Lon)), double.Parse(awyPoint.Dec_Lat) };
            var textProperties = new Properties { text = new[] { awyPoint.PointId } };
            var textFeature = new Feature { type = "Feature", geometry = new Geometry { type = "Point", coordinates = coordinates }, properties = textProperties };
            textFeatures.Add(textFeature);

            var symbolProperties = new Properties();
            symbolProperties.style = awyPoint.Type switch
            {
                "NDB" or "NDB/DME" => "ndb",
                "VOR" or "VOR/DME" or "VORTAC" or "DME" or "TACAN" => "vor",
                _ => "airwayIntersections",
            };
            var symbolFeature = new Feature { type = "Feature", geometry = new Geometry { type = "Point", coordinates = coordinates }, properties = symbolProperties };
            symbolFeatures.Add(symbolFeature);
        }

        public static FeatureCollection ReadJson(string path)
        {
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                var json = JsonConvert.DeserializeObject<FeatureCollection>(text);
                return json;
            }
            return new FeatureCollection() { features = new List<Feature>()};
        }

        /// <summary>
        /// Parse the ATS Data from the FAA
        /// </summary>
        private void ParseAtsData(string effectiveDate)
        {
            Logger.LogMessage("INFO", "STARTED AWY PARSING");

            atsAwyPointModel atsPoint = new atsAwyPointModel();

            foreach (string line in File.ReadAllLines($"{GlobalConfig.tempPath}\\{effectiveDate}_ATS\\ATS.txt"))
            {
                if (line.Substring(0, 4) == "ATS1")
                {
                    atsPoint = new atsAwyPointModel();

                    if (line.Substring(118, 1) == "X")
                    {
                        atsPoint.GapAfter = true;
                    }

                    if (line.Substring(156, 1) == " ")
                    {
                        atsPoint.EndOfAirway = true;
                    }

                    if (line.IndexOf("BORDER", 156, 40) != -1)
                    {
                        atsPoint.BorderAfter = true;
                    }
                }
                else if (line.Substring(0, 4) == "ATS2")
                {
                    atsPoint.AirwayId = line.Substring(6, 12).Trim();

                    if (atsPoint.AirwayId.Length > 6)
                    {
                        if (atsPoint.AirwayId.Substring(0, 6) == "ROUTE ")
                        {
                            List<string> newRTEString = atsPoint.AirwayId.Split(' ').ToList();
                            newRTEString[1] = newRTEString[1].TrimStart(new char[] { '0' });
                            atsPoint.AirwayId = $"RTE{newRTEString[1]}";
                        }
                    }


                    atsPoint.Name = line.Substring(25, 40).Trim();
                    atsPoint.Lat = LatLonHelpers.CorrectLatLon(line.Substring(109, 14).Trim(), true, GlobalConfig.Convert);
                    atsPoint.Lon = LatLonHelpers.CorrectLatLon(line.Substring(123, 14).Trim(), false, GlobalConfig.Convert);
                    atsPoint.Dec_Lat = LatLonHelpers.CreateDecFormat(atsPoint.Lat, true);
                    atsPoint.Dec_Lon = LatLonHelpers.CreateDecFormat(atsPoint.Lon, true);

                    if (line.Substring(65, 25).Trim() == "NDB")
                    {
                        atsPoint.Type = "NDB";
                    }
                    else
                    {
                        atsPoint.Type = "VOR";
                    }

                    if (line.Substring(90, 15).Trim() == "FIX")
                    {
                        atsPoint.Type = "FIX";
                    }

                    if (line.Substring(142, 4).Trim() != string.Empty)
                    {
                        atsPoint.PointId = line.Substring(142, 4).Trim();
                    }
                    else
                    {
                        atsPoint.PointId = atsPoint.Name;
                    }

                    allAtsAwyPoints.Add(atsPoint);
                }
            }

            AtsAirwayModel atsAwy = new AtsAirwayModel();
            int totalPoints = allAtsAwyPoints.Count();
            int currentPointCount = 0;

            foreach (atsAwyPointModel point in allAtsAwyPoints)
            {
                currentPointCount += 1;
                if (point.AirwayId == atsAwy.Id)
                {
                    atsAwy.atsAwyPoints.Add(point);
                }
                else
                {
                    if (atsAwy.Id != null)
                    {
                        allAtsAwy.Add(atsAwy);
                    }

                    atsAwy = new AtsAirwayModel();
                    atsAwy.Id = point.AirwayId;
                    atsAwy.atsAwyPoints = new List<atsAwyPointModel>();
                    atsAwy.atsAwyPoints.Add(point);
                }

                if (totalPoints == currentPointCount)
                {
                    allAtsAwy.Add(atsAwy);
                }
            }
            Logger.LogMessage("INFO", "COMPLETED AWY PARSING");

        }

        private void WriteAwyAlias()
        {
            Logger.LogMessage("INFO", "STARTED AWY ALIAS");

            foreach (atsAwyPointModel pointModel in allAtsAwyPoints)
            {

                if (!awyFixes.ContainsKey(pointModel.AirwayId))
                {
                    awyFixes.Add(pointModel.AirwayId, new List<string>());
                }

                awyFixes[pointModel.AirwayId].Add(pointModel.PointId);
            }

            string awyAliasFilePath = $"{GlobalConfig.outputDirectory}\\ALIAS\\AWY_ALIAS.txt";
            StringBuilder sb = new StringBuilder();

            foreach (string awyId in awyFixes.Keys)
            {
                string saveString = $".{awyId}F .FF ";
                string allFixesToSave = string.Join(" ", awyFixes[awyId]);
                saveString += allFixesToSave;
                sb.AppendLine(saveString);
            }

            File.AppendAllText(awyAliasFilePath, sb.ToString());
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\ALIAS\\AliasTestFile.txt", sb.ToString());
            Logger.LogMessage("INFO", "COMPLTED AWY ALIAS");

        }

        /// <summary>
        /// Write the AWY Sector File data to a file. this will be under [HIGH AIRWAYS]
        /// </summary>
        private void WriteAwySctData()
        {
            // Warning: AwyData must be preformed before AtsAwyData, Refactoring of this code is needed! 
            Logger.LogMessage("INFO", "STARTED AWY SCT WRITER");

            // Set our File Path
            string filePath = $"{GlobalConfig.outputDirectory}\\VRC\\[HIGH AIRWAY].sct2";

            StringBuilder sb = new StringBuilder();

            //sb.AppendLine("[HIGH AIRWAY]");

            foreach (AtsAirwayModel airway in allAtsAwy)
            {
                atsAwyPointModel prevPoint = new atsAwyPointModel();

                foreach (atsAwyPointModel point in airway.atsAwyPoints)
                {
                    if (prevPoint.AirwayId == null)
                    {
                        // Set Previous Point = Current Point
                        prevPoint = point;
                    }
                    else
                    {
                        sb.AppendLine($"{airway.Id.PadRight(27)}{prevPoint.Lat} {prevPoint.Lon} {point.Lat} {point.Lon} ;{prevPoint.PointId.PadRight(5)} {point.PointId.PadRight(5)}");
                        GlobalConfig.HighAwyGeoMap.AppendLine($"            <Element xsi:type=\"Line\" Filters=\"\" StartLat=\"{prevPoint.Dec_Lat}\" StartLon=\"{prevPoint.Dec_Lon}\" EndLat=\"{point.Dec_Lat}\" EndLon=\"{point.Dec_Lon}\" />");

                        if (point.GapAfter)
                        {
                            prevPoint = new atsAwyPointModel();
                            sb.AppendLine("\n;GAP\n");
                        }
                        else
                        {
                            prevPoint = point;
                        }
                    }
                }
                sb.AppendLine();

            }
            File.AppendAllText(filePath, sb.ToString());
            File.AppendAllText(filePath, $"\n\n\n\n\n\n");
            Logger.LogMessage("DEBUG", "SAVED AWY SCT FILE");

            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(filePath));
            Logger.LogMessage("DEBUG", "ADDED AWY TO TEST SCT FILE");
            Logger.LogMessage("INFO", "COMPLETED AWY SCT WRITER");

        }
    }
}
