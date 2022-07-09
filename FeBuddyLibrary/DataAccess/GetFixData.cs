using System.Collections.Generic;
using System.IO;
using System.Text;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models;

namespace FeBuddyLibrary.DataAccess
{
    public class GetFixData
    {
        private List<FixModel> allFixesInData = new List<FixModel>();

        /// <summary>
        /// Calls all the needed functions
        /// </summary>
        /// <param name="effectiveDate">Format: YYYY-MM-DD</param>
        public void FixQuarterbackFunc(string effectiveDate)
        {
            Logger.LogMessage("INFO", "STARTED FIXES");

            ParseFixData(effectiveDate);
            WriteFixSctData();
            StoreXMLData();
            Logger.LogMessage("INFO", "COMPLETED FIXES");

        }

        /// <summary>
        /// Parse through and get the data we need for all the fixes.
        /// </summary>
        private void ParseFixData(string effectiveDate)
        {
            Logger.LogMessage("INFO", "STARTED PARSING FIXES");

            // Variable for all the 'bad' characters. We will remove all these characters from the data.
            char[] removeChars = { ' ', '.' };

            // Read ALL the lines in FAA Fix data text file.
            foreach (string line in File.ReadAllLines($"{GlobalConfig.tempPath}\\{effectiveDate}_FIX\\FIX.txt"))
            {
                // Check to see if the begining of the line starts with "FIX1"
                if (line.Substring(0, 4) == "FIX1")
                {
                    // Create the FixModel. This is needed to store the data so we can later write the SCT file.
                    FixModel individualFixData = new FixModel
                    {
                        Id = line.Substring(4, 5).Trim(removeChars),
                        Lat = LatLonHelpers.CorrectLatLon(line.Substring(66, 14).Trim(removeChars), true, GlobalConfig.Convert),
                        Lon = LatLonHelpers.CorrectLatLon(line.Substring(80, 14).Trim(removeChars), false, GlobalConfig.Convert),
                        Catagory = line.Substring(94, 3).Trim(removeChars),
                        Use = line.Substring(213, 15).Trim(removeChars),
                        HiArtcc = line.Substring(233, 4).Trim(removeChars),
                        LoArtcc = line.Substring(237, 4).Trim(removeChars),
                    };

                    // Set the Decimal Format for the Lat and Lon
                    individualFixData.Lat_Dec = LatLonHelpers.CreateDecFormat(individualFixData.Lat, true);
                    individualFixData.Lon_Dec = LatLonHelpers.CreateDecFormat(individualFixData.Lon, true);

                    // Add this FIX MODEL to the list of all Fixes.
                    allFixesInData.Add(individualFixData);
                }
            }
            Logger.LogMessage("INFO", "COMPLETED PARSING FIXES");

        }

        /// <summary>
        /// Store the XML data for Fixes for waypoints.xml. We can not write this data yet because,
        /// the waypoints.xml includes airports, fixes, vor, and ndb data. So for now we just store it
        /// until we have all the data needed to write it. 
        /// </summary>
        private void StoreXMLData()
        {
            Logger.LogMessage("INFO", "STARTED STORING FIX XML DATA");

            // Create an Empty list of Waypoints, So that we can add our waypoints to this list.
            List<Waypoint> waypointList = new List<Waypoint>();

            // Loop through all the fixes we collected
            foreach (FixModel fix in allFixesInData)
            {
                // set our location model
                Location loc = new Location { Lat = fix.Lat_Dec, Lon = fix.Lon_Dec };

                // Create our Waypoint Model.
                Waypoint wpt = new Waypoint
                {
                    Type = "Intersection",
                    Location = loc
                };

                // Set our Waypoint Id to our Fix ID
                wpt.ID = fix.Id;

                // Add the waypoint to our list.
                waypointList.Add(wpt);
            }

            // Loop through all the Waypoints we have already stored and add it to our list.
            foreach (Waypoint globalWaypoint in GlobalConfig.waypoints)
            {
                // add the waypoint to our list
                waypointList.Add(globalWaypoint);
            }

            // Set our Global Waypoint list (includes airports, vor, ndb, fixes) to our List we just made. AND convert it to an Array instead of a list.
            GlobalConfig.waypoints = waypointList.ToArray();

            Logger.LogMessage("INFO", "COMPLETED STORING FIX XML DATA");

        }

        /// <summary>
        /// Create the Fix.sct2 File for VRC.
        /// </summary>
        private void WriteFixSctData()
        {
            Logger.LogMessage("INFO", "STARTED SAVING FIX SCT DATA");

            // This is where the new SCT2 File will be saved to.
            string filePath = $"{GlobalConfig.outputDirectory}\\VRC\\[FIXES].sct2";

            // String Builders are super effictient and FAST when manipulating strings in bulk.
            StringBuilder sb = new StringBuilder();

            // The very begining of the file needs to have "[FIXES]" on the first line.
            sb.AppendLine("[FIXES]");

            // Loop through ALL of the fixes we have collected and already parsed through, and add it to our string builder.
            foreach (FixModel dataforEachFix in allFixesInData)
            {
                // add the line containing all the data to our string builder.
                sb.AppendLine($"{dataforEachFix.Id.PadRight(6)}{dataforEachFix.Lat} {dataforEachFix.Lon} ;{dataforEachFix.HiArtcc}/{dataforEachFix.LoArtcc} {dataforEachFix.Catagory} {dataforEachFix.Use}");
            }

            // Write the data to the Fix.sct2 file. 
            File.WriteAllText(filePath, sb.ToString());

            // Add some blank lines to the end of the file. 
            File.AppendAllText(filePath, $"\n\n\n\n\n\n");
            Logger.LogMessage("INFO", "SAVED FIX SCT FILE");


            // Add this file to our Test Sector File.
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(filePath));
            Logger.LogMessage("INFO", "SAVED FIX SCT DATA TO TEST SCT FILE");


            Logger.LogMessage("INFO", "COMPLETED SAVING FIX SCT DATA");

        }
    }
}
