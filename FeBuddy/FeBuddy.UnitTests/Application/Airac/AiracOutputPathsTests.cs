using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers <see cref="AiracOutputPaths"/>: the cycle folder, with and without the
/// <c>FE-Buddy_Output</c> folder, and where each kind of file goes inside it.
/// </summary>
public sealed class AiracOutputPathsTests
{
	private static readonly string Output = Path.Combine("C:", "Out");

	[Fact]
	public void a_cycle_folder_is_named_for_its_cycle()
	{
		Assert.Equal("AIRAC_2610", AiracOutputPaths.CycleFolderName("2610"));
	}

	[Theory]
	[InlineData(true, "FE-Buddy_Output", "AIRAC_2610")]
	[InlineData(false, "AIRAC_2610", null)]
	public void the_cycle_folder_goes_inside_fe_buddy_output_only_when_asked(bool addFeBuddyOutputFolder, string first, string? second)
	{
		string expected = second is null ? Path.Combine(Output, first) : Path.Combine(Output, first, second);

		Assert.Equal(expected, AiracOutputPaths.CycleDirectory(Output, addFeBuddyOutputFolder, "2610"));
	}

	[Theory]
	[InlineData(false, false, new string[0])]
	[InlineData(true, false, new[] { "Geojson" })]
	[InlineData(false, true, new[] { "Upload_to_vNAS" })]
	[InlineData(true, true, new[] { "Upload_to_vNAS", "Geojson" })]
	public void a_file_goes_in_geojson_and_or_upload_to_vnas(bool isGeojson, bool uploadToVnas, string[] folders)
	{
		Assert.Equal(
			Path.Combine([Output, .. folders]),
			AiracOutputPaths.FileDirectory(Output, isGeojson, uploadToVnas));
	}

	[Theory]
	[InlineData(CrcFeatureKind.Line, "Lines")]
	[InlineData(CrcFeatureKind.Symbol, "Symbols")]
	[InlineData(CrcFeatureKind.Text, "Text")]
	public void each_kind_has_its_file_name_suffix(CrcFeatureKind kind, string expected)
	{
		Assert.Equal(expected, AiracOutputPaths.FileKindSuffix(kind));
	}

	[Fact]
	public void an_unknown_kind_has_no_suffix()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => AiracOutputPaths.FileKindSuffix((CrcFeatureKind)99));
	}
}
