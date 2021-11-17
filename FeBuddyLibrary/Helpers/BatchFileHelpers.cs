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
        private static string ProgramTempPath { get; } = GlobalConfig.tempPath;

        public static void CreateCurlBatchFile(string name, string url, string outputFileName)
        {
            string filePath = $"{ProgramTempPath}\\{name}";
            string writeMe = $"cd /d \"{ProgramTempPath}\"\n" +
                $"curl \"{url}\" > {outputFileName}";
            File.WriteAllText(filePath, writeMe);
        }

        public static void ExecuteCurlBatchFile(string batchFileName)
        {
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + $"\"{ProgramTempPath}\\{batchFileName}\"")
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
