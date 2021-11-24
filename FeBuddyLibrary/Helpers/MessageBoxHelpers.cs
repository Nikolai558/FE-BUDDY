using System.Collections.Generic;
using System.Windows;

namespace FeBuddyLibrary.Helpers
{
    public class MessageBoxHelpers
    {
        public static void FileDownloadErrorMB(string fileName, Dictionary<string, string> allURLs)
        {
            // Yes I know this function is just this one line...Maybe change it
            MessageBox.Show($"FAILED DOWNLOADING: \n\n{fileName}\n{allURLs[fileName]}\n\nThis program will exit.\nPlease try again.");
        }
    }
}
