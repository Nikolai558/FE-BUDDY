using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace FeBuddy.Core.Infrastructure.Nasr;

/// <summary>
/// Reads NASR CSV files: every row as a column-name dictionary, plus the value parsers the NASR
/// parsers use on those fields.
/// </summary>
public static class NasrCsvReader
{
	/// <summary>
	/// Reads every row of a CSV file with a header row, turning each into a value.
	/// </summary>
	/// <typeparam name="T">The row model to build.</typeparam>
	/// <param name="filePath">The CSV file.</param>
	/// <param name="lineProcessor">
	/// Builds one row model from that row's fields, keyed by column name. Values are trimmed; a
	/// column the row is too short to reach reads as an empty string, never <see langword="null"/>.
	/// </param>
	/// <returns>One model per data row, in file order.</returns>
	/// <exception cref="InvalidDataException">Thrown when the file has no header row.</exception>
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
	/// <param name="fields">One row's fields, as <see cref="ProcessLines{T}"/> passes them.</param>
	/// <param name="key">The column name.</param>
	/// <returns>The value, or an empty string when the file has no such column.</returns>
	public static string GetField(Dictionary<string, string> fields, string key)
	{
		return fields.TryGetValue(key, out string? value) ? value : string.Empty;
	}

	/// <summary>Parses a required integer field.</summary>
	/// <param name="value">The field's text.</param>
	/// <returns>The integer.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the field is blank.</exception>
	/// <exception cref="FormatException">Thrown when the field is not an integer.</exception>
	public static int ParseInt(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentNullException(nameof(value), "Expected non-null or non-empty string for int parsing.");

		if (!int.TryParse(value, out int result))
			throw new FormatException($"Invalid integer format: '{value}'");

		return result;
	}

	/// <summary>Parses an optional integer field.</summary>
	/// <param name="value">The field's text.</param>
	/// <returns>The integer, or <see langword="null"/> when the field is blank or not an integer.</returns>
	public static int? ParseNullableInt(string value)
	{
		return int.TryParse(value, out int result) ? result : (int?)null;
	}

	/// <summary>Parses a required decimal field.</summary>
	/// <param name="value">The field's text.</param>
	/// <returns>The number.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the field is blank.</exception>
	/// <exception cref="FormatException">Thrown when the field is not a number.</exception>
	public static double ParseDouble(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentNullException(nameof(value), "Expected non-null or non-empty string for double parsing.");

		if (!double.TryParse(value, out double result))
			throw new FormatException($"Invalid double format: '{value}'");

		return result;
	}

	/// <summary>Parses an optional decimal field.</summary>
	/// <param name="value">The field's text.</param>
	/// <returns>The number, or <see langword="null"/> when the field is blank or not a number.</returns>
	public static double? ParseNullableDouble(string value)
	{
		return double.TryParse(value, out double result) ? result : (double?)null;
	}


}
