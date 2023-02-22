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
    public class GetArbData
    {
        private List<ArbModel> allBoundaryPoints = new List<ArbModel>();
        private List<BoundryModel> allBoundaries = new List<BoundryModel>();

        /// <summary>
        /// Calls all internal functions in order to process the FAA Boundary Data.
        /// </summary>
        /// <param name="effectiveDate">Airacc Effective Date (e.x. "2021-10-07")</param>
        public void ArbMain(string effectiveDate)
        {
            Logger.LogMessage("INFO", "STARTED ARB");

            ParseArb(effectiveDate);

            WriteArbGeo();


            WriteArbSct();
            Logger.LogMessage("INFO", "COMPLETED ARB");

        }

        private void WriteArbGeo()
        {

            //if (boundry.Type == "HIGH" || boundry.Type == "FIR ONLY" || boundry.Type == "UTA")
            //else if (boundry.Type == "LOW" || boundry.Type == "CTA" || boundry.Type == "BDRY")
            //else
            //{
            //    // If we get here the FAA created a new Boundary Type. Need to handle that when it comes. 
            //    throw new NotImplementedException();
            //}

            string boundaryHighFile = $"{GlobalConfig.outputDirectory}\\CRC\\ARTCC BOUNDARIES-HIGH_lines.geojson";
            string boundaryLowFile = $"{GlobalConfig.outputDirectory}\\CRC\\ARTCC BOUNDARIES-LOW_lines.geojson";


            FeatureCollection highGeo = new FeatureCollection();
            List<Feature> highFeatures = new List<Feature>();


            FeatureCollection lowGeo = new FeatureCollection();
            List<Feature> lowFeatures = new List<Feature>();

            bool isHigh = false;
            foreach (BoundryModel boundryModel in allBoundaries)
            {
                if (boundryModel.Type == "HIGH" || boundryModel.Type == "FIR ONLY" || boundryModel.Type == "UTA") 
                {
                    isHigh = true;
                }
                else if (boundryModel.Type == "LOW" || boundryModel.Type == "CTA" || boundryModel.Type == "BDRY") 
                {
                    isHigh = false;
                }
                else
                {
                    // If we get here the FAA created a new Boundary Type. Need to handle that when it comes. 
                    throw new NotImplementedException("BOUNDARY TYPE NOT FOUND: " + boundryModel.Type);
                }

                double prevLat = -999;
                double prevLon = -999;
                double currentLat = -999;
                double currentLon = -999;
                Feature feature = null;
                foreach (ArbModel arb in boundryModel.AllPoints)
                {
                    if (prevLat == -999 || prevLon == -999)
                    {
                        feature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() } };

                        prevLat = double.Parse(LatLonHelpers.CreateDecFormat(arb.Lat, false));
                        prevLon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(arb.Lon, false)));

                        feature.geometry.coordinates.Add(new List<double> { prevLon, prevLat });
                        continue;
                    }
                    currentLat = double.Parse(LatLonHelpers.CreateDecFormat(arb.Lat, false));
                    currentLon = LatLonHelpers.CorrectIlleagleLon(double.Parse(LatLonHelpers.CreateDecFormat(arb.Lon, false)));

                    bool crossesAM = false;
                    var coords = LatLonHelpers.CheckAMCrossing(prevLat, prevLon, currentLat, currentLon);
                    if (coords.Count() == 4) { crossesAM = true; }

                    if (crossesAM)
                    {
                        feature.geometry.coordinates.Add(coords[1]);

                        if (isHigh)
                        {
                            highFeatures.Add(feature);
                        }
                        else
                        {
                            lowFeatures.Add(feature);
                        }

                        feature = new Feature() { type = "Feature", geometry = new Geometry() { type = "LineString", coordinates = new List<dynamic>() } };
                        feature.geometry.coordinates.Add(coords[2]);
                        feature.geometry.coordinates.Add(coords[3]);
                    }
                    else
                    {
                        feature.geometry.coordinates.Add(new List<double> { currentLon, currentLat });
                    }

                    prevLat = currentLat;
                    prevLon = currentLon;
                }
                if (isHigh)
                {
                    highFeatures.Add(feature);
                }
                else
                {
                    lowFeatures.Add(feature);
                }
            }

            if (lowFeatures.Count() >= 1)
            {
                lowGeo.features = lowFeatures;
                string json = JsonConvert.SerializeObject(lowGeo, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(boundaryLowFile, json);
            }

            if (highFeatures.Count() >=1 )
            {
                highGeo.features = highFeatures;
                string json = JsonConvert.SerializeObject(highGeo, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(boundaryHighFile, json);
            }
        }

        /// <summary>
        /// Parse and create our ARB Model
        /// </summary>
        /// <param name="effectiveDate">Airacc Effective Date (e.x. "2021-10-07")</param>
        private void ParseArb(string effectiveDate)
        {
            Logger.LogMessage("INFO", "STARTED ARB PARSING");

            foreach (string line in File.ReadAllLines($"{GlobalConfig.tempPath}\\{effectiveDate}_ARB\\ARB.txt"))
            {
                ArbModel arb = new ArbModel
                {
                    Identifier = line.Substring(0, 4).Trim(),
                    CenterName = line.Substring(12, 40).Trim(),
                    DecodeName = line.Substring(52, 10).Trim(),
                    Lat = LatLonHelpers.CorrectLatLon(line.Substring(62, 14).Trim(), true, GlobalConfig.Convert),
                    Lon = LatLonHelpers.CorrectLatLon(line.Substring(76, 14).Trim(), false, GlobalConfig.Convert),
                    Description = line.Substring(90, 300).Trim(),
                    Sequence = line.Substring(390, 6).Trim(),
                    Legal = line.Substring(396, 1).Trim()
                };

                if (arb.Description.IndexOf("POINT OF BEGINNING", 0) != -1)
                {
                    arb.ToBeginingAfterThis = true;
                }

                allBoundaryPoints.Add(arb);
            }

            BoundryModel Boundary = new BoundryModel();
            int totalPoints = allBoundaryPoints.Count;
            int currentPointCount = 0;

            foreach (ArbModel point in allBoundaryPoints)
            {
                currentPointCount += 1;
                if (point.Identifier == Boundary.Identifier && point.DecodeName == Boundary.Type)
                {
                    Boundary.AllPoints.Add(point);
                    if (point.ToBeginingAfterThis)
                    {
                        Boundary.AllPoints.Add(Boundary.AllPoints.First());
                        allBoundaries.Add(Boundary);
                        Boundary = new BoundryModel();
                    }
                }
                else
                {
                    if (Boundary.Identifier != null)
                    {
                        allBoundaries.Add(Boundary);
                    }

                    Boundary = new BoundryModel
                    {
                        Identifier = point.Identifier,
                        Type = point.DecodeName,
                        AllPoints = new List<ArbModel>()
                    };
                    Boundary.AllPoints.Add(point);
                }

                if (totalPoints == currentPointCount && Boundary.Identifier != null)
                {
                    allBoundaries.Add(Boundary);
                }
            }
            Logger.LogMessage("INFO", "COMPLETED ARB PARSING");

        }

        /// <summary>
        /// Write our ARB Sector Files for VRC. Will create both HIGH and LOW .sct2 Files.
        /// </summary>
        private void WriteArbSct()
        {
            Logger.LogMessage("INFO", "STARTED ARB SAVING SCT DATA");

            // HIGH ARTCC = HIGH, FIR_ONLY, UTA
            // LOW ARTCC  = LOW, CTA, BDRY

            string highFilePath = $"{GlobalConfig.outputDirectory}\\VRC\\[ARTCC HIGH].sct2";
            string lowFilePath = $"{GlobalConfig.outputDirectory}\\VRC\\[ARTCC LOW].sct2";
            StringBuilder highArb = new StringBuilder();
            StringBuilder lowArb = new StringBuilder();

            // ARB File Headers
            highArb.AppendLine("[ARTCC HIGH]");
            lowArb.AppendLine("[ARTCC LOW]");

            foreach (BoundryModel boundry in allBoundaries)
            {
                ArbModel prevPoint = new ArbModel();
                foreach (ArbModel arbPoint in boundry.AllPoints)
                {
                    if (prevPoint.Identifier == null)
                    {
                        prevPoint = arbPoint;
                    }
                    else
                    {
                        if (boundry.Type == "HIGH" || boundry.Type == "FIR ONLY" || boundry.Type == "UTA")
                        {
                            highArb.AppendLine($"{boundry.Identifier} {prevPoint.Lat} {prevPoint.Lon} {arbPoint.Lat} {arbPoint.Lon} ;{prevPoint.Sequence} {arbPoint.Sequence} / {prevPoint.DecodeName} {arbPoint.DecodeName}");
                        }
                        else if (boundry.Type == "LOW" || boundry.Type == "CTA" || boundry.Type == "BDRY")
                        {
                            lowArb.AppendLine($"{boundry.Identifier} {prevPoint.Lat} {prevPoint.Lon} {arbPoint.Lat} {arbPoint.Lon} ;{prevPoint.Sequence} {arbPoint.Sequence} / {prevPoint.DecodeName} {arbPoint.DecodeName}");
                        }
                        else
                        {
                            // If we get here the FAA created a new Boundary Type. Need to handle that when it comes. 
                            throw new NotImplementedException();
                        }

                        prevPoint = arbPoint;
                    }
                }
            }

            highArb.AppendLine("\n\n\n\n\n\n");
            lowArb.AppendLine("\n\n\n\n\n\n");

            File.WriteAllText(highFilePath, highArb.ToString());
            File.WriteAllText(lowFilePath, lowArb.ToString());
            Logger.LogMessage("DEBUG", "SAVED HIGH AND LOW ARB SCT FILES");

            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(highFilePath));
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(lowFilePath));
            Logger.LogMessage("DEBUG", "ADDING TO TEST SCT FILE");


            Logger.LogMessage("INFO", "COMPLETED ARB SAVING SCT DATA");

        }
    }
}
