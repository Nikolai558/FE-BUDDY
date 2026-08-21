using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Parsers.NASR.CSV;
using FEBuddyLibrary.Generators.NASR;
using System;
using System.Collections.Generic;
using System.IO;

namespace FEBuddyTest;

internal static class Program
{
    public static void Main()
    {
        string sourceDirectory = @"C:\Users\ksand\Downloads\03_Sep_2026_CSV";

        // AWY GeoJSON generation settings
        Dictionary<string, string> airwaySettings = new()
        {
            { "OutputDirectory", @"C:\Users\ksand\Downloads" },
            { "OutputBy", "HighLow" },
            { "SplitAtAntimeridian", "Y" },
            { "WaypointBuffer", "Y" }
        };

        Console.Write("NASR CSV parsing... ");

        var allNasrCsvData = NasrCsvParserController.Main(new string[]
        {
            sourceDirectory
        });

        Console.Write("complete.");

        Console.WriteLine("\n\nGenerating AWY GeoJSON...");

        string awyGeojsonPath = AwyGeojsonGenerator.Generate(
            allNasrCsvData,
            airwaySettings);

        Console.WriteLine($"AWY GeoJSON created: {awyGeojsonPath}");
    }
}