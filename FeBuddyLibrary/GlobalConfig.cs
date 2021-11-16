using FeBuddyLibrary.Models;
using FeBuddyLibrary.Models.MetaFileModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Xml.Serialization;

namespace FeBuddyLibrary
{
    public class GlobalConfig
    {
        public static readonly string ProgramVersion = "0.9.2";

        public static string GithubVersion = "";

        public static string airacEffectiveDate = "";

        public static string FaaHtmlFileVariable { get; protected set; } = DateTime.Now.ToString("MMddHHmmss");

        private static List<string> DownloadedFilePaths = new List<string>();

        public static readonly string testSectorFileName = $"\\VRC\\TestSectorFile.sct2";

        public static bool updateProgram = false;

        public static List<AssetsModel> AllAssetsToDownload = new List<AssetsModel>();

        public static string ReleaseBody = "";

        public static StringBuilder AwyGeoMap = new StringBuilder();
        public static string AwyGeoMapFileName = "AWY_GEOMAP.xml";

        public static List<AptModel> allAptModelsForCheck = null;

        private static XmlRootAttribute xmlRootAttribute = new XmlRootAttribute("Waypoints");
        public static XmlSerializer WaypointSerializer = new XmlSerializer(typeof(Waypoint[]), xmlRootAttribute);

        public static Waypoint[] waypoints = new Waypoint[] { };

        public static string currentAiracDate;
        public static string nextAiracDate;

        public static string outputDirectory = null;
        public static string outputDirBase;

        public static bool Convert = false;

        public static readonly string tempPath = $"{Path.GetTempPath()}FE-BUDDY";

        public static List<MetaAirportModel> AllMetaFileAirports = new List<MetaAirportModel>();

        public static string facilityID;

        public static List<string> allArtcc = new List<string>() {
            "FAA","FIM","SBA","ZAB","ZAK","ZAN","ZAP","ZAU","ZBW","ZDC","ZDV","ZEG","ZFW","ZHN",
            "ZHU","ZID","ZJX","ZKC","ZLA","ZLC","ZMA","ZME","ZMP","ZNY","ZOA","ZOB","ZSE","ZSU",
            "ZTL","ZUA","ZUL","ZVR","ZWG","ZYZ"
        };
        private static bool hasCurl;

        public static bool GetMetaUrlResponse()
        {
            string nextUrl = $"https://aeronav.faa.gov/d-tpp/{AiracDateCycleModel.AllCycleDates[nextAiracDate]}/xml_data/d-tpp_Metafile.xml";
            //string testUrl = $"https://aeronav.faa.gov/d-tpp/2201/xml_data/d-tpp_Metafile.xml";

            HttpStatusCode result = default(HttpStatusCode);

            try
            {
                var request = HttpWebRequest.Create(nextUrl);
                request.Method = "HEAD";
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response != null)
                    {
                        result = response.StatusCode;
                        response.Close();
                    }
                }

                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }

        public static void CreateAwyGeomapHeadersAndEnding(bool CreateStart)
        {
            if (CreateStart)
            {
                AwyGeoMap.AppendLine("        <GeoMapObject Description=\"AIRWAYS\" TdmOnly=\"false\">");
                AwyGeoMap.AppendLine("          <LineDefaults Bcg=\"4\" Filters=\"4\" Style=\"ShortDashed\" Thickness=\"1\" />");
                AwyGeoMap.AppendLine("          <Elements>");
            }
            else
            {
                AwyGeoMap.AppendLine("          </Elements>");
                AwyGeoMap.AppendLine("        </GeoMapObject>");
                File.WriteAllText($"{outputDirectory}\\VERAM\\{AwyGeoMapFileName}", AwyGeoMap.ToString());
            }
        }

        public static void DownloadAssets()
        {
            foreach (AssetsModel asset in AllAssetsToDownload)
            {
                var client = new WebClient();
                client.DownloadFile(asset.DownloadURL, $"{tempPath}\\{asset.Name}");
            }
        }

        public static void CheckTempDir(bool onlyCreateTempFolder)
        {
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
        }

        public static void DownloadAllFiles(string effectiveDate, string airacCycle, bool getMetaFile = true)
        {
            DownloadedFilePaths = new List<string>();
            Dictionary<string, string> allURLs;
            if (getMetaFile)
            {
                allURLs = new Dictionary<string, string>()
                {
                    { $"{effectiveDate}_STARDP.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/STARDP.zip" },
                    { $"{effectiveDate}_APT.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/APT.zip" },
                    //{ $"{effectiveDate}_WXSTATIONS.txt", $"https://www.aviationweather.gov/docs/metar/stations.txt" },
                    { $"{effectiveDate}_ARB.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/ARB.zip" },
                    { $"{effectiveDate}_ATS.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/ATS.zip" },
                    { $"{effectiveDate}_AWY.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/AWY.zip"},
                    { $"{airacCycle}_FAA_Meta.xml", $"https://aeronav.faa.gov/d-tpp/{airacCycle}/xml_data/d-tpp_Metafile.xml"},
                    { $"{effectiveDate}_FIX.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/FIX.zip" },
                    { $"{effectiveDate}_NAV.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/NAV.zip"},
                    { $"{airacCycle}_TELEPHONY.html", $"https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_2.html" },
                    { $"{effectiveDate}_NWS-WX-STATIONS.xml", $"https://w1.weather.gov/xml/current_obs/index.xml" }
                    //{ $"{effectiveDate}_WX-VATSIM.txt", $"http://metar.vatsim.net/metar.php?id=all" }
                };
            }
            else
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

                File.WriteAllText($"{outputDirectory}\\WARN-README.txt", warningMSG);

                allURLs = new Dictionary<string, string>()
                {
                    { $"{effectiveDate}_STARDP.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/STARDP.zip" },
                    { $"{effectiveDate}_APT.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/APT.zip" },
                    //{ $"{effectiveDate}_WXSTATIONS.txt", $"https://www.aviationweather.gov/docs/metar/stations.txt" },
                    { $"{effectiveDate}_ARB.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/ARB.zip" },
                    { $"{effectiveDate}_ATS.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/ATS.zip" },
                    { $"{effectiveDate}_AWY.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/AWY.zip"},
                    { $"{effectiveDate}_FIX.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/FIX.zip" },
                    { $"{effectiveDate}_NAV.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/NAV.zip"},
                    { $"{airacCycle}_TELEPHONY.html", $"https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_2.html" },
                    { $"{effectiveDate}_NWS-WX-STATIONS.xml", $"https://w1.weather.gov/xml/current_obs/index.xml" }
                    //{ $"{effectiveDate}_WX-VATSIM.txt", $"http://metar.vatsim.net/metar.php?id=all" }
                };
            }



            // Web Client used to connect to the FAA website.
            using (var client = new WebClient())
            {
                foreach (string fileName in allURLs.Keys)
                {
                    try
                    {
                        if (hasCurl)
                        {
                            if (fileName == $"{effectiveDate}_NWS-WX-STATIONS.xml")
                            {
                                CreateCurlBatchFile("NWS-WX-STATIONS.bat", "https://w1.weather.gov/xml/current_obs/index.xml", fileName);
                                ExecuteCurlBatchFile("NWS-WX-STATIONS.bat");
                            }
                            else if (fileName == $"{airacCycle}_TELEPHONY.html")
                            {
                                CreateCurlBatchFile("TELEPHONY.bat", "https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_2.html", fileName);
                                ExecuteCurlBatchFile("TELEPHONY.bat");
                            }
                            else if (fileName == $"{airacCycle}_FAA_Meta.xml")
                            {
                                CreateCurlBatchFile("FAA_Meta.bat", $"https://aeronav.faa.gov/d-tpp/{airacCycle}/xml_data/d-tpp_Metafile.xml", fileName);
                                ExecuteCurlBatchFile("FAA_Meta.bat");
                            }
                            else
                            {
                                client.DownloadFile(allURLs[fileName], $"{tempPath}\\{fileName}");
                            }
                        }
                        else
                        {
                            client.DownloadFile(allURLs[fileName], $"{tempPath}\\{fileName}");
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show($"FAILED DOWNLOADING: \n\n{fileName}\n{allURLs[fileName]}\n\nThis program will exit.\nPlease try again.");
                        Environment.Exit(-1);
                    }
                    DownloadedFilePaths.Add($"{tempPath}\\{fileName}");
                }
            }
        }

        public static void UnzipAllDownloaded()
        {
            foreach (string filePath in DownloadedFilePaths)
            {
                if (filePath.Contains(".zip"))
                {
                    ZipFile.ExtractToDirectory(filePath, filePath.Replace(".zip", string.Empty));
                }
            }
        }

        public static void CheckTempDir()
        {
            if (Directory.Exists(tempPath) && !updateProgram)
            {
                DirectoryInfo di = new DirectoryInfo(tempPath);

                foreach (FileInfo file in di.EnumerateFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                Directory.CreateDirectory(tempPath);
            }
        }

        public static void UpdateCheck()
        {
            string owner = "Nikolai558";
            string repo = "FE-BUDDY";

            string latestReleaseURL = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(latestReleaseURL);
            request.Accept = "application/vnd.github.v3+json";
            request.UserAgent = "request";
            request.AllowAutoRedirect = true;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            List<JObject> listOfAssests = new List<JObject>();

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string content = reader.ReadToEnd();

                var jsonobj = JObject.Parse(content);

                GithubVersion = jsonobj["tag_name"].ToString();
                ReleaseBody = jsonobj["body"].ToString();

                foreach (JObject asset in jsonobj["assets"])
                {
                    listOfAssests.Add(asset);
                }
            }

            foreach (JObject asset in listOfAssests)
            {
                AssetsModel downloadAsset = new AssetsModel() { Name = asset["name"].ToString(), DownloadURL = asset["browser_download_url"].ToString() };
                AllAssetsToDownload.Add(downloadAsset);
            }
        }

        /// <summary>
        /// Convert the Lat AND Lon from the FAA Data into a standard [N-S-E-W]000.00.00.000 format.
        /// </summary>
        /// <param name="value">Needs to have 3 decimal points, and one of the following Letters [N-S-E-W]</param>
        /// <param name="Lat">Is this value a Lat, if so Put true</param>
        /// <param name="ConvertEast">Do you need ALL East Coords converted, if so put true.</param>
        /// <returns>standard [N-S-E-W]000.00.00.000 lat/lon format</returns>
        public static string CorrectLatLon(string value, bool Lat, bool ConvertEast)
        {
            // Valid format is N000.00.00.000 W000.00.000
            string correctedValue = "";

            // Split the value based on these char
            char[] splitValue = new char[] { '.', '-' };

            // Declare our variables.
            string degrees;
            string minutes;
            string seconds;
            string milSeconds;
            string declination = "";

            // If the value is a Lat
            if (Lat)
            {
                // Find the Character "N"
                if (value.IndexOf("N", 0, value.Length) != -1)
                {
                    // Set Declination to N
                    declination = "N";

                    // Delete the N from the Value
                    value = value.Replace("N", "");
                }

                // Find the Char "S"
                else if (value.IndexOf("S", 0, value.Length) != -1)
                {
                    // Set declination to S
                    declination = "S";

                    // Remove the S from our Value
                    value = value.Replace("S", "");
                }

                // Split the Value by our chars we defined above.
                string[] valueSplit = value.Split(splitValue);

                // Set our Variables
                degrees = valueSplit[0];
                minutes = valueSplit[1];
                seconds = valueSplit[2];

                // Check the length of our Milliseconds. 
                if (valueSplit[3].Length > 3)
                {
                    // If its greater then 3 we only want to keep the first three.
                    milSeconds = valueSplit[3].Substring(0, 3);
                }
                else
                {
                    // our the length is less than or equal to three so just set it. 
                    milSeconds = valueSplit[3];
                }

                // Correct Format is now set.
                correctedValue = $"{declination}{degrees.PadLeft(3, '0')}.{minutes.PadRight(2, '0')}.{seconds.PadRight(2, '0')}.{milSeconds.PadRight(3, '0')}";
            }

            // Value is a Lon
            else
            {
                // Check for "E" Char
                if (value.IndexOf("E", 0, value.Length) != -1)
                {
                    // Set Declination to E
                    declination = "E";

                    //Remove the E from our Value
                    value = value.Replace("E", "");
                }

                // Check for "W" char
                else if (value.IndexOf("W", 0, value.Length) != -1)
                {
                    // Set Declination to W
                    declination = "W";

                    // Remove the W from our Value.
                    value = value.Replace("W", "");
                }

                // Value does not have an E or W. 
                else
                {
                    // There has been an error in the value passed in.
                    return value;
                }

                // Split the value by our char we defined above.
                string[] valueSplit = value.Split(splitValue);

                // Set all of our variables.
                degrees = valueSplit[0];
                minutes = valueSplit[1];
                seconds = valueSplit[2];

                if (valueSplit[3].Length > 3)
                {
                    // If its greater then 3 we only want to keep the first three.
                    milSeconds = valueSplit[3].Substring(0, 3);
                }
                else
                {
                    // our the length is less than or equal to three so just set it. 
                    milSeconds = valueSplit[3];
                }


                // Set the Corrected Value
                correctedValue = $"{declination}{degrees.PadLeft(3, '0')}.{minutes.PadRight(2, '0')}.{seconds.PadRight(2, '0')}.{milSeconds.PadRight(3, '0')}";
            }

            // Check to see if Convert E is True and Check our Value's Declination to make sure it is an E Coord.
            if (ConvertEast && declination == "E")
            {
                double oldDecForm = double.Parse(CreateDecFormat(correctedValue, false));

                double newDecForm = 180 - oldDecForm;

                newDecForm = (newDecForm + 180) * -1;

                correctedValue = createDMS(newDecForm, false);

                // Return the corrected value. 
                return correctedValue;
            }

            // No Conversion needed
            else
            {
                // Return the corrected Value
                return correctedValue;
            }

        }

        public static string createDMS(double value, bool lat)
        {

            int degrees;
            decimal degreeFloat = 0;

            int minutes;
            decimal minuteFloat = 0;

            int seconds;
            decimal secondFloat = 0;

            string miliseconds = "0";

            string dms;

            degrees = int.Parse(value.ToString().Split('.')[0]);

            if (value.ToString().Split('.').Count() > 1)
            {
                degreeFloat = decimal.Parse("0." + value.ToString().Split('.')[1]);
            }

            minutes = int.Parse((degreeFloat * 60).ToString().Split('.')[0]);

            if ((degreeFloat * 60).ToString().Split('.').Count() > 1)
            {
                minuteFloat = decimal.Parse("0." + (degreeFloat * 60).ToString().Split('.')[1]);
            }

            seconds = int.Parse((minuteFloat * 60).ToString().Split('.')[0]);

            if ((minuteFloat * 60).ToString().Split('.').Count() > 1)
            {
                secondFloat = decimal.Parse("0." + (minuteFloat * 60).ToString().Split('.')[1]);
            }

            secondFloat = Math.Round(secondFloat, 3);

            if (secondFloat.ToString().Split('.').Count() > 1)
            {
                miliseconds = secondFloat.ToString().Split('.')[1];
            }


            if (lat)
            {
                if (degrees < 0)
                {
                    degrees = degrees * -1;
                    dms = $"S{degrees.ToString().PadLeft(3, '0')}.{minutes.ToString().PadLeft(2, '0')}.{seconds.ToString().PadLeft(2, '0')}.{miliseconds.ToString().PadLeft(3, '0')}";
                }
                else
                {
                    dms = $"N{degrees.ToString().PadLeft(3, '0')}.{minutes.ToString().PadLeft(2, '0')}.{seconds.ToString().PadLeft(2, '0')}.{miliseconds.ToString().PadLeft(3, '0')}";
                }
            }
            else
            {
                if (degrees < 0)
                {
                    degrees = degrees * -1;
                    dms = $"W{degrees.ToString().PadLeft(3, '0')}.{minutes.ToString().PadLeft(2, '0')}.{seconds.ToString().PadLeft(2, '0')}.{miliseconds.ToString().PadLeft(3, '0')}";
                }
                else
                {
                    dms = $"E{degrees.ToString().PadLeft(3, '0')}.{minutes.ToString().PadLeft(2, '0')}.{seconds.ToString().PadLeft(2, '0')}.{miliseconds.ToString().PadLeft(3, '0')}";
                }
            }

            return dms;
        }

        /// <summary>
        /// Do some math to convert lat/lon to decimal format
        /// </summary>
        /// <param name="value">lat OR Lon</param>
        /// <returns>Decimal Format of the value past in.</returns>
        public static string CreateDecFormat(string value, bool roundSixPlaces)
        {
            // Split the value at decimal points.
            string[] splitValue = value.Split('.');

            // set our values
            string declination = splitValue[0].Substring(0, 1);
            string degrees = splitValue[0].Substring(1, 3);
            string minutes = splitValue[1];
            string seconds = splitValue[2];
            string miliSeconds = splitValue[3];
            string decFormatSeconds = $"{seconds}.{miliSeconds}";

            // Do some math with all of our variables.
            string decFormat = (double.Parse(degrees) + (double.Parse(minutes) / 60) + (double.Parse(decFormatSeconds) / 3600)).ToString();

            // Check the Declination
            if (declination == "S" || declination == "W")
            {
                // if it is S or W it needs to be a negative number.
                decFormat = $"-{decFormat}";
            }

            if (roundSixPlaces)
            {
                // Round the Decimal format to 6 places after the decimal.
                decFormat = Math.Round(double.Parse(decFormat), 6).ToString();
            }

            // Return the Decimal Format.
            return decFormat;
        }

        private static void CreateCurlBatchFile(string name, string url, string outputFileName)
        {
            string filePath = $"{tempPath}\\{name}";
            string writeMe = $"cd /d \"{tempPath}\"\n" +
                $"curl \"{url}\" > {outputFileName}";
            File.WriteAllText(filePath, writeMe);
        }

        private static void ExecuteCurlBatchFile(string batchFileName)
        {
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + $"\"{tempPath}\\{batchFileName}\"");
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;

            Process = Process.Start(ProcessInfo);
            Process.WaitForExit();

            Process.Close();
        }

        /// <summary>
        /// Get the Airac Effective Dates and Set our Global Variables to it.
        /// </summary>
        public static void GetAiracDateFromFAA()
        {
            // FAA URL that contains the effective dates for both Current and Next AIRAC Cycles.
            string url = "https://www.faa.gov/air_traffic/flight_info/aeronav/aero_data/NASR_Subscription/";

            string response;

            CreateCurlBatchFile("getAiraccEff.bat", "https://www.faa.gov/air_traffic/flight_info/aeronav/aero_data/NASR_Subscription/", $"{FaaHtmlFileVariable}_FAA_NASR.HTML");

            ExecuteCurlBatchFile("getAiraccEff.bat");

            if (File.Exists($"{tempPath}\\{FaaHtmlFileVariable}_FAA_NASR.HTML") && File.ReadAllText($"{tempPath}\\{FaaHtmlFileVariable}_FAA_NASR.HTML").Length > 10)
            {
                response = File.ReadAllText($"{tempPath}\\{FaaHtmlFileVariable}_FAA_NASR.HTML");
                hasCurl = true;
            }
            else
            {
                // If we get here the user does not have Curl, OR Curl returned a file that is not longer than 10 Characters.
                hasCurl = false;
                using (var client = new System.Net.WebClient())
                {
                    client.Proxy = null;

                    //client.Proxy = GlobalProxySelection.GetEmptyWebProxy();
                    response = client.DownloadString(url);
                }
            }

            response.Trim();

            // Find the two strings that contain the effective date and set our Global Variables.
            nextAiracDate = response.Substring(response.IndexOf("NASR_Subscription_") + 18, 10);
            currentAiracDate = response.Substring(response.LastIndexOf("NASR_Subscription_") + 18, 10);
        }

        /// <summary>
        /// Create our Output directories inside the directory the user chose.
        /// </summary>
        public static void CreateDirectories()
        {
            Directory.CreateDirectory(outputDirectory);
            Directory.CreateDirectory($"{outputDirectory}\\ALIAS");
            Directory.CreateDirectory($"{outputDirectory}\\VRC");
            Directory.CreateDirectory($"{outputDirectory}\\VSTARS");
            Directory.CreateDirectory($"{outputDirectory}\\VERAM");
            Directory.CreateDirectory($"{outputDirectory}\\VRC\\[SID]");
            Directory.CreateDirectory($"{outputDirectory}\\VRC\\[STAR]");
        }

        /// <summary>
        /// Write the Waypoints.xml File. NOTE: The Global Variable that contains all the waypoints has to be 
        /// completely filled in before we call this function!
        /// </summary>
        public static void WriteWaypointsXML()
        {
            // File path for the waypoints.xml that we want to store to.
            string filePath = $"{outputDirectory}\\VERAM\\Waypoints.xml";

            // Write the XML Serializer to the file.
            TextWriter writer = new StreamWriter(filePath);
            WaypointSerializer.Serialize(writer, waypoints);
            writer.Close();
        }

        public static void WriteNavXmlOutput()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("        <GeoMapObject Description=\"NAVAIDS\" TdmOnly=\"false\">");
            sb.AppendLine("          <SymbolDefaults Bcg=\"13\" Filters=\"13\" Style=\"VOR\" Size=\"1\" />");
            sb.AppendLine("          <Elements>");

            string readFilePath = $"{outputDirectory}\\VERAM\\Waypoints.xml";
            string saveFilePath = $"{ outputDirectory}\\VERAM\\NAVAID_GEOMAP.xml";

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
            string filepath = $"{outputDirectory}\\VERAM\\Waypoints.xml";

            // Add the Airac effective date in comment form to the end of the file.
            File.AppendAllText(filepath, $"\n<!--AIRAC_EFFECTIVE_DATE {AiracDate}-->");

            // Copy the file from VERAM to VSTARS. - They use the same format. 
            File.Copy($"{outputDirectory}\\VERAM\\Waypoints.xml", $"{outputDirectory}\\VSTARS\\Waypoints.xml");
        }

        /// <summary>
        /// Write a test sector file for VRC.
        /// </summary>
        public static void WriteTestSctFile()
        {
            // Write the file INFO section.
            File.WriteAllText($"{outputDirectory}\\{testSectorFileName}", $"[INFO]\nTEST_SECTOR\nTST_CTR\nXXXX\nN043.31.08.418\nW112.03.50.103\n60.043\n43.536\n-11.8\n1.000\n\n\n\n\n");
        }
    }
}
