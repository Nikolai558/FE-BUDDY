using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers <see cref="OutputFileNamesParser"/>: which files can be renamed, and what happens to an
/// entry for anything else.
/// </summary>
public sealed class OutputFileNamesParserTests
{
	[Fact]
	public void no_block_renames_nothing()
	{
		OutputFileNamesParseResult result = OutputFileNamesParser.Parse(null);

		Assert.Empty(result.FileNames.NewNames);
		Assert.Empty(result.Messages);
	}

	/// <summary>Every file a run writes, except the Departures and Arrivals GeoJSON files.</summary>
	[Theory]
	[InlineData("Airways_High_Lines")]
	[InlineData("Airways_J_Text")]
	[InlineData("Runways_Lines")]
	[InlineData("Airports_Symbols")]
	[InlineData("NAVAIDs_Symbols")]
	[InlineData("NAVAIDs_VOR-DMEs_Text")]
	[InlineData("ARTCC-Boundary_High_Lines")]
	[InlineData("ARTCC-Boundary_ZOB-HIGH_Lines")]
	[InlineData("Fix_Symbols")]
	[InlineData("Fix_WYPNT_Text")]
	[InlineData("Wx_Symbols")]
	[InlineData("Airways.txt")]
	[InlineData("Airports.txt")]
	[InlineData("Departures.txt")]
	[InlineData("Arrivals.txt")]
	[InlineData("Navaids.txt")]
	[InlineData("Faa_Chart_Recall.txt")]
	[InlineData("Telephony.txt")]
	[InlineData("Procedure_Changes.md")]
	[InlineData("Procedures.json")]
	[InlineData("Combined_Alias.txt")]
	[InlineData("Duplicate_Alias_Commands.txt")]
	[InlineData("duplicate_alias_commands.TXT")]
	public void a_file_a_run_writes_can_be_renamed(string key)
	{
		OutputFileNamesParseResult result = OutputFileNamesParser.Parse(new Dictionary<string, string> { [key] = "Mine" });

		Assert.True(OutputFileNamesParser.CanRename(key));
		Assert.Equal("Mine", result.FileNames.NewNames[key]);
		Assert.Empty(result.Messages);
	}

	[Theory]
	[InlineData("Departures_Lines")]
	[InlineData("Arrivals_Text")]
	public void a_departures_or_arrivals_geojson_file_keeps_its_name(string key)
	{
		OutputFileNamesParseResult result = OutputFileNamesParser.Parse(new Dictionary<string, string> { [key] = "Mine" });

		Assert.False(OutputFileNamesParser.CanRename(key));
		Assert.Empty(result.FileNames.NewNames);
		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Equal($"The new name for '{key}' was ignored: Departures and Arrivals name each procedure's GeoJSON file from the FAA's data, so those files keep their names.", message.Text);
	}

	[Theory]
	[InlineData("Airways_High_Lines.geojson")]
	[InlineData("Airways")]
	[InlineData("Nothing.txt")]
	public void a_key_for_no_file_is_ignored_with_a_warning(string key)
	{
		OutputFileNamesParseResult result = OutputFileNamesParser.Parse(new Dictionary<string, string> { [key] = "Mine" });

		Assert.Empty(result.FileNames.NewNames);
		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Equal($"The new name for '{key}' was ignored: it is not a file FE-Buddy writes.", message.Text);
	}

	[Fact]
	public void a_blank_name_keeps_fe_buddys_name()
	{
		OutputFileNamesParseResult result = OutputFileNamesParser.Parse(new Dictionary<string, string>
		{
			["Airways.txt"] = "  ",
			["Airports.txt"] = "",
		});

		Assert.Empty(result.FileNames.NewNames);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void a_name_that_cannot_be_used_stops_the_run()
	{
		Assert.Throws<ArgumentException>(() => OutputFileNamesParser.Parse(new Dictionary<string, string> { ["Airways.txt"] = "ZOB?" }));
	}
}
