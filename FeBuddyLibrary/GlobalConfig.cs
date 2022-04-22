using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using FeBuddyLibrary.Models;
using FeBuddyLibrary.Models.MetaFileModels;

namespace FeBuddyLibrary
{
    public class GlobalConfig
    {
        public static string airacEffectiveDate = "";

        public static string FaaHtmlFileVariable { get; protected set; } = DateTime.Now.ToString("MMddHHmmss");

        public static List<string> DownloadedFilePaths = new List<string>();

        public static string testSectorFileName { get; } = $"\\VRC\\TestSectorFile.sct2";

        public static StringBuilder AwyGeoMap { get; } = new StringBuilder();
        public static string AwyGeoMapFileName { get; } = "AWY_GEOMAP.xml";

        public static List<AptModel> allAptModelsForCheck = null;
        public static XmlSerializer WaypointSerializer { get; } = new XmlSerializer(typeof(Waypoint[]), new XmlRootAttribute("Waypoints"));

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

        public static bool hasCurl;
    }
}
