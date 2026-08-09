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
                if (GlobalConfig.DEVMODE)
                {
                    return;
                }

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

        public static void DecompressGz(string sourceGzFile, string destinationFile)
        {
            // Open the compressed .gz file for reading
            using FileStream compressedStream = new FileStream(sourceGzFile, FileMode.Open, FileAccess.Read);

            // Create the destination file where uncompressed data will be saved
            using FileStream targetStream = File.Create(destinationFile);

            // Wrap the compressed stream in a GZipStream configured to Decompress
            using GZipStream decompressor = new GZipStream(compressedStream, CompressionMode.Decompress);

            // Copy the decompressed data directly to the target file
            decompressor.CopyTo(targetStream);
        }

        public static void UnzipAllDownloaded()
        {
            foreach (string filePath in GlobalConfig.DownloadedFilePaths)
            {
                if (Directory.Exists(filePath.Replace(".zip", string.Empty)) && GlobalConfig.DEVMODE)
                {
                    continue;
                }

                if (filePath.Contains(".zip"))
                {
                    Logger.LogMessage("INFO", $"UNZIPING: {filePath}");
                    ZipFile.ExtractToDirectory(filePath, filePath.Replace(".zip", string.Empty));
                }

                /// We already decompress the GZ file of the weather stations inside of the GetAptData.cs Line 222. Uncommenting this would do every .GZ file we download right up front. 
                ///else if (filePath.Contains(".gz"))
                ///{
                ///    DecompressGz(filePath, filePath.Replace(".gz", string.Empty));
                ///}

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
