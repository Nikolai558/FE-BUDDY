using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyLibrary.Helpers
{
    public static class BatchFileHelpers
    {
        public static void CreateCurlBatchFile(string name, string url, string outputFileName)
        {
            string filePath = $"{GlobalConfig.tempPath}\\{name}";
            string writeMe = $"cd /d \"{GlobalConfig.tempPath}\"\n" +
                $"curl \"{url}\" > {outputFileName}";
            File.WriteAllText(filePath, writeMe);
        }

        public static void ExecuteCurlBatchFile(string batchFileName)
        {
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + $"\"{GlobalConfig.tempPath}\\{batchFileName}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process = Process.Start(ProcessInfo);
            Process.WaitForExit();

            Process.Close();
        }
    }
}
