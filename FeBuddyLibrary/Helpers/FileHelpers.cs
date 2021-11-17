using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyLibrary.Helpers
{
    public class FileHelpers
    {
        private static string OutputPath { get; } = GlobalConfig.outputDirectory;

        public static void WriteWarnMeFile() 
        {
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

            File.WriteAllText($"{OutputPath}\\WARN-README.txt", warningMSG);
        }

        public static void CreateAwyGeomapHeadersAndEnding(bool CreateStart)
        {
            if (CreateStart)
            {
                GlobalConfig.AwyGeoMap.AppendLine("        <GeoMapObject Description=\"AIRWAYS\" TdmOnly=\"false\">");
                GlobalConfig.AwyGeoMap.AppendLine("          <LineDefaults Bcg=\"4\" Filters=\"4\" Style=\"ShortDashed\" Thickness=\"1\" />");
                GlobalConfig.AwyGeoMap.AppendLine("          <Elements>");
            }
            else
            {
                GlobalConfig.AwyGeoMap.AppendLine("          </Elements>");
                GlobalConfig.AwyGeoMap.AppendLine("        </GeoMapObject>");
                File.WriteAllText($"{OutputPath}\\VERAM\\{GlobalConfig.AwyGeoMapFileName}", GlobalConfig.AwyGeoMap.ToString());
            }
        }

        /// <summary>
        /// Write the Waypoints.xml File. NOTE: The Global Variable that contains all the waypoints has to be 
        /// completely filled in before we call this function!
        /// </summary>
        public static void WriteWaypointsXML()
        {
            // File path for the waypoints.xml that we want to store to.
            string filePath = $"{OutputPath}\\VERAM\\Waypoints.xml";

            // Write the XML Serializer to the file.
            TextWriter writer = new StreamWriter(filePath);
            GlobalConfig.WaypointSerializer.Serialize(writer, GlobalConfig.waypoints);
            writer.Close();
        }

        public static void WriteNavXmlOutput()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("        <GeoMapObject Description=\"NAVAIDS\" TdmOnly=\"false\">");
            sb.AppendLine("          <SymbolDefaults Bcg=\"13\" Filters=\"13\" Style=\"VOR\" Size=\"1\" />");
            sb.AppendLine("          <Elements>");

            string readFilePath = $"{OutputPath}\\VERAM\\Waypoints.xml";
            string saveFilePath = $"{OutputPath}\\VERAM\\NAVAID_GEOMAP.xml";

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
        }

        /// <summary>
        /// Add Coment to the end of the XML file for Kyle's Batch File
        /// </summary>
        /// <param name="AiracDate">Correct Format: YYYY-MM-DD</param>
        public static void AppendCommentToXML(string AiracDate)
        {
            // File path to the Waypoints.xml File.
            string filepath = $"{OutputPath}\\VERAM\\Waypoints.xml";

            // Add the Airac effective date in comment form to the end of the file.
            File.AppendAllText(filepath, $"\n<!--AIRAC_EFFECTIVE_DATE {AiracDate}-->");

            // Copy the file from VERAM to VSTARS. - They use the same format. 
            File.Copy($"{OutputPath}\\VERAM\\Waypoints.xml", $"{OutputPath}\\VSTARS\\Waypoints.xml");
        }

        /// <summary>
        /// Write a test sector file for VRC.
        /// </summary>
        public static void WriteTestSctFile()
        {
            // Write the file INFO section.
            File.WriteAllText($"{OutputPath}\\{GlobalConfig.testSectorFileName}", $"[INFO]\nTEST_SECTOR\nTST_CTR\nXXXX\nN043.31.08.418\nW112.03.50.103\n60.043\n43.536\n-11.8\n1.000\n\n\n\n\n");
        }
    }
}
