using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Domain.Navaids.Models;

using FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Navaids;

/// <summary>
/// Covers the <c>NAVAIDs.txt</c> alias file. Like <c>AirportAliasWriterTests</c>, the load-bearing
/// assertion is that a block holds literal <c>\n</c>, <c>\t</c> and <c>\s</c> escapes - never real
/// newlines, tabs or spaces holding a column - plus the NAVAIDs-specific command-merging rules:
/// several NAVAIDs sharing an ID or a name command are appended, joined by <c>\n---</c>, in the
/// order their command was first seen.
/// </summary>
public sealed class NavaidAliasWriterTests : IDisposable
{
	/// <summary>CGT's block, exactly as <c>NavaidAliasWriter</c> must produce it.</summary>
	private const string CgtBlock =
		@"\nNAVAID:\t\t\sCGT\s-\sCHICAGO HEIGHTS\n\t\t\t\tVORTAC\nFREQ:\t\t\s\s\s114.20\nARTCC\sHIGH:\t\sZAU\nARTCC\sLOW:\t\s\sZAU";

	private const string AbqVortacBlock =
		@"\nNAVAID:\t\t\sABQ\s-\sALBUQUERQUE\n\t\t\t\tVORTAC\nFREQ:\t\t\s\s\s113.20\nARTCC\sHIGH:\t\sZAB\nARTCC\sLOW:\t\s\sZAB";

	/// <summary>ABQ's VOT block: frequency "111.00" and blank ARTCC boundaries.</summary>
	private const string AbqVotBlock =
		@"\nNAVAID:\t\t\sABQ\s-\sALBUQUERQUE\n\t\t\t\tVOT\nFREQ:\t\t\s\s\s111.00\nARTCC\sHIGH:\t\s\nARTCC\sLOW:\t\s\s";

	private const string AaCedarBlock =
		@"\nNAVAID:\t\t\sAA\s-\sCEDAR\n\t\t\t\tNDB\nFREQ:\t\t\s\s\s341\nARTCC\sHIGH:\t\sZTL\nARTCC\sLOW:\t\s\sZTL";

	private const string AaKenieBlock =
		@"\nNAVAID:\t\t\sAA\s-\sKENIE\n\t\t\t\tNDB\nFREQ:\t\t\s\s\s365\nARTCC\sHIGH:\t\sZMP\nARTCC\sLOW:\t\s\sZMP";

	private const string ElyBlock =
		@"\nNAVAID:\t\t\sELY\s-\sELY\n\t\t\t\tVOR/DME\nFREQ:\t\t\s\s\s113.95\nARTCC\sHIGH:\t\sZLC\nARTCC\sLOW:\t\s\sZLC";

	private const string EloBlock =
		@"\nNAVAID:\t\t\sELO\s-\sELY\n\t\t\t\tDME\nFREQ:\t\t\s\s\s113.45\nARTCC\sHIGH:\t\sZMP\nARTCC\sLOW:\t\s\sZMP";

	/// <summary>The literal text joining two NAVAIDs' blocks under one shared command.</summary>
	private const string BlockSeparator = @"\n---";

	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_NavAlias_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_outputDirectory))
			{
				Directory.Delete(_outputDirectory, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	private NavaidSettings Settings() => new()
	{
		OutputDirectory = _outputDirectory,
		GenerateGeojson = false,
		GenerateAliasFile = true,
		IncludeFebCustomProperties = false,
	};

	[Fact]
	public void the_cgt_id_and_name_commands_hold_byte_for_byte_identical_bodies()
	{
		NavaidAliasGenerateResult result = NavaidAliasWriter.Generate([NavaidTestData.Cgt()], Settings());

		string expected =
			$".navCGT .echo {CgtBlock}" + Environment.NewLine +
			$".navCHICAGOHEIGHTS .echo {CgtBlock}" + Environment.NewLine;

		Assert.Equal(expected, File.ReadAllText(result.FilePath!));
		Assert.Equal(2, result.CommandCount);
	}

	[Fact]
	public void abq_id_and_name_commands_join_the_vortac_and_vot_blocks_with_the_separator()
	{
		NavaidAliasGenerateResult result = NavaidAliasWriter.Generate(
			[NavaidTestData.AbqVortac(), NavaidTestData.AbqVot()], Settings());

		string contents = File.ReadAllText(result.FilePath!);
		string expectedBody = AbqVortacBlock + BlockSeparator + AbqVotBlock;

		Assert.Contains($".navABQ .echo {expectedBody}" + Environment.NewLine, contents);
		Assert.Contains($".navALBUQUERQUE .echo {expectedBody}" + Environment.NewLine, contents);
		Assert.Equal(2, result.CommandCount);
	}

	[Fact]
	public void aa_command_holds_both_ndb_blocks_by_id_and_each_ndb_also_gets_its_own_name_command()
	{
		NavaidAliasGenerateResult result = NavaidAliasWriter.Generate(
			[NavaidTestData.AaCedar(), NavaidTestData.AaKenie()], Settings());

		string contents = File.ReadAllText(result.FilePath!);
		string expectedBody = AaCedarBlock + BlockSeparator + AaKenieBlock;

		Assert.Contains($".navAA .echo {expectedBody}" + Environment.NewLine, contents);
		Assert.Contains($".navCEDAR .echo {AaCedarBlock}" + Environment.NewLine, contents);
		Assert.Contains($".navKENIE .echo {AaKenieBlock}" + Environment.NewLine, contents);
		Assert.Equal(3, result.CommandCount);
	}

	[Fact]
	public void ely_command_holds_the_elo_block_then_elys_own_block_and_elys_own_name_does_not_duplicate_it()
	{
		NavaidAliasGenerateResult result = NavaidAliasWriter.Generate(
			[NavaidTestData.Elo(), NavaidTestData.Ely()], Settings());

		string contents = File.ReadAllText(result.FilePath!);
		string expectedElyBody = EloBlock + BlockSeparator + ElyBlock;

		Assert.Contains($".navELY .echo {expectedElyBody}" + Environment.NewLine, contents);
		Assert.Contains($".navELO .echo {EloBlock}" + Environment.NewLine, contents);

		// Written exactly once, not once per source NAVAID.
		Assert.Equal(1, contents.Split(".navELY .echo").Length - 1);
		Assert.Equal(2, result.CommandCount);
	}

	[Fact]
	public void commands_are_written_in_first_seen_order_not_alphabetical_order()
	{
		Navaid last = NavaidTestData.BuiltNavaid("ZZZ", "VOR", "ZULU", 108.0, 10, 10, "", "");
		Navaid first = NavaidTestData.BuiltNavaid("AAA", "VOR", "ALPHA", 108.0, 20, 20, "", "");

		NavaidAliasGenerateResult result = NavaidAliasWriter.Generate([last, first], Settings());

		string[] lines = File.ReadAllLines(result.FilePath!);

		Assert.Equal(4, lines.Length);
		Assert.StartsWith(".navZZZ ", lines[0]);
		Assert.StartsWith(".navZULU ", lines[1]);
		Assert.StartsWith(".navAAA ", lines[2]);
		Assert.StartsWith(".navALPHA ", lines[3]);
	}

	[Fact]
	public void a_name_with_no_letters_or_digits_gets_no_name_command()
	{
		Navaid navaid = NavaidTestData.BuiltNavaid("ABC", "VOR", "---", 108.0, 10, 10, "", "");

		NavaidAliasGenerateResult result = NavaidAliasWriter.Generate([navaid], Settings());

		Assert.Equal(1, result.CommandCount);
		Assert.StartsWith(".navABC .echo ", File.ReadAllText(result.FilePath!));
	}

	[Fact]
	public void the_alias_file_is_written_as_utf8_without_a_bom()
	{
		NavaidAliasGenerateResult result = NavaidAliasWriter.Generate([NavaidTestData.Cgt()], Settings());

		byte[] bytes = File.ReadAllBytes(result.FilePath!);

		Assert.Equal((byte)'.', bytes[0]);
	}

	[Fact]
	public void the_alias_file_goes_under_upload_to_vnas_when_marked()
	{
		NavaidSettings settings = Settings() with { Vnas = new VnasFileChoices([NavaidOutputFiles.Alias], []) };

		NavaidAliasGenerateResult result = NavaidAliasWriter.Generate([NavaidTestData.Cgt()], settings);

		Assert.Equal(Path.Combine(_outputDirectory, "Upload_to_vNAS", "NAVAIDs.txt"), result.FilePath);
	}

	[Fact]
	public void nothing_is_written_when_there_are_no_navaids()
	{
		NavaidAliasGenerateResult result = NavaidAliasWriter.Generate([], Settings());

		Assert.Null(result.FilePath);
		Assert.Equal(0, result.CommandCount);
	}

	[Fact]
	public void generate_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => NavaidAliasWriter.Generate(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => NavaidAliasWriter.Generate([], null!));
	}
}
