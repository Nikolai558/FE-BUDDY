using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace FeBuddyLibrary.Helpers
{
    public class FileHelpers
    {
        public static void WriteWarnMeFile()
        {
            Logger.LogMessage("DEBUG", "WRITING WARNME FILE FOR META NOT BEING INCLUDED IN DOWNLOAD / PARSING");

            string warningMSG = "\n" +
                    "WARNING:\n" +
                    "	Since FE-BUDDY was ran before the FAA    \n" +
                    "	meta file was completed for this AIRAC,  \n" +
                    "	the following files are not generated!   \n\n" +
                    "	If you need the following files, please  \n" +
                    "	run FE-BUDDY again when the FAA meta     \n" +
                    "	file is ready. This is usually available \n" +
                    "	15-18 days before the AIRAC Effective    \n" +
                    "	Date.                                    \n\n" +
                    "*********************************************\n" +
                    "**                                         **\n" +
                    "**	- PUBLICATIONS (ALL or Specific)       **\n" +
                    "**	                                       **\n" +
                    "**	- ALIAS                                **\n" +
                    "**		- FAA_CHART_RECALL.TXT             **\n" +
                    "**		                                   **\n" +
                    "*********************************************\n";

            File.WriteAllText($"{GlobalConfig.outputDirectory}\\WARN-README.txt", warningMSG);
        }

        public static void CreateAwyGeomapHeadersAndEnding(bool CreateStart)
        {

            if (CreateStart)
            {
                Logger.LogMessage("DEBUG", "CREATING AWY HI GEOMAP HEADERS");

                GlobalConfig.HighAwyGeoMap.AppendLine("        <GeoMapObject Description=\"HI AIRWAYS\" TdmOnly=\"false\">");
                GlobalConfig.HighAwyGeoMap.AppendLine("          <LineDefaults Bcg=\"5\" Filters=\"5\" Style=\"Solid\" Thickness=\"1\" />");
                GlobalConfig.HighAwyGeoMap.AppendLine("          <Elements>");

                Logger.LogMessage("DEBUG", "CREATING AWY LO GEOMAP HEADERS");

                GlobalConfig.LowAwyGeoMap.AppendLine("        <GeoMapObject Description=\"LO AIRWAYS\" TdmOnly=\"false\">");
                GlobalConfig.LowAwyGeoMap.AppendLine("          <LineDefaults Bcg=\"15\" Filters=\"15\" Style=\"Solid\" Thickness=\"1\" />");
                GlobalConfig.LowAwyGeoMap.AppendLine("          <Elements>");
            }
            else
            {
                Logger.LogMessage("DEBUG", "CREATING AWY HI GEOMAP ENDINGS");

                GlobalConfig.HighAwyGeoMap.AppendLine("          </Elements>");
                GlobalConfig.HighAwyGeoMap.AppendLine("        </GeoMapObject>");
                File.WriteAllText($"{GlobalConfig.outputDirectory}\\VERAM\\{GlobalConfig.HighAwyGeoMapFileName}", GlobalConfig.HighAwyGeoMap.ToString());
                Logger.LogMessage("INFO", "SAVED AWY HI GEOMAP");

                Logger.LogMessage("DEBUG", "CREATING AWY Low GEOMAP ENDINGS");

                GlobalConfig.LowAwyGeoMap.AppendLine("          </Elements>");
                GlobalConfig.LowAwyGeoMap.AppendLine("        </GeoMapObject>");
                File.WriteAllText($"{GlobalConfig.outputDirectory}\\VERAM\\{GlobalConfig.LowAwyGeoMapFileName}", GlobalConfig.LowAwyGeoMap.ToString());
                Logger.LogMessage("INFO", "SAVED AWY LOW GEOMAP");

            }
        }

        /// <summary>
        /// Write the Waypoints.xml File. NOTE: The Global Variable that contains all the waypoints has to be 
        /// completely filled in before we call this function!
        /// </summary>
        public static void WriteWaypointsXML()
        {
            Logger.LogMessage("INFO", "STARTED WAYPOINTS XML WRITER");

            XmlWriterSettings xmlWriterSettings = new()
            {
                Indent = true
            };

            // File path for the waypoints.xml that we want to store to.
            string filePath = $"{GlobalConfig.outputDirectory}\\VERAM\\Waypoints.xml";


            using (XmlWriter xmlWriter = XmlWriter.Create(filePath, xmlWriterSettings))
            {
                GlobalConfig.WaypointSerializer.Serialize(xmlWriter, GlobalConfig.waypoints);
            };

            // Write the XML Serializer to the file.
            //TextWriter writer = new StreamWriter(filePath);
            //GlobalConfig.WaypointSerializer.Serialize(writer, GlobalConfig.waypoints);
            //writer.Close();
            Logger.LogMessage("INFO", "COMPLETED WAYPOINTS XML WRITER");

        }

        public static void WriteNavXmlOutput()
        {
            Logger.LogMessage("INFO", "STARTED NAV XML WRITER");

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("        <GeoMapObject Description=\"NAVAIDS\" TdmOnly=\"false\">");
            sb.AppendLine("          <SymbolDefaults Bcg=\"13\" Filters=\"13\" Style=\"VOR\" Size=\"1\" />");
            sb.AppendLine("          <Elements>");

            string readFilePath = $"{GlobalConfig.outputDirectory}\\VERAM\\Waypoints.xml";
            string saveFilePath = $"{GlobalConfig.outputDirectory}\\VERAM\\NAVAID_GEOMAP.xml";

            bool grabLocation = false;

            foreach (string line in File.ReadAllLines(readFilePath))
            {
                if (line.Length > 13)
                {
                    if (line.Substring(3, 8) == "Waypoint")
                    {
                        if (line.Substring(18, 7) != "Airport" && line.Substring(18, 12) != "Intersection")
                        {
                            grabLocation = true;
                        }
                        else
                        {
                            grabLocation = false;
                        }
                    }
                    else if (line.Substring(5, 8) == "Location" && grabLocation == true)
                    {
                        int locLength = line.Length - 17;
                        List<string> latLonSplitValue = line.Substring(14, locLength).Split(' ').ToList();

                        string printString = $"            <Element xsi:type=\"Symbol\" Filters=\"\" {latLonSplitValue[1]} {latLonSplitValue[0]} />";

                        sb.AppendLine(printString);
                    }
                }
            }

            sb.AppendLine("          </Elements>");
            sb.AppendLine("        </GeoMapObject>");

            File.WriteAllText(saveFilePath, sb.ToString());
            Logger.LogMessage("INFO", "COMPLETED NAV XML WRITER");

        }

        /// <summary>
        /// Add Coment to the end of the XML file for Kyle's Batch File
        /// </summary>
        /// <param name="AiracDate">Correct Format: YYYY-MM-DD</param>
        public static void AppendCommentToXML(string AiracDate)
        {

            // File path to the Waypoints.xml File.
            string filepath = $"{GlobalConfig.outputDirectory}\\VERAM\\Waypoints.xml";

            // Add the Airac effective date in comment form to the end of the file.
            File.AppendAllText(filepath, $"\n<!--AIRAC_EFFECTIVE_DATE {AiracDate}-->");

            // Copy the file from VERAM to VSTARS. - They use the same format. 
            File.Copy($"{GlobalConfig.outputDirectory}\\VERAM\\Waypoints.xml", $"{GlobalConfig.outputDirectory}\\VSTARS\\Waypoints.xml");
            Logger.LogMessage("INFO", "ADDED COMMENTS TO WAYPOINTS XML");
        }

        /// <summary>
        /// Write a test sector file for VRC.
        /// </summary>
        public static void WriteTestSctFile()
        {
            // Write the file INFO section.
            Logger.LogMessage("DEBUG", "CREATING THE TEST SECTOR FILE ");
            File.WriteAllText($"{GlobalConfig.outputDirectory}\\{GlobalConfig.testSectorFileName}", $"[INFO]\nTEST_SECTOR\nTST_CTR\nXXXX\nN043.31.08.418\nW112.03.50.103\n60.043\n43.536\n-11.8\n1.000\n\n\n\n\n");
        }
    }
}
