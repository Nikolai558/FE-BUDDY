using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyLibrary.Helpers
{
    public static class DirectoryHelpers
    {
        private static string TempPath { get; } = GlobalConfig.tempPath;
        private static string OutputPath { get; } = GlobalConfig.outputDirectory;


        public static void CheckTempDir()
        {
            if (Directory.Exists(TempPath) && !GlobalConfig.updateProgram)
            {
                DirectoryInfo di = new DirectoryInfo(TempPath);

                foreach (FileInfo file in di.EnumerateFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }
            else
            {
                Directory.CreateDirectory(TempPath);
            }
        }

        public static void UnzipAllDownloaded()
        {
            foreach (string filePath in GlobalConfig.DownloadedFilePaths)
            {
                if (filePath.Contains(".zip"))
                {
                    ZipFile.ExtractToDirectory(filePath, filePath.Replace(".zip", string.Empty));
                }
            }
        }

        /// <summary>
        /// Create our Output directories inside the directory the user chose.
        /// </summary>
        public static void CreateDirectories()
        {
            Directory.CreateDirectory(OutputPath);
            Directory.CreateDirectory($"{OutputPath}\\ALIAS");
            Directory.CreateDirectory($"{OutputPath}\\VRC");
            Directory.CreateDirectory($"{OutputPath}\\VSTARS");
            Directory.CreateDirectory($"{OutputPath}\\VERAM");
            Directory.CreateDirectory($"{OutputPath}\\VRC\\[SID]");
            Directory.CreateDirectory($"{OutputPath}\\VRC\\[STAR]");
        }
    }
}
