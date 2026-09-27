using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Domain.Telephony.Models;

namespace FeBuddy.UnitTests.Application.Airac.Telephony;

/// <summary>
/// Covers the <c>Telephony.txt</c> alias file. Like <c>NavaidAliasWriterTests</c>, the load-bearing
/// assertion is that a card holds literal <c>\n</c>, <c>\t</c> and <c>\s</c> escapes - never real
/// newlines, tabs or spaces holding a column - plus Telephony's own merge rules: a telephony that
/// spells another operator's designator, two telephonies that only differ by spaces, an identifier
/// equal to its own telephony getting one command instead of two, and commands written in ordinal
/// alphabetical order.
/// </summary>
public sealed class TelephonyAliasWriterTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_TelephonyAlias_" + Guid.NewGuid().ToString("N"));

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

	private TelephonySettings Settings(VnasFileChoices? vnas = null) => new()
	{
		OutputDirectory = _outputDirectory,
		Vnas = vnas ?? VnasFileChoices.None,
	};

	private static TelephonyEntry Icao(string designator, string telephony, string company, string country) =>
		new(TelephonyEntryKind.IcaoAssignment, designator, telephony, company, country);

	private static TelephonyEntry Special(string identifier, string telephony, string agency, string expires) =>
		new(TelephonyEntryKind.UsSpecialCallSign, identifier, telephony, agency, expires);

	// ---- BuildCard ----

	[Fact]
	public void build_card_produces_the_exact_icao_assignment_escapes()
	{
		TelephonyEntry entry = Icao("AVA", "AVIANCA", "AEROVIAS DEL CONTINENTE AMERICANO S.A.", "COLOMBIA");

		string card = TelephonyAliasWriter.BuildCard(entry);

		Assert.Equal(
			@"\n3LD:\t\t\tAVA\nTELEPHONY:\t\s\sAVIANCA\nCOMPANY:\t\tAEROVIAS DEL CONTINENTE AMERICANO S.A.\nCOUNTRY:\t\tCOLOMBIA",
			card);
	}

	[Fact]
	public void build_card_produces_the_exact_special_call_sign_escapes()
	{
		TelephonyEntry entry = Special("ARSIX", "AIR SIX", "NYC ENVIRONMENTAL PROTECTION (NEW WINDSOR, NY)", "24-FEB-2027");

		string card = TelephonyAliasWriter.BuildCard(entry);

		Assert.Equal(
			@"\nID:\t\t\t\sARSIX\nTELEPHONY:\t\s\sAIR SIX\nAGENCY:\t\t\sNYC ENVIRONMENTAL PROTECTION (NEW WINDSOR, NY)\nEXPIRES:\t\t24-FEB-2027",
			card);
	}

	// ---- file path / format ----

	[Fact]
	public void the_alias_file_goes_in_the_aliases_folder_by_default()
	{
		TelephonyAliasGenerateResult result = TelephonyAliasWriter.Generate(
			[Icao("AVA", "AVIANCA", "AVIANCA S.A.", "COLOMBIA")], Settings());

		Assert.Equal(Path.Combine(_outputDirectory, "Aliases", "Telephony.txt"), result.FilePath);
	}

	[Fact]
	public void the_alias_file_stays_in_the_aliases_folder_when_marked_for_vnas()
	{
		TelephonySettings settings = Settings(new VnasFileChoices([TelephonyOutputFiles.Alias], []));

		TelephonyAliasGenerateResult result = TelephonyAliasWriter.Generate(
			[Icao("AVA", "AVIANCA", "AVIANCA S.A.", "COLOMBIA")], settings);

		Assert.Equal(Path.Combine(_outputDirectory, "Aliases", "Telephony.txt"), result.FilePath);
	}

	[Fact]
	public void the_alias_file_is_written_as_utf8_without_a_bom()
	{
		TelephonyAliasGenerateResult result = TelephonyAliasWriter.Generate(
			[Icao("AVA", "AVIANCA", "AVIANCA S.A.", "COLOMBIA")], Settings());

		byte[] bytes = File.ReadAllBytes(result.FilePath!);

		Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..]);
	}

	[Fact]
	public void no_entries_writes_nothing_and_creates_no_folder()
	{
		TelephonyAliasGenerateResult result = TelephonyAliasWriter.Generate([], Settings());

		Assert.Null(result.FilePath);
		Assert.Equal(0, result.CommandCount);
		Assert.Equal(0, result.MergedCommandCount);
		Assert.False(Directory.Exists(_outputDirectory));
	}

	[Fact]
	public void generate_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => TelephonyAliasWriter.Generate(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => TelephonyAliasWriter.Generate([], null!));
	}

	// ---- merge / ordering / dedup ----

	[Fact]
	public void a_telephony_that_spells_another_operators_designator_lists_that_operator_first()
	{
		TelephonyEntry avianca = Icao("AVA", "AVIANCA", "AEROVIAS DEL CONTINENTE AMERICANO S.A.", "COLOMBIA");
		TelephonyEntry axv = Icao("AXV", "AVA", "SOME OTHER OPERATOR", "SOME COUNTRY");

		TelephonyAliasGenerateResult result = TelephonyAliasWriter.Generate([avianca, axv], Settings());
		string contents = File.ReadAllText(result.FilePath!);

		string expectedBody = TelephonyAliasWriter.BuildCard(avianca) + @"\n---" + TelephonyAliasWriter.BuildCard(axv);
		Assert.Contains($".idAVA .echo {expectedBody}" + Environment.NewLine, contents);
		Assert.Equal(3, result.CommandCount); // .idAVA (merged), .idAVIANCA, .idAXV
		Assert.Equal(1, result.MergedCommandCount);
	}

	[Fact]
	public void ryan_air_and_ryanair_share_one_command_with_the_first_entrys_card_first()
	{
		TelephonyEntry rya = Icao("RYA", "RYAN AIR", "RYANAIR DAC", "IRELAND");
		TelephonyEntry ryr = Icao("RYR", "RYANAIR", "RYANAIR UK LTD", "UNITED KINGDOM");

		TelephonyAliasGenerateResult result = TelephonyAliasWriter.Generate([rya, ryr], Settings());
		string contents = File.ReadAllText(result.FilePath!);

		string expectedBody = TelephonyAliasWriter.BuildCard(rya) + @"\n---" + TelephonyAliasWriter.BuildCard(ryr);
		Assert.Contains($".idRYANAIR .echo {expectedBody}" + Environment.NewLine, contents);
		Assert.Equal(3, result.CommandCount); // .idRYA, .idRYR, .idRYANAIR (merged)
		Assert.Equal(1, result.MergedCommandCount);
	}

	[Fact]
	public void an_identifier_equal_to_its_own_telephony_gets_one_command_not_two()
	{
		TelephonyEntry nasa = Special("NASA", "NASA", "NATIONAL AERONAUTICS AND SPACE ADMINISTRATION", "N/A");

		TelephonyAliasGenerateResult result = TelephonyAliasWriter.Generate([nasa], Settings());
		string contents = File.ReadAllText(result.FilePath!);

		Assert.Equal(1, result.CommandCount);
		Assert.Equal(0, result.MergedCommandCount);
		Assert.Equal(1, contents.Split(".idNASA ").Length - 1);
	}

	[Fact]
	public void a_telephony_with_no_letter_or_digit_gets_no_command_of_its_own()
	{
		TelephonyEntry dashes = Icao("DSH", "--", "DASH OPERATOR", "COUNTRY D");

		TelephonyAliasGenerateResult result = TelephonyAliasWriter.Generate([dashes], Settings());

		Assert.Equal(1, result.CommandCount);
		Assert.StartsWith(".idDSH .echo ", Assert.Single(File.ReadAllLines(result.FilePath!)));
	}

	[Fact]
	public void commands_are_written_in_ordinal_alphabetical_order()
	{
		TelephonyEntry zulu = Icao("ZZZ", "ZULU OPERATOR", "ZULU OPERATOR CO", "COUNTRY Z");
		TelephonyEntry alpha = Icao("AAA", "ALPHA OPERATOR", "ALPHA OPERATOR CO", "COUNTRY A");

		TelephonyAliasGenerateResult result = TelephonyAliasWriter.Generate([zulu, alpha], Settings());
		string[] lines = File.ReadAllLines(result.FilePath!);

		Assert.Equal(4, lines.Length);
		Assert.StartsWith(".idAAA ", lines[0]);
		Assert.StartsWith(".idALPHAOPERATOR ", lines[1]);
		Assert.StartsWith(".idZULUOPERATOR ", lines[2]);
		Assert.StartsWith(".idZZZ ", lines[3]);
	}
}
