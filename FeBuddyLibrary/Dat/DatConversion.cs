using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FeBuddyLibrary.Helpers.LatLonHelpers;

namespace FeBuddyLibrary.Dat
{
    public class DatConversion
    {
        private void ReadDAT(string filepath, double distanceFromCenter = -1)
        {
            distanceFromCenter = distanceFromCenter * 1609.344;

            var datFileLines = File.ReadAllLines(filepath).ToList();

            string datFileName = datFileLines.Where(x => x.Contains("Filename:")).First().Split(':')[^1].Trim().Split('.')[0];

            // Point of Tangency
            string potString = datFileLines.Where(x => x.Contains("9900")).First().Split("9900")[^1].Trim();
            Loc PointOfTangencyCoord = new Loc
            {
                Latitude = double.Parse(CreateDecFormat(CorrectLatLon($"N0{potString.Split(' ')[0]}.{potString.Split(' ')[1]}.{potString.Split(' ')[2]}", true, false), false)),
                Longitude = double.Parse(CreateDecFormat(CorrectLatLon($"W{potString.Split(' ')[4]}.{potString.Split(' ')[5]}.{potString.Split(' ')[6]}", false, false), false))
            };

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"{datFileName.PadRight(26, ' ')}N000.00.00.000 W000.00.00.000 N000.00.00.000 W000.00.00.000");

            bool isInLineSection = false;
            bool isSecondCoord = false;
            string previousCoords = "";
            foreach (string line in datFileLines)
            {
                if (line.Contains("LINE"))
                {
                    isInLineSection = true;
                    isSecondCoord = false;
                    previousCoords = "";
                    continue;
                }

                if (isInLineSection)
                {
                    string[] splitLine = line.Split(' ');

                    string lat_cord = CorrectLatLon($"N0{splitLine[1]}.{splitLine[2]}.{splitLine[3]}", true, false);
                    string lon_cord = CorrectLatLon($"W{splitLine[5]}.{splitLine[6]}.{splitLine[7]}", true, false);

                    Loc coord = new Loc { Latitude = double.Parse(CreateDecFormat(lat_cord, true)), Longitude = double.Parse(CreateDecFormat(lon_cord, true)) };

                    if (isSecondCoord)
                    {
                        sb.AppendLine("".PadRight(26, ' ') + $"{previousCoords} {lat_cord} {lon_cord}");
                        previousCoords = $"{lat_cord} {lon_cord}";
                    }
                    else
                    {
                        if (Math.Abs(CalculateDistance(PointOfTangencyCoord, coord)) < distanceFromCenter)
                        {
                            previousCoords = $"{lat_cord} {lon_cord}";
                            isSecondCoord = true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }

            File.WriteAllText(@"C:\Users\nikol\Desktop\DAT TESTING\Testing.sct2", sb.ToString());
        }
    }
}


