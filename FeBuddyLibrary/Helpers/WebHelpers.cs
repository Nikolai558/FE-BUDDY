﻿using FeBuddyLibrary.Models;
using FeBuddyLibrary.Models.MetaFileModels;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace FeBuddyLibrary.Helpers
{
    public class WebHelpers
    {
        public static bool GetMetaUrlResponse()
        {
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

                return true;
            }
            catch (WebException)
            {
                return false;
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

                GlobalConfig.GithubVersion = jsonobj["tag_name"].ToString();
                GlobalConfig.ReleaseBody = jsonobj["body"].ToString();

                foreach (JObject asset in jsonobj["assets"])
                {
                    listOfAssests.Add(asset);
                }
            }

            foreach (JObject asset in listOfAssests)
            {
                AssetsModel downloadAsset = new AssetsModel() { Name = asset["name"].ToString(), DownloadURL = asset["browser_download_url"].ToString() };
                GlobalConfig.AllAssetsToDownload.Add(downloadAsset);
            }
        }

        /// <summary>
        /// Get the Airac Effective Dates and Set our Global Variables to it.
        /// </summary>
        public static void GetAiracDateFromFAA()
        {
            // FAA URL that contains the effective dates for both Current and Next AIRAC Cycles.
            string url = "https://www.faa.gov/air_traffic/flight_info/aeronav/aero_data/NASR_Subscription/";

            string response;

            BatchFileHelpers.CreateCurlBatchFile("getAiraccEff.bat", "https://www.faa.gov/air_traffic/flight_info/aeronav/aero_data/NASR_Subscription/", $"{GlobalConfig.FaaHtmlFileVariable}_FAA_NASR.HTML");

            BatchFileHelpers.ExecuteCurlBatchFile("getAiraccEff.bat");

            if (File.Exists($"{GlobalConfig.tempPath}\\{GlobalConfig.FaaHtmlFileVariable}_FAA_NASR.HTML") && File.ReadAllText($"{GlobalConfig.tempPath}\\{GlobalConfig.FaaHtmlFileVariable}_FAA_NASR.HTML").Length > 10)
            {
                response = File.ReadAllText($"{GlobalConfig.tempPath}\\{GlobalConfig.FaaHtmlFileVariable}_FAA_NASR.HTML");
                GlobalConfig.hasCurl = true;
            }
            else
            {
                GlobalConfig.hasCurl = false;
                using (var client = new System.Net.WebClient())
                {
                    client.Proxy = null;

                    //client.Proxy = GlobalProxySelection.GetEmptyWebProxy();
                    response = client.DownloadString(url);
                }
            }

            response.Trim();

            // Find the two strings that contain the effective date and set our Global Variables.
            GlobalConfig.nextAiracDate = response.Substring(response.IndexOf("NASR_Subscription_") + 18, 10);
            GlobalConfig.currentAiracDate = response.Substring(response.LastIndexOf("NASR_Subscription_") + 18, 10);
        }
    }
}