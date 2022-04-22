using System;
using System.Collections.Generic;
using System.Net;

namespace FeBuddyLibrary.Helpers
{
    public class DownloadHelpers
    {
        public static void DownloadAllFiles(string effectiveDate, string airacCycle, bool getMetaFile = true)
        {
            Logger.LogMessage("DEBUG", "DOWNLOADING ALL FILES REQUIRED");

            GlobalConfig.DownloadedFilePaths = new List<string>();
            Dictionary<string, string> allURLs;
            // TODO - This should be a static readonly dictionary, then grab only what we need in terms of meta info or not
            if (getMetaFile)
            {
                Logger.LogMessage("DEBUG", "INCLUDING META FILES");

                allURLs = new Dictionary<string, string>()
                {
                    { $"{effectiveDate}_STARDP.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/STARDP.zip" },
                    { $"{effectiveDate}_APT.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/APT.zip" },
                    { $"{effectiveDate}_ARB.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/ARB.zip" },
                    { $"{effectiveDate}_ATS.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/ATS.zip" },
                    { $"{effectiveDate}_AWY.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/AWY.zip"},
                    { $"{airacCycle}_FAA_Meta.xml", $"https://aeronav.faa.gov/d-tpp/{airacCycle}/xml_data/d-tpp_Metafile.xml"},
                    { $"{effectiveDate}_FIX.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/FIX.zip" },
                    { $"{effectiveDate}_NAV.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/NAV.zip"},
                    { $"{airacCycle}_TELEPHONY.html", $"https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_2.html" },
                    { $"{effectiveDate}_NWS-WX-STATIONS.xml", $"https://w1.weather.gov/xml/current_obs/index.xml" },
                    { $"{effectiveDate}_AWOS.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/AWOS.zip" }
                };
            }
            else
            {
                Logger.LogMessage("DEBUG", "EXCLUDING META FILES");

                FileHelpers.WriteWarnMeFile();
                allURLs = new Dictionary<string, string>()
                {
                    { $"{effectiveDate}_STARDP.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/STARDP.zip" },
                    { $"{effectiveDate}_APT.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/APT.zip" },
                    { $"{effectiveDate}_ARB.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/ARB.zip" },
                    { $"{effectiveDate}_ATS.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/ATS.zip" },
                    { $"{effectiveDate}_AWY.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/AWY.zip"},
                    { $"{effectiveDate}_FIX.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/FIX.zip" },
                    { $"{effectiveDate}_NAV.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/NAV.zip"},
                    { $"{airacCycle}_TELEPHONY.html", $"https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_2.html" },
                    { $"{effectiveDate}_NWS-WX-STATIONS.xml", $"https://w1.weather.gov/xml/current_obs/index.xml" },
                    { $"{effectiveDate}_AWOS.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/AWOS.zip" }
                };
            }

            // Web Client used to connect to the FAA website.
            using (var client = new WebClient())
            {
                foreach (string fileName in allURLs.Keys)
                {
                    try
                    {
                        Logger.LogMessage("INFO", $"ATTEMPTING TO DOWNLOAD: {fileName}");

                        if (GlobalConfig.hasCurl)
                        {
                            if (fileName == $"{effectiveDate}_NWS-WX-STATIONS.xml")
                            {
                                BatchFileHelpers.CreateCurlBatchFile("NWS-WX-STATIONS.bat", "https://w1.weather.gov/xml/current_obs/index.xml", fileName);
                                BatchFileHelpers.ExecuteCurlBatchFile("NWS-WX-STATIONS.bat");
                            }
                            else if (fileName == $"{airacCycle}_TELEPHONY.html")
                            {
                                BatchFileHelpers.CreateCurlBatchFile("TELEPHONY.bat", "https://www.faa.gov/air_traffic/publications/atpubs/cnt_html/chap3_section_2.html", fileName);
                                BatchFileHelpers.ExecuteCurlBatchFile("TELEPHONY.bat");
                            }
                            else if (fileName == $"{airacCycle}_FAA_Meta.xml")
                            {
                                BatchFileHelpers.CreateCurlBatchFile("FAA_Meta.bat", $"https://aeronav.faa.gov/d-tpp/{airacCycle}/xml_data/d-tpp_Metafile.xml", fileName);
                                BatchFileHelpers.ExecuteCurlBatchFile("FAA_Meta.bat");
                            }
                            else
                            {
                                client.DownloadFile(allURLs[fileName], $"{GlobalConfig.tempPath}\\{fileName}");
                            }
                        }
                        else
                        {
                            client.DownloadFile(allURLs[fileName], $"{GlobalConfig.tempPath}\\{fileName}");
                        }
                        Logger.LogMessage("INFO", $"DOWNLOAD SUCCESSFUL: {fileName}");

                    }
                    catch (Exception)
                    {
                        Logger.LogMessage("ERROR", $"DOWNLOAD FAILED: {fileName}");

                        MessageBoxHelpers.FileDownloadErrorMB(fileName, allURLs);
                        Logger.LogMessage("ERROR", $"PROGRAM CLOSING: {fileName}");

                        Environment.Exit(-1);
                    }
                    GlobalConfig.DownloadedFilePaths.Add($"{GlobalConfig.tempPath}\\{fileName}");
                }
            }
        }
    }
}
