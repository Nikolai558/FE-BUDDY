using System.IO;
using System.IO.Compression;

namespace FeBuddyLibrary.Helpers
{
    public class WebHelpers
    {
        public static void DecompressGZipFile(string gzipFilePath, string destFilePath)
        {
            using (FileStream originalFileStream = new FileStream(gzipFilePath, FileMode.Open, FileAccess.Read))
            {
                using (FileStream decompressedFileStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }
    }
}
