using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Settings;

/// <summary>
/// Covers <see cref="SubServiceSettingsReader.ReadCrcDefaultsFiles"/>: the list defaults to empty,
/// and only a sub-service's own GeoJSON files are accepted. Also that a key no sub-service reads
/// any more (<c>UploadToVnas</c>) is warned about as unrecognized.
/// </summary>
public sealed class SubServiceSettingsReaderTests
{
	private static bool IsGeojson(string key) => key is "Things_Lines" or "Things_Text";

	private static Dictionary<string, string> Settings(params (string Key, string Value)[] entries) =>
		entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);

	private static CrcDefaultsFiles Read(params (string Key, string Value)[] entries) =>
		SubServiceSettingsReader.ReadCrcDefaultsFiles(
			Settings(entries), IsGeojson, example: "Things_Lines, Things_Text");

	[Fact]
	public void no_file_gets_crc_defaults_when_the_key_is_absent_or_blank()
	{
		Assert.Empty(Read().Files);
		Assert.Empty(Read(("CrcDefaultsFor", " ")).Files);
		Assert.Empty(Read(("CrcDefaultsFor", "")).Files);
	}

	[Fact]
	public void the_list_is_trimmed_and_blanks_ignored()
	{
		CrcDefaultsFiles files = Read(("CrcDefaultsFor", " Things_Lines , ,Things_Text,"));

		Assert.Equal(2, files.Files.Count);
		Assert.True(files.HasCrcDefaults("Things_Lines"));
		Assert.True(files.HasCrcDefaults("Things_Text"));
	}

	/// <summary>Only a GeoJSON file the sub-service writes can get CRC-ERAM defaults: not another one's, not an alias file, not junk.</summary>
	[Theory]
	[InlineData("Other_Lines")]
	[InlineData("Things.txt")]
	[InlineData("junk")]
	public void a_file_that_is_not_a_geojson_file_of_the_sub_service_is_rejected_with_an_example(string key)
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => Read(("CrcDefaultsFor", $"Things_Lines,{key}")));

		Assert.Contains($"'{key}'", ex.Message, StringComparison.Ordinal);
		Assert.Contains("'CrcDefaultsFor'", ex.Message, StringComparison.Ordinal);
		Assert.Contains("Things_Lines, Things_Text", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void crc_defaults_for_is_a_setting_every_sub_service_understands()
	{
		IReadOnlyList<ServiceMessage> warnings = SubServiceSettingsReader.UnknownKeyWarnings(
			Settings(("CrcDefaultsFor", "Things_Lines")),
			new HashSet<string>(),
			new Dictionary<string, CrcFeatureKind[]>(),
			source: "ThingsSettingsParser",
			labelSource: "each thing is labelled with its own ID");

		Assert.Empty(warnings);
	}

	/// <summary>A saved <c>UploadToVnas</c> from before every file became a vNAS file is no longer read, and says so.</summary>
	[Fact]
	public void a_stale_upload_to_vnas_key_gets_an_unrecognized_setting_warning()
	{
		IReadOnlyList<ServiceMessage> warnings = SubServiceSettingsReader.UnknownKeyWarnings(
			Settings(("UploadToVnas", "Things_Lines,Things.txt")),
			new HashSet<string>(),
			new Dictionary<string, CrcFeatureKind[]>(),
			source: "ThingsSettingsParser",
			labelSource: "each thing is labelled with its own ID");

		ServiceMessage warning = Assert.Single(warnings);

		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.Equal("ThingsSettingsParser", warning.Source);
		Assert.Equal("Unrecognized setting 'UploadToVnas' was ignored.", warning.Text);
	}
}
