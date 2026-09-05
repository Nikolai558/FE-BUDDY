using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class FebCsvHelper
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
        var headers = csv.HeaderRecord;

        while (csv.Read())
        {
            var record = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                record[header] = csv.GetField(header);
            }

            results.Add(lineProcessor(record));
        }

        return results;
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
