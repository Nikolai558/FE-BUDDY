using System.IO.Compression;

namespace FEBuddyLibrary.Handlers;
/// <summary>
/// Provides utility methods for handling files, including managing a temporary directory and unzipping downloaded files.
/// </summary>
public static class FileHandler
{
    /// <summary>
    /// The path to the temporary directory used for storing downloaded files.
    /// </summary>
    public static readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "FE-BUDDY");

    /// <summary>
    /// Unzips all downloaded files that are in the ZIP format.
    /// </summary>
    /// <param name="downloadFilePaths">A list of file paths for the downloaded files.</param>
    public static void UnzipAllDownloads(List<string> downloadFilePaths)
    {
        // Iterate through the list of downloaded file paths.
        foreach (string filePath in downloadFilePaths)
        {
            // Check if the file path contains ".zip" indicating it's a ZIP file.
            if (filePath.Contains(".zip"))
            {
                // Extract the contents of the ZIP file to a directory with the same name as the ZIP file (without the ".zip" extension).
                ZipFile.ExtractToDirectory(filePath, filePath.Replace(".zip", string.Empty));
            }
        }
    }

    /// <summary>
    /// Creates a temporary directory if it doesn't already exist.
    /// </summary>
    public static void CreateTempDirectory()
    {
        try
        {
            // Check if the temporary directory doesn't exist and create it if needed.
            if (!Directory.Exists(_tempDirectory)) Directory.CreateDirectory(_tempDirectory);
        }
        catch (Exception)
        {
            // If an exception occurs during directory creation, re-throw it to propagate it up the call stack.
            throw;
        }
    }

    /// <summary>
    /// Cleans the temporary directory by deleting all files and subdirectories.
    /// </summary>
    public static void CleanTempDirectory()
    {
        // Create a DirectoryInfo object for the temporary directory.
        DirectoryInfo tempDir = new DirectoryInfo(_tempDirectory);

        // Delete all files in the temporary directory.
        foreach (FileInfo file in tempDir.EnumerateFiles())
        {
            file.Delete();
        }

        // Delete all subdirectories and their contents in the temporary directory.
        foreach (DirectoryInfo directory in tempDir.EnumerateDirectories())
        {
            directory.Delete(true);
        }
    }
}
