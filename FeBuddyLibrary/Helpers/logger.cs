using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeBuddyLibrary.Helpers
{
    public class Logger
    {
        // TODO - Use actual Program Logger Framework.
        
        public static readonly string logFilePath = $"{Environment.CurrentDirectory}\\log.txt";
        
        public static void LogMessage(string level, string message)
        {
            string output = $"{DateTime.UtcNow:HH:mm:ss.fff} - {level} - {message}";

            File.AppendAllText(logFilePath, output += "\n");
        }
    }
}
