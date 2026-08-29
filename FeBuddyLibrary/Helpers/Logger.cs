using System;
using System.IO;

namespace FeBuddyLibrary.Helpers
{
    public class Logger
    {
        // TODO - Use actual Program Logger Framework.

        private static readonly string _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FE-BUDDY", "Logs");
        public static readonly string _logFilePath = Path.Combine(_logDirectory, "FE-BUDDY_LOG.txt");

        public static void LogMessage(string level, string message)
        {
            string output = $"{DateTime.UtcNow:HH:mm:ss.fff} - {level} - {message}\n";

            try
            {
                File.AppendAllText(_logFilePath, output);
            }
            catch (DirectoryNotFoundException)
            {
                // The log directory can disappear mid-session: it lives under
                // %LocalAppData%\FE-BUDDY\, and Squirrel's uninstaller wipes that whole
                // tree during the Squirrel->MSI migration (see FeBuddyLibrary.Update.
                // SquirrelInstall). Recreate it and retry once - a failed log write must
                // never take the app down.
                try
                {
                    Directory.CreateDirectory(_logDirectory);
                    File.AppendAllText(_logFilePath, output);
                }
                catch
                {
                    // Give up on this one line rather than throwing into the caller.
                }
            }
            catch
            {
                // Logging is best-effort; never let it propagate.
            }
        }

        public static void CreateLogFile()
        {
            Directory.CreateDirectory(_logDirectory);

            string logHeader = "This file may serve useful to the developers in the case of program issues. Please send this file with your bug report.";

            File.WriteAllText(_logFilePath, logHeader += "\n\n");
        }
    }
}
