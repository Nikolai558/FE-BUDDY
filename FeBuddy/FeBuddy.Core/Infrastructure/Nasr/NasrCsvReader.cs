using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FeBuddy.Core.Infrastructure.Nasr;

public static class NasrCsvReader
{
    public static List<T> ProcessLines<T>(string filePath, Func<Dictionary<string, string>, T> lineProcessor)
    {
        var results = new List<T>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        csv.Read();
        csv.ReadHeader();

        // ReadHeader either fills HeaderRecord or throws, so this only fires if CsvHelper's
        // behaviour changes - and then it names the file rather than failing on a null below.
        string[] headers = csv.HeaderRecord
            ?? throw new InvalidDataException($"CSV file '{filePath}' has no header row.");

        while (csv.Read())
        {
            var record = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                // CsvHelper returns null for a column this row is too short to reach (with
                // MissingFieldFound off). Stored as empty, the same as GetField below does for a
                // column the whole file lacks, so every row value is a non-null string.
                record[header] = csv.GetField(header) ?? string.Empty;
            }

            results.Add(lineProcessor(record));
        }

        return results;
    }

    /// <summary>
    /// Reads a field from a parsed CSV row, returning an empty string instead of throwing
    /// when the column is absent from this particular file.
    /// </summary>
    /// <remarks>
    /// <see cref="ProcessLines{T}"/> builds each row's dictionary only from the columns that
    /// actually appear in that file's header record, so a column present in one NASR CSV
    /// cycle can be silently absent in another (the FAA has added and removed optional
    /// columns - e.g. the ACN-PCN pavement classification fields on APT_RWY.csv - between
    /// cycles without notice). Use this instead of the dictionary indexer for any field that
    /// isn't guaranteed to exist in every cycle's file.
    /// </remarks>
    public static string GetField(Dictionary<string, string> fields, string key)
    {
        return fields.TryGetValue(key, out string? value) ? value : string.Empty;
    }

    // Safely parse a non-nullable int.
    public static int ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value), "Expected non-null or non-empty string for int parsing.");

        if (!int.TryParse(value, out int result))
            throw new FormatException($"Invalid integer format: '{value}'");

        return result;
    }

    // Safely parse a nullable int.
    public static int? ParseNullableInt(string value)
    {
        return int.TryParse(value, out int result) ? result : (int?)null;
    }

    // Safely parse a non-nullable double.
    public static double ParseDouble(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value), "Expected non-null or non-empty string for double parsing.");

        if (!double.TryParse(value, out double result))
            throw new FormatException($"Invalid double format: '{value}'");

        return result;
    }

    // Safely parse a nullable double.
    public static double? ParseNullableDouble(string value)
    {
        return double.TryParse(value, out double result) ? result : (double?)null;
    }


}
