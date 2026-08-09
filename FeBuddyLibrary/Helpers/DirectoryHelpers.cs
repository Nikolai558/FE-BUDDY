using System;
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

        public static void UnzipAllDownloaded()
        {
            foreach (string filePath in GlobalConfig.DownloadedFilePaths)
            {
                // Get the file extension from the current file path.
                // Example:
                //          "C:\Downloads\data.zip"     ->  ".zip"
                //          "C:\Downloads\data.csv.gz"  ->  ".gz"
                string extension = Path.GetExtension(filePath);

                // Check whether the current file is a ZIP archive.
                if (string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // Build the destination folder path for the ZIP contents.
                    //      "*\data.zip"  ->  "*\data"
                    string destinationPath = Path.Combine(
                        Path.GetDirectoryName(filePath)!,
                        Path.GetFileNameWithoutExtension(filePath));

                    // In development mode, skip extraction if the destination
                    // directory already exists.
                    if (Directory.Exists(destinationPath) && GlobalConfig.DEVMODE)
                    {
                        continue;
                    }

                    Logger.LogMessage("INFO", $"UNZIPPING: {filePath}");

                    // Extract all files and folders from the ZIP archive
                    // into the destination directory created.
                    ZipFile.ExtractToDirectory(filePath, destinationPath);
                }

                // If the file is not a ZIP file, check whether it is a GZIP file.
                else if (string.Equals(extension, ".gz", StringComparison.OrdinalIgnoreCase))
                {
                    // Build the output filename for the decompressed GZIP file.
                    //      "*\data.csv.gz"  ->  "*\data.csv"
                    string destinationFilePath = Path.Combine(
                        Path.GetDirectoryName(filePath)!,
                        Path.GetFileNameWithoutExtension(filePath));

                    // In development mode, skip decompression if the
                    // destination file already exists.
                    if (File.Exists(destinationFilePath) && GlobalConfig.DEVMODE)
                    {
                        continue;
                    }

                    Logger.LogMessage("INFO", $"DECOMPRESSING GZ: {filePath}");

                    // Open the .gz file for reading.
                    using FileStream compressedFileStream = File.OpenRead(filePath);

                    // Create a GZipStream that reads from the compressed file
                    // and decompresses its contents.
                    using GZipStream decompressionStream =
                        new GZipStream(
                            compressedFileStream,
                            CompressionMode.Decompress);

                    // Create the destination file that will receive the
                    // decompressed contents.
                    using FileStream outputFileStream =
                        File.Create(destinationFilePath);

                    // Read the decompressed bytes from the GZipStream
                    // and write them into the destination file.
                    decompressionStream.CopyTo(outputFileStream);
                }

                // TODO: Handle other file types if necessary, and/or log a message for unsupported formats.
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
