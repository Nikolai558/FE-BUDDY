using FeBuddyLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
            ParseArb(effectiveDate);
            WriteArbSct();
        }

        /// <summary>
        /// Parse and create our ARB Model
        /// </summary>
        /// <param name="effectiveDate">Airacc Effective Date (e.x. "2021-10-07")</param>
        private void ParseArb(string effectiveDate)
        {
            foreach (string line in File.ReadAllLines($"{GlobalConfig.tempPath}\\{effectiveDate}_ARB\\ARB.txt"))
            {
                ArbModel arb = new ArbModel
                {
                    Identifier = line.Substring(0, 4).Trim(),
                    CenterName = line.Substring(12, 40).Trim(),
                    DecodeName = line.Substring(52, 10).Trim(),
                    Lat = GlobalConfig.CorrectLatLon(line.Substring(62, 14).Trim(), true, GlobalConfig.Convert),
                    Lon = GlobalConfig.CorrectLatLon(line.Substring(76, 14).Trim(), false, GlobalConfig.Convert),
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
        }

        /// <summary>
        /// Write our ARB Sector Files for VRC. Will create both HIGH and LOW .sct2 Files.
        /// </summary>
        private void WriteArbSct()
        {
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
                            highArb.AppendLine($"{boundry.Identifier} {prevPoint.Lat} {prevPoint.Lon} {arbPoint.Lat} {arbPoint.Lon}; {prevPoint.Sequence} {arbPoint.Sequence} / {prevPoint.DecodeName} {arbPoint.DecodeName}");
                        }
                        else if (boundry.Type == "LOW" || boundry.Type == "CTA" || boundry.Type == "BDRY")
                        {
                            lowArb.AppendLine($"{boundry.Identifier} {prevPoint.Lat} {prevPoint.Lon} {arbPoint.Lat} {arbPoint.Lon}; {prevPoint.Sequence} {arbPoint.Sequence} / {prevPoint.DecodeName} {arbPoint.DecodeName}");
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
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(highFilePath));
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(lowFilePath));
        }
    }
}
