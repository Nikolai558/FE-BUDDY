using FEBuddyLibrary.Generators.NASR;
using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Parsers.NASR.CSV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FEBuddyTest;

internal static class Program
{
    public static async Task Main()
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

        var stopwatch = Stopwatch.StartNew();

        var allNasrCsvData = await NasrCsvParserController.MainAsync(new string[]
        {
            sourceDirectory
        });

        stopwatch.Stop();

        Console.WriteLine($"complete. ({stopwatch.ElapsedMilliseconds:N0} ms)");









        Console.WriteLine("\n\nGenerating AWY GeoJSON...");

        string awyGeojsonPath = AwyGeojsonGenerator.Generate(
            allNasrCsvData,
            airwaySettings);

        Console.WriteLine($"AWY GeoJSON created: {awyGeojsonPath}");
    }
}