﻿using System.IO;
using System.Net;
using FeBuddyLibrary.Models.MetaFileModels;

namespace FeBuddyLibrary.Helpers
{
    public class WebHelpers
    {
        public static bool GetMetaUrlResponse()
        {
            Logger.LogMessage("DEBUG", "CHECKING IF NEXT AIRAC HAS THE META FILE OR NOT");

            string nextUrl = $"https://aeronav.faa.gov/d-tpp/{AiracDateCycleModel.AllCycleDates[GlobalConfig.nextAiracDate]}/xml_data/d-tpp_Metafile.xml";
            //string testUrl = $"https://aeronav.faa.gov/d-tpp/2201/xml_data/d-tpp_Metafile.xml";

            HttpStatusCode result;

            try
            {
                var request = HttpWebRequest.Create(nextUrl);
                request.Method = "HEAD";
                if (request.GetResponse() is HttpWebResponse response)
                {
                    if (response != null)
                    {
                        result = response.StatusCode;
                    }

                    response.Close();
                }

                Logger.LogMessage("DEBUG", "NEXT AIRAC IS AVAILABLE");
                return true;
            }
            catch (WebException)
            {
                Logger.LogMessage("DEBUG", "NEXT AIRAC NOT AVAILABLE");

                return false;
            }
        }

        /// <summary>
        /// Get the Airac Effective Dates and Set our Global Variables to it.
        /// </summary>
        public static void GetAiracDateFromFAA()
        {
            Logger.LogMessage("DEBUG", "SEARCHING FAA WEBSITE FOR AIRAC DATES");

            // FAA URL that contains the effective dates for both Current and Next AIRAC Cycles.
            string url = "https://www.faa.gov/air_traffic/flight_info/aeronav/aero_data/NASR_Subscription/";

            string response;

            BatchFileHelpers.CreateCurlBatchFile("getAiraccEff.bat", "https://www.faa.gov/air_traffic/flight_info/aeronav/aero_data/NASR_Subscription/", $"{GlobalConfig.FaaHtmlFileVariable}_FAA_NASR.HTML");

            BatchFileHelpers.ExecuteCurlBatchFile("getAiraccEff.bat");

            if (File.Exists($"{GlobalConfig.tempPath}\\{GlobalConfig.FaaHtmlFileVariable}_FAA_NASR.HTML") && File.ReadAllText($"{GlobalConfig.tempPath}\\{GlobalConfig.FaaHtmlFileVariable}_FAA_NASR.HTML").Length > 10)
            {
                Logger.LogMessage("DEBUG", "GOT RESPONSE FROM FAA WEBSITE");
                response = File.ReadAllText($"{GlobalConfig.tempPath}\\{GlobalConfig.FaaHtmlFileVariable}_FAA_NASR.HTML");

                Logger.LogMessage("DEBUG", "USER HAS CURL");
                GlobalConfig.hasCurl = true;
            }
            else
            {
                Logger.LogMessage("WARNING", "USER DOES NOT HAVE CURL OR IT FAILED");
                GlobalConfig.hasCurl = false;
                using (var client = new System.Net.WebClient())
                {
                    client.Proxy = null;

                    //client.Proxy = GlobalProxySelection.GetEmptyWebProxy();
                    response = client.DownloadString(url);
                    Logger.LogMessage("DEBUG", "GOT AIRAC DATE USING WEBCLIENT");
                }
            }

            response.Trim();

            // Find the two strings that contain the effective date and set our Global Variables.
            GlobalConfig.nextAiracDate = response.Substring(response.IndexOf("./../NASR_Subscription") + 23, 10);
            GlobalConfig.currentAiracDate = response.Substring(response.LastIndexOf("./../NASR_Subscription") + 23, 10);
            Logger.LogMessage("INFO", $"CURRENT AIRAC DATE: {GlobalConfig.currentAiracDate} / NEXT AIRAC DATE: {GlobalConfig.nextAiracDate}");

        }
    }
}
