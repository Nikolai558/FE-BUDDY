using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Settings;

namespace FeBuddy.UnitTests.Application.Settings;

/// <summary>
/// Covers <see cref="SubServiceSettingsReader.ReadVnasFiles"/>: both lists default to empty, only
/// a sub-service's own files are accepted, and CRC defaults go only on uploaded GeoJSON files.
/// </summary>
public sealed class SubServiceSettingsReaderTests
{
	private const string Alias = "Things.txt";

	private static bool IsGeojson(string key) => key is "Things_Lines" or "Things_Text";

	private static VnasFileChoices Read(params (string Key, string Value)[] entries) =>
		SubServiceSettingsReader.ReadVnasFiles(
			entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase),
			Alias, IsGeojson, example: "Things_Lines, Things.txt");

	[Fact]
	public void nothing_is_uploaded_when_the_keys_are_absent_or_blank()
	{
		Assert.Empty(Read().UploadFiles);
		Assert.Empty(Read(("UploadToVnas", " "), ("CrcDefaultsFor", "")).UploadFiles);
	}

	[Fact]
	public void the_lists_are_trimmed_and_blanks_ignored()
	{
		VnasFileChoices choices = Read(("UploadToVnas", " Things_Lines , ,Things.txt"), ("CrcDefaultsFor", "Things_Lines,"));

		Assert.Equal(2, choices.UploadFiles.Count);
		Assert.True(choices.IsUploaded(Alias));
		Assert.True(choices.HasCrcDefaults("Things_Lines"));
	}

	[Fact]
	public void a_file_the_sub_service_does_not_write_is_rejected_with_an_example()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => Read(("UploadToVnas", "Things_Lines,Other_Lines")));

		Assert.Contains("'Other_Lines'", ex.Message, StringComparison.Ordinal);
		Assert.Contains("Things_Lines, Things.txt", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void crc_defaults_for_a_file_not_uploaded_are_rejected()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			Read(("UploadToVnas", "Things_Lines"), ("CrcDefaultsFor", "Things_Text")));

		Assert.Contains("not in 'UploadToVnas'", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void crc_defaults_for_the_alias_file_are_rejected()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			Read(("UploadToVnas", "Things.txt"), ("CrcDefaultsFor", "things.txt")));

		Assert.Contains("alias file", ex.Message, StringComparison.Ordinal);
	}

	// ---- a null aliasFileKey (a sub-service with no alias file, e.g. ARTCC Boundaries) ----

	private static VnasFileChoices ReadNoAlias(params (string Key, string Value)[] entries) =>
		SubServiceSettingsReader.ReadVnasFiles(
			entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase),
			aliasFileKey: null, IsGeojson, example: "Things_Lines");

	[Fact]
	public void a_null_alias_file_key_accepts_geojson_keys()
	{
		VnasFileChoices choices = ReadNoAlias(("UploadToVnas", "Things_Lines"), ("CrcDefaultsFor", "Things_Lines"));

		Assert.True(choices.IsUploaded("Things_Lines"));
		Assert.True(choices.HasCrcDefaults("Things_Lines"));
	}

	[Fact]
	public void a_null_alias_file_key_rejects_the_key_the_alias_file_would_have_used()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => ReadNoAlias(("UploadToVnas", Alias)));

		Assert.Contains($"'{Alias}'", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void a_null_alias_file_key_rejects_any_other_unrecognized_key_too()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => ReadNoAlias(("UploadToVnas", "junk")));

		Assert.Contains("'junk'", ex.Message, StringComparison.Ordinal);
	}
}
