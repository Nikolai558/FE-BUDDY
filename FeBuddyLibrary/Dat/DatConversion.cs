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
            Location_test PointOfTangencyCoord = new Location_test
            {
                Latitude = double.Parse(CreateDecFormat(CorrectLatLon($"N0{potString.Split(' ')[0]}.{potString.Split(' ')[1]}.{potString.Split(' ')[2]}", true, false), false)),
                Longitude = double.Parse(CreateDecFormat(CorrectLatLon($"W{potString.Split(' ')[4]}.{potString.Split(' ')[5]}.{potString.Split(' ')[6]}", false, false), false))
            };

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[INFO]\nZLC ARTCC\nSLC_33_CTR\nKSLC\nN043.31.08.418\nW112.03.50.103\n60.043\n43.536\n-11.8");

            sb.AppendLine("[STAR]");
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

                    Location_test coord = new Location_test { Latitude = double.Parse(CreateDecFormat(lat_cord, true)), Longitude = double.Parse(CreateDecFormat(lon_cord, true)) };

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
        public double CalculateDistance(Location_test point1, Location_test point2)
        {
            var d1 = point1.Latitude * (Math.PI / 180.0);
            var num1 = point1.Longitude * (Math.PI / 180.0);
            var d2 = point2.Latitude * (Math.PI / 180.0);
            var num2 = point2.Longitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
    }

}

public class Location_test
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
