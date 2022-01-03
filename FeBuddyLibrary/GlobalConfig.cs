using FeBuddyLibrary.Models;
using FeBuddyLibrary.Models.MetaFileModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace FeBuddyLibrary
{
    public class GlobalConfig
    {
        public static readonly string ProgramVersion = "1.0.3";

        public static string GithubVersion = "";

        public static string airacEffectiveDate = "";

        public static string FaaHtmlFileVariable { get; protected set; } = DateTime.Now.ToString("MMddHHmmss");

        public static List<string> DownloadedFilePaths = new List<string>();

        public static readonly string testSectorFileName = $"\\VRC\\TestSectorFile.sct2";

        public static bool updateProgram = false;

        public static List<AssetsModel> AllAssetsToDownload = new List<AssetsModel>();

        public static string ReleaseBody = "";

        public static StringBuilder AwyGeoMap = new StringBuilder();
        public static string AwyGeoMapFileName = "AWY_GEOMAP.xml";

        public static List<AptModel> allAptModelsForCheck = null;

        private static readonly XmlRootAttribute xmlRootAttribute = new XmlRootAttribute("Waypoints");
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

        public static bool hasCurl;
    }
}
