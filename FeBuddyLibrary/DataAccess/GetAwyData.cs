using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FeBuddyLibrary.DataAccess
{
    public class GetAwyData
    {
        List<AwyPointModel> allAWYPoints = new List<AwyPointModel>();

        Dictionary<string, List<string>> awyFixes = new Dictionary<string, List<string>>();

        // Create empty list to hold all of the Airways.
        List<AirwayModel> allAwy = new List<AirwayModel>();

        /// <summary>
        /// Calls All the needed functions.
        /// </summary>
        /// <param name="effectiveDate">Format: YYYY-MM-DD</param>
        public void AWYQuarterbackFunc(string effectiveDate)
        {
            ParseAwyData(effectiveDate);
            WriteAwySctData();
            WriteAwyAlias();
        }

        /// <summary>
        /// Parse the AWY data from the FAA.
        /// </summary>
        private void ParseAwyData(string effectiveDate)
        {
            // Create our AWY point Model
            AwyPointModel awyPoint = new AwyPointModel();

            // Loop through all the Lines in the AWY.TXT file
            foreach (string line in File.ReadAllLines($"{GlobalConfig.tempPath}\\{effectiveDate}_AWY\\AWY.txt"))
            {
                // IF the Line begins with AWY1
                if (line.Substring(0, 4) == "AWY1")
                {
                    // We've hit a new point Create a new point.
                    awyPoint = new AwyPointModel();

                    // If the GAP after Flag is set
                    if (line.Substring(106, 1) == "X")
                    {
                        // Set the AWY point Model GapAfter = true.
                        awyPoint.GapAfter = true;
                    }

                    // Check to see if it has a point after the one we are on.
                    if (line.Substring(144, 1) == " ")
                    {
                        // If it does not set end of airway to true.
                        awyPoint.EndOfAirway = true;
                    }

                    // Check to see if the word Border is in the Next point section
                    if (line.IndexOf("BORDER", 144, 33) != -1)
                    {
                        // If it is, Set boder after to true.
                        awyPoint.BorderAfter = true;
                    }
                }

                // Check to see if line begins with AWY2
                else if (line.Substring(0, 4) == "AWY2")
                {
                    // Set the Awy Point Model Data
                    awyPoint.AirwayId = line.Substring(4, 5).Trim();
                    awyPoint.Name = line.Substring(15, 30).Trim();

                    // Check to see if it has a name.
                    if (line.Substring(116, 4).Trim() == string.Empty)
                    {
                        // If it does not, set the Point ID to Name.
                        awyPoint.PointId = awyPoint.Name;
                    }
                    else
                    {
                        // If it does set the point Id to ID.
                        awyPoint.PointId = line.Substring(116, 4).Trim();
                    }

                    // Set the AwyPoint Type.
                    awyPoint.Type = line.Substring(45, 19).Trim();

                    // Check to make sure the name does not have Border in it.
                    if (awyPoint.Name.IndexOf("BORDER", 0, awyPoint.Name.Length) == -1)
                    {
                        // Set the Lat Lon
                        awyPoint.Lat = LatLonHelpers.CorrectLatLon(line.Substring(83, 14).Trim(), true, GlobalConfig.Convert);
                        awyPoint.Lon = LatLonHelpers.CorrectLatLon(line.Substring(97, 14).Trim(), false, GlobalConfig.Convert);

                        // Set the Decimal Version of Lat and Lon
                        awyPoint.Dec_Lat = LatLonHelpers.CreateDecFormat(awyPoint.Lat, true);
                        awyPoint.Dec_Lon = LatLonHelpers.CreateDecFormat(awyPoint.Lon, true);

                        // Add this point to our List
                        allAWYPoints.Add(awyPoint);
                    }
                }
            }

            // Create a new Airway Model
            AirwayModel awy = new AirwayModel();

            // Set the total AWY point count and our current Point that we are on.
            int totalPoints = allAWYPoints.Count;
            int currentPointCount = 0;

            // Loop through all the AWY points in our list
            foreach (AwyPointModel point in allAWYPoints)
            {
                // add one to our current Point Count.
                currentPointCount += 1;

                // If the point ID is the same as the AWY ID
                if (point.AirwayId == awy.Id)
                {
                    // Add the point to the current AWY Model
                    awy.AwyPoints.Add(point);
                }
                else
                {
                    // Make sure the Airway ID is not Null
                    if (awy.Id != null)
                    {
                        // If it doesnt == null then we add the AWY to our list.
                        allAwy.Add(awy);
                    }

                    // Set the Airway Model Data
                    awy = new AirwayModel();
                    awy.Id = point.AirwayId;
                    awy.AwyPoints = new List<AwyPointModel>();
                    awy.AwyPoints.Add(point);
                }

                // Check to see if we are done with all of the points
                if (totalPoints == currentPointCount)
                {
                    // if we are add the airway to our list. We have to do this to make sure the last awy we create makes it into our list.
                    allAwy.Add(awy);
                }
            }
        }

        private void WriteAwyAlias()
        {
            foreach (AwyPointModel pointModel in allAWYPoints)
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

            File.WriteAllText(awyAliasFilePath, sb.ToString());
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\ALIAS\\AliasTestFile.txt", sb.ToString());

        }


        /// <summary>
        /// Write the AWY Sector File data to a file. this will be under [HIGH AIRWAYS]
        /// </summary>
        private void WriteAwySctData()
        {
            // Set our File Path
            string filePath = $"{GlobalConfig.outputDirectory}\\VRC\\[HIGH AIRWAY].sct2";

            // Create a new String Builder
            StringBuilder sb = new StringBuilder();

            // Add [HIGH AIRWAY] to the verry begining of our string builder
            sb.AppendLine("[HIGH AIRWAY]");

            // Loop through all of our Airways
            foreach (AirwayModel airway in allAwy)
            {
                // Set the Previous Point to a new AWY Point Model.
                AwyPointModel prevPoint = new AwyPointModel();

                // Loop through all of our Points in our Airway
                foreach (AwyPointModel point in airway.AwyPoints)
                {
                    // If our Previous Point is null, we need to set it to our current one.
                    if (prevPoint.AirwayId == null)
                    {
                        // Set Previous Point = Current Point
                        prevPoint = point;
                    }

                    // If it doesn't we need to add data to our string builder
                    else
                    {
                        // Add a line with the Airway data.
                        sb.AppendLine($"{airway.Id.PadRight(27)}{prevPoint.Lat} {prevPoint.Lon} {point.Lat} {point.Lon}; {prevPoint.PointId.PadRight(5)} {point.PointId.PadRight(5)}");

                        GlobalConfig.AwyGeoMap.AppendLine($"            <Element xsi:type=\"Line\" Filters=\"\" StartLat=\"{prevPoint.Dec_Lat}\" StartLon=\"{prevPoint.Dec_Lon}\" EndLat=\"{point.Dec_Lat}\" EndLon=\"{point.Dec_Lon}\" />");

                        // If there is a gap after this current point
                        if (point.GapAfter)
                        {
                            // Set previous point to a new Model. (this ensures that we have a break in the AWY)
                            prevPoint = new AwyPointModel();

                            // Add Gap in our string builder.
                            sb.AppendLine("\n;GAP\n");
                        }
                        else
                        {
                            // No gap after current point so keep going through, set our prev point to our current point.
                            prevPoint = point;
                        }
                    }
                }

                // We have looped through our Points in our Airway, Set an Empty line. and Continue to the next AWY 
                sb.AppendLine();
            }

            // Write the stringbuilder to our File.
            File.WriteAllText(filePath, sb.ToString());

            // Add some blank lines at the end of the file.
            File.AppendAllText(filePath, $"\n\n\n\n\n\n");

            // Add the file to our Test Sector File.
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(filePath));
        }
    }
}
