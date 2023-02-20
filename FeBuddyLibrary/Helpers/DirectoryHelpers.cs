using System.IO;
using System.IO.Compression;

namespace FeBuddyLibrary.Helpers
{
    public static class DirectoryHelpers
    {
        public static void CheckTempDir()
        {
            Logger.LogMessage("DEBUG", "CHECKING TEMP DIRECTORY");

            if (Directory.Exists(GlobalConfig.tempPath))
            {
                // GEOJSONTODO Remove this from code once done Testing CRC Airac Data Creation Stuff.
                return;

                DirectoryInfo di = new DirectoryInfo(GlobalConfig.tempPath);

                foreach (FileInfo file in di.EnumerateFiles())
                {
                    file.Delete();
                    Logger.LogMessage("DEBUG", $"DELETED FILE: {file.FullName}");
                }

                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    dir.Delete(true);
                    Logger.LogMessage("DEBUG", $"DELETED DIRECTORY: {dir.FullName}");
                }
            }
            else
            {
                Directory.CreateDirectory(GlobalConfig.tempPath);
                Logger.LogMessage("DEBUG", $"CREATED DIRECTORY: {GlobalConfig.tempPath}");
            }
        }

        public static void UnzipAllDownloaded()
        {
            foreach (string filePath in GlobalConfig.DownloadedFilePaths)
            {
                if (Directory.Exists(filePath.Replace(".zip", string.Empty)))
                {
                    // GEOJSONTODO - Remove this code once dev for crc airac stuff is done. 
                    continue;
                }

                if (filePath.Contains(".zip"))
                {
                    Logger.LogMessage("INFO", $"UNZIPING: {filePath}");
                    ZipFile.ExtractToDirectory(filePath, filePath.Replace(".zip", string.Empty));
                }
            }
        }

        /// <summary>
        /// Create our Output directories inside the directory the user chose.
        /// </summary>
        public static void CreateDirectories()
        {
            Logger.LogMessage("DEBUG", "CREATING OUTPUT DIRECTORIES");
            Directory.CreateDirectory(GlobalConfig.outputDirectory);
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\ALIAS");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\CRC");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\CRC\\STARs");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\CRC\\DPs");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VRC");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VSTARS");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VERAM");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VRC\\[SID]");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VRC\\[STAR]");
        }
    }
}
