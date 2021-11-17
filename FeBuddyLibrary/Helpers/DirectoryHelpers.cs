using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyLibrary.Helpers
{
    public class DirectoryHelpers
    {
        public static void CheckTempDir()
        {
            if (Directory.Exists(GlobalConfig.tempPath) && !GlobalConfig.updateProgram)
            {
                DirectoryInfo di = new DirectoryInfo(GlobalConfig.tempPath);

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
                Directory.CreateDirectory(GlobalConfig.tempPath);
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
            Directory.CreateDirectory(GlobalConfig.outputDirectory);
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\ALIAS");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VRC");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VSTARS");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VERAM");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VRC\\[SID]");
            Directory.CreateDirectory($"{GlobalConfig.outputDirectory}\\VRC\\[STAR]");
        }
    }
}
