using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using FeBuddyLibrary.Models;

namespace FeBuddyLibrary.Helpers
{
    public class DownloadHelpers
    {
        /// <param name="onBytes">
        /// Optional callback invoked as the body streams in: (bytesReceivedSoFar, totalBytesOrNull).
        /// Called once with 0 up front so callers can show the file starting even before the
        /// first chunk arrives. When null, the fast bulk copy is used instead.
        /// </param>
        private static void DownloadFile(string url, string destPath, Action<long, long?> onBytes = null)
        {
            using var response = SharedHttp.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                                                  .GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            using var src = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            using var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);

            if (onBytes == null)
            {
                src.CopyTo(dst);
                return;
            }

            long? totalBytes = response.Content.Headers.ContentLength;
            byte[] buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            onBytes(0, totalBytes);
            while ((bytesRead = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                dst.Write(buffer, 0, bytesRead);
                totalRead += bytesRead;
                onBytes(totalRead, totalBytes);
            }
        }

        /// <param name="progress">
        /// Optional per-file download progress for the AIRAC "Downloading FAA Data" step.
        /// Files fetched via curl (telephony/meta/NWS when curl is present) and cached files
        /// in DEV mode report as a single step rather than byte-by-byte.
        /// </param>
        public static void DownloadAllFiles(string effectiveDate, string airacCycle, bool getMetaFile = true, IProgress<AiracDownloadProgress> progress = null)
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

            int fileCount = allURLs.Count;
            int fileIndex = 0;

            foreach (string fileName in allURLs.Keys)
                {
                    fileIndex++;

                    // "2025-08-07_APT.zip" -> "APT.zip"  /  "2508_FAA_Meta.xml" -> "FAA_Meta.xml"
                    int splitAt = fileName.IndexOf('_');
                    string friendlyName = splitAt >= 0 ? fileName.Substring(splitAt + 1) : fileName;

                    int lastOverall = -1;
                    void ReportBytes(long received, long? total)
                    {
                        if (progress == null)
                        {
                            return;
                        }

                        var info = new AiracDownloadProgress
                        {
                            FileName = friendlyName,
                            FileIndex = fileIndex,
                            FileCount = fileCount,
                            BytesReceived = received,
                            TotalBytes = total
                        };

                        // Always let the leading (received == 0) call through so the file name
                        // updates immediately; otherwise only forward when the overall percent
                        // actually moved, to keep the UI message pump from being flooded.
                        if (received != 0 && info.OverallPercent == lastOverall)
                        {
                            return;
                        }

                        lastOverall = info.OverallPercent;
                        progress.Report(info);
                    }

                    if (File.Exists($"{GlobalConfig.tempPath}\\{fileName}") && GlobalConfig.DEVMODE)
                    {
                        ReportBytes(0, null);
                        ReportBytes(1, 1);
                        GlobalConfig.DownloadedFilePaths.Add($"{GlobalConfig.tempPath}\\{fileName}");
                        continue;
                    }

                    try
                    {
                        Logger.LogMessage("INFO", $"ATTEMPTING TO DOWNLOAD: {fileName}");
                        ReportBytes(0, null);

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
                                DownloadFile(allURLs[fileName], $"{GlobalConfig.tempPath}\\{fileName}", ReportBytes);
                            }
                        }
                        else
                        {
                            DownloadFile(allURLs[fileName], $"{GlobalConfig.tempPath}\\{fileName}", ReportBytes);
                        }
                        Logger.LogMessage("INFO", $"DOWNLOAD SUCCESSFUL: {fileName}");
                        ReportBytes(1, 1);

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
