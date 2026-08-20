using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Parsers.NASR.CSV;
using FEBuddyLibrary.Generators.NASR;
using System;
using System.IO;

namespace FEBuddyTest;

/// <summary>
/// A console application used to test different functions of the FEBuddyLibrary without the need for a GUI.
/// </summary>
internal static class Program
{
    public static void Main()
    {
        // Input/output directories
        string sourceDirectory = @"C:\Users\ksand\Downloads\03_Sep_2026_CSV";
        string outputDirectory = @"C:\Users\ksand\Downloads";


        Console.WriteLine("NASR CSV parsing... ");

        // Parse the NASR CSV files and store the data in a NasrCsvDataCollection object
        var allNasrCsvData = NasrCsvParserController.Main(new string[]
        {
            sourceDirectory
        });


        Console.Write("complete.");

        Console.WriteLine("Generating AWY GeoJSON...");

        string awyGeojsonPath = AwyGeojsonGenerator.Generate(
            allNasrCsvData,
            outputDirectory);

        Console.WriteLine($"AWY GeoJSON created: {awyGeojsonPath}");
    }
}
