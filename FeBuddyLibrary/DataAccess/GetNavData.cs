using FeBuddyLibrary.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FeBuddyLibrary.DataAccess
{
    public class GetNavData
    {
        private List<NDBModel> allNDBData = new List<NDBModel>();
        private List<VORModel> allVORData = new List<VORModel>();

        /// <summary>
        /// Call all the related functions to get the NAV data from the FAA.
        /// </summary>
        /// <param name="effectiveDate">Format: YYYY-MM-DD"</param>
        public void NAVQuarterbackFunc(string effectiveDate, string Artcc)
        {
            ParseNAVData(effectiveDate);
            StoreXMLData();
            WriteNAVSctData();
            WriteNavISR(Artcc);
        }

        /// <summary>
        /// Parse through the NAV data.
        /// This data contains two data types, NDB and VOR
        /// </summary>
        private void ParseNAVData(string effectiveDate)
        {
            // FAA Provides data for ALL types of NDB and VOR, we only need certain types. Exclude the one we are on if it has any of these.
            List<string> excludeTypes = new List<string> { "VOT", "FAN MARKER", "CONSOLAN", "MARINE NDB", "DECOMMISSIONED", "MARINE NDB/DME" };

            // We are looking for these types, need to catagorize NDB or VOR depending on what the current NAVE type is.
            List<string> NDBTypes = new List<string> { "NDB", "NDB/DME", "UHF/NDB" };
            List<string> VORTypes = new List<string> { "DME", "VOR", "TACAN", "VOR/DME", "VORTAC" };

            // Variable to Remove all LEADING AND TRAILING characters.
            char[] removeChars = { ' ', '.' };

            // Read the NAV.txt file, Loop through all the lines in Nav.txt
            foreach (string line in File.ReadAllLines($"{GlobalConfig.tempPath}\\{effectiveDate}_NAV\\NAV.txt"))
            {
                // Bool to tell our program if it is to exclude this current line. To start we set it to false.
                bool exclude = false;

                // Check to see if the first part of the line is "NAV1"
                if (line.IndexOf("NAV1", 0, 4) != -1)
                {
                    // If the current line we are working in has a string that matches excludeTypes, then set bool exclude to TRUE
                    foreach (string x in excludeTypes)
                    {
                        // Make sure the line does not have any of the Types we want to EXCLUDE
                        if (line.IndexOf(x) != -1)
                        {
                            // If it does, set exclude to True.
                            exclude = true;
                        }
                    }

                    // Check to see if we need to exclude it. If exclude does not equal true.
                    if (exclude != true)
                    {
                        // Checks to see if the TYPE is an NDB
                        if (NDBTypes.Contains(line.Substring(8, 20).Trim(removeChars)))
                        {
                            // Create our NDB Model
                            NDBModel individualNDB = new NDBModel
                            {
                                Id = line.Substring(28, 4).Trim(removeChars),
                                Type = line.Substring(8, 20).Trim(removeChars),
                                Name = line.Substring(42, 30).Trim(removeChars),
                                Freq = line.Substring(533, 6).Trim(removeChars),
                                Lat = GlobalConfig.CorrectLatLon(line.Substring(371, 14).Trim(removeChars), true, GlobalConfig.Convert),
                                Lon = GlobalConfig.CorrectLatLon(line.Substring(396, 14).Trim(removeChars), false, GlobalConfig.Convert)
                            };

                            // Get the Decimal Format for Lat Lon and set it in our Model.
                            individualNDB.Dec_Lat = GlobalConfig.CreateDecFormat(individualNDB.Lat, true);
                            individualNDB.Dec_Lon = GlobalConfig.CreateDecFormat(individualNDB.Lon, true);

                            // Add the NDB model we just created to our LIST of NDB Models
                            allNDBData.Add(individualNDB);
                        }

                        // Check to see if TYPE is VOR
                        else if (VORTypes.Contains(line.Substring(8, 20).Trim(removeChars)))
                        {
                            // Create our VOR Model
                            VORModel individualVOR = new VORModel
                            {
                                Id = line.Substring(28, 4).Trim(removeChars),
                                Type = line.Substring(8, 20).Trim(removeChars),
                                Name = line.Substring(42, 30).Trim(removeChars),
                                Freq = line.Substring(533, 6).Trim(removeChars),
                                Lat = GlobalConfig.CorrectLatLon(line.Substring(371, 14).Trim(removeChars), true, GlobalConfig.Convert),
                                Lon = GlobalConfig.CorrectLatLon(line.Substring(396, 14).Trim(removeChars), false, GlobalConfig.Convert)
                            };

                            // Get the Decimal Format for Lat Lon and set it in our Model.
                            individualVOR.Dec_Lat = GlobalConfig.CreateDecFormat(individualVOR.Lat, true);
                            individualVOR.Dec_Lon = GlobalConfig.CreateDecFormat(individualVOR.Lon, true);

                            // Add the VOR model we just created to our LIST of VOR Models.
                            allVORData.Add(individualVOR);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Store the XML data in our Global XML data storage for Waypoints.xml. WE can not just write it or append the data
        /// because the waypoints.xml file has airport, fix, vor, ndb data types included in it. 
        /// so we just store it in a global variable so we can create the .xml file when it has all the data we need.
        /// </summary>
        private void StoreXMLData()
        {
            // Create an Empty list for our Waypoints 
            List<Waypoint> waypointList = new List<Waypoint>();

            // Loop through all the NDB Modles in our list.
            foreach (NDBModel ndb in allNDBData)
            {
                // Create our Location Model with the lat and lon of the NDB
                Location loc = new Location { Lat = ndb.Dec_Lat, Lon = ndb.Dec_Lon };

                // Create our Waypoint Model.
                Waypoint wpt = new Waypoint
                {
                    Type = "NDB",
                    Location = loc
                };

                // Set our Waypoint ID equal to our NDB id.
                wpt.ID = ndb.Id;

                // Add the Waypoint to our list.
                waypointList.Add(wpt);
            }

            // Loop through the VOR Models in our list.
            foreach (VORModel vor in allVORData)
            {
                // Create our Location Model
                Location loc = new Location { Lat = vor.Dec_Lat, Lon = vor.Dec_Lon };

                // Create our Waypoint Model
                Waypoint wpt = new Waypoint
                {
                    Type = "VOR",
                    Location = loc
                };

                // Set our Waypoint ID equal to our VOR Id
                wpt.ID = vor.Id;

                // Add our Waypoint to our List
                waypointList.Add(wpt);
            }

            // Loop through ALL the waypoints we have stored so far and add it to our list.
            foreach (Waypoint globalWaypoint in GlobalConfig.waypoints)
            {
                // add the waypoint from our GLOBAL waypoint storage to our list.
                waypointList.Add(globalWaypoint);
            }

            // Set our GLOBAL storage of waypoints for the xml file to our list and convert it to an array.
            GlobalConfig.waypoints = waypointList.ToArray();
        }

        /// <summary>
        /// Write the Nav In scope referecne commands
        /// </summary>
        /// <param name="Artcc">User Artcc Code</param>
        private void WriteNavISR(string Artcc)
        {
            // File path to save the ISR file
            string filePath = $"{GlobalConfig.outputDirectory}\\ALIAS\\ISR_NAVAID.txt";
            string navTextGeoMapFile = $"{GlobalConfig.outputDirectory}\\vERAM\\NAVAID_TEXT_GEOMAP.xml";


            StringBuilder navTextGeoMap = new StringBuilder();
            navTextGeoMap.AppendLine("        <GeoMapObject Description=\"NAVAID TEXT\" TdmOnly=\"true\">");
            navTextGeoMap.AppendLine("          <TextDefaults Bcg=\"13\" Filters=\"13\" Size=\"1\" Underline=\"false\" Opaque=\"false\" XOffset=\"12\" YOffset=\"0\" />");
            navTextGeoMap.AppendLine("          <Elements>");

            // String builder to store all the Lines for the file
            StringBuilder sb = new StringBuilder();

            // Loop through all of the NDB's
            foreach (NDBModel ndb in allNDBData)
            {
                // Grab the name of the NDB
                string shortName = ndb.Name;

                // Bad Characters in the name, we want to remove these
                string[] removeChar = { " ", "=", "-", "." };

                // loop through the remove characters to replace them in our ShortName Variable
                foreach (var bad in removeChar)
                {
                    // Set shortName = shortName with out the bad character.
                    shortName = shortName.Replace(bad, string.Empty);
                }

                // Add both the .NAV{ID} and .NAV{Name} to the String builder.
                sb.AppendLine($".NAV{ndb.Id} .MSG {Artcc}_ISR *** {ndb.Id} {ndb.Freq} {ndb.Name} {ndb.Type}");
                sb.AppendLine($".NAV{shortName} .MSG {Artcc}_ISR *** {ndb.Id} {ndb.Freq} {ndb.Name} {ndb.Type}");

                string tempAptName = ndb.Name;
                List<char> badChar = new List<char>() { '&', '"' };

                foreach (char bad in badChar)
                {
                    tempAptName = tempAptName.Replace(bad, '-');
                }

                navTextGeoMap.AppendLine($"            <Element xsi:type=\"Text\" Filters=\"\" Lat=\"{ndb.Dec_Lat}\" Lon=\"{ndb.Dec_Lon}\" Lines=\"{ndb.Id} {tempAptName} {ndb.Type}\" />");
            }

            // Loop through all of the VOR's we have
            foreach (VORModel vor in allVORData)
            {
                // Grab the VOR Name
                string shortName = vor.Name;

                // Characters to be removed from the name
                string[] removeChar = { " ", "=", "-", "." };

                // Loop through the bad characters
                foreach (var bad in removeChar)
                {
                    // Remove the character and set the new string back to shortName
                    shortName = shortName.Replace(bad, string.Empty);
                }

                // Add both the .NAV{ID} and .NAV{Name} to the String builder.
                sb.AppendLine($".NAV{vor.Id} .MSG {Artcc}_ISR *** {vor.Id} {vor.Freq} {vor.Name} {vor.Type}");
                sb.AppendLine($".NAV{shortName} .MSG {Artcc}_ISR *** {vor.Id} {vor.Freq} {vor.Name} {vor.Type}");

                string tempAptName = vor.Name;
                List<char> badChar = new List<char>() { '&', '"' };

                foreach (char bad in badChar)
                {
                    tempAptName = tempAptName.Replace(bad, '-');
                }
                navTextGeoMap.AppendLine($"            <Element xsi:type=\"Text\" Filters=\"\" Lat=\"{vor.Dec_Lat}\" Lon=\"{vor.Dec_Lon}\" Lines=\"{vor.Id} {tempAptName} {vor.Type}\" />");

            }

            navTextGeoMap.AppendLine("          </Elements>");
            navTextGeoMap.AppendLine("        </GeoMapObject>");

            File.WriteAllText(navTextGeoMapFile, navTextGeoMap.ToString());

            // Write the string builder to the file. Both the NDB and VOR commands are inside ONE string builder.
            File.WriteAllText(filePath, sb.ToString());

            File.AppendAllText($"{GlobalConfig.outputDirectory}\\ALIAS\\AliasTestFile.txt", sb.ToString());

        }

        /// <summary>
        /// Create the SCT2 File for NDB and VOR
        /// </summary>
        private void WriteNAVSctData()
        {
            // Variable for the full file path for our two types.
            string NDBfilePath = $"{GlobalConfig.outputDirectory}\\VRC\\[NDB].sct2";
            string VORfilePath = $"{GlobalConfig.outputDirectory}\\VRC\\[VOR].sct2";

            // Create NDB String builder with all the required data.
            StringBuilder sb = new StringBuilder();

            // Add NDB to the begining of the string builder
            sb.AppendLine("[NDB]");

            // Loop through all the NDB models we have
            foreach (NDBModel ndb in allNDBData)
            {
                // Add the Line containing all the Data for the NDB into the string builder
                sb.AppendLine($"{ndb.Id.PadRight(4)}{ndb.Freq.PadRight(8)}{ndb.Lat} {ndb.Lon} ;{ndb.Name} {ndb.Type}");
            }

            // Writh the string builder to the file.
            File.WriteAllText(NDBfilePath, sb.ToString());

            // Add some blank lines to the end of the file.
            File.AppendAllText(NDBfilePath, $"\n\n\n\n\n\n");

            // Add this file data to our Sector TEST file.
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(NDBfilePath));

            // Overide the previous NDB String Builder with a new one and create VOR String builder with all the required data.
            sb = new StringBuilder();

            // Add VOR to the begining of the String builder
            sb.AppendLine("[VOR]");

            // Loop through all of our VORS
            foreach (VORModel vor in allVORData)
            {
                // add the line with all the data into our string builder
                sb.AppendLine($"{vor.Id.PadRight(4)}{vor.Freq.PadRight(8)}{vor.Lat} {vor.Lon} ;{vor.Name} {vor.Type}");
            }

            // write the string builder to a file.
            File.WriteAllText(VORfilePath, sb.ToString());

            // Add some Blank Lines to the end of the file.
            File.AppendAllText(VORfilePath, $"\n\n\n\n\n\n");

            // Add this file data to our TEST sector File.
            File.AppendAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", File.ReadAllText(VORfilePath));
        }
    }
}
