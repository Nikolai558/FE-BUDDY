using System;
using System.IO;

namespace FeBuddyLibrary.Helpers
{
    public class Logger
    {
        // TODO - Use actual Program Logger Framework.

        public static readonly string _logFilePath = $"{Environment.CurrentDirectory}\\FE-BUDDY_LOG.txt";

        public static void LogMessage(string level, string message)
        {
            string output = $"{DateTime.UtcNow:HH:mm:ss.fff} - {level} - {message}";

            File.AppendAllText(_logFilePath, output += "\n");
        }

        public static void CreateLogFile()
        {
            string logHeader = "This file may serve useful to the developers in the case of program issues. Please send this file with your bug report.";

            File.WriteAllText(_logFilePath, logHeader += "\n\n");
        }
    }
}
