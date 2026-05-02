using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace FeBuddyLibrary.Helpers
{
    public class DownloadHelpers
    {
        private static void DownloadFile(string url, string destPath)
        {
            using var response = SharedHttp.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                                                  .GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            using var src = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            using var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
            src.CopyTo(dst);
        }

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
                    { $"{effectiveDate}_NWS-WX-STATIONS.xml.gz", $"https://aviationweather.gov/data/cache/stations.cache.xml.gz" },
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
                    { $"{effectiveDate}_NWS-WX-STATIONS.xml.gz", $"https://aviationweather.gov/data/cache/stations.cache.xml.gz" },
                    { $"{effectiveDate}_AWOS.zip", $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate}/AWOS.zip" }
                };
            }

            foreach (string fileName in allURLs.Keys)
            {
                if (File.Exists($"{GlobalConfig.tempPath}\\{fileName}") && GlobalConfig.DEVMODE)
                {
                    GlobalConfig.DownloadedFilePaths.Add($"{GlobalConfig.tempPath}\\{fileName}");
                    continue;
                }

                try
                {
                    Logger.LogMessage("INFO", $"ATTEMPTING TO DOWNLOAD: {fileName}");
                    DownloadFile(allURLs[fileName], $"{GlobalConfig.tempPath}\\{fileName}");
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
