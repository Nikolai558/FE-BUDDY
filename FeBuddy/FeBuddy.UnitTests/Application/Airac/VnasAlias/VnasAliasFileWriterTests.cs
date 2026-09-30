using System.Text;

using FeBuddy.Core.Application.Airac.VnasAlias;
using FeBuddy.Core.Application.Airac.VnasAlias.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Airac.VnasAlias;

/// <summary>
/// Covers <see cref="VnasAliasFileWriter"/>: <c>vNAS_Alias.txt</c> is FE-Buddy's marked alias files
/// between a start and an end line, then the custom alias files, so CRC (last copy wins) uses the
/// user's commands; a <c>.FeUseOnly</c> line stays first; an old <c>vNAS_Alias.txt</c> used as a
/// custom file loses its FE-Buddy section, in either layout; commands in more than one file are
/// reported; and a custom file that could not be read is left out with a warning.
/// </summary>
public sealed class VnasAliasFileWriterTests : IDisposable
{
	private const string Start2610 =
		"; ===== FE-Buddy aliases (AIRAC 2610) start here. FE-Buddy replaces everything down to the end line every cycle. =====";

	private const string End =
		"; ===== End of FE-Buddy aliases. Your own aliases go below this line: CRC uses the last copy of a command, so yours replace FE-Buddy's. =====";

	private readonly string _output = Path.Combine(Path.GetTempPath(), "FeBuddyTests_VnasAlias_" + Guid.NewGuid().ToString("N"));

	public VnasAliasFileWriterTests() => Directory.CreateDirectory(Path.Combine(_output, "Aliases"));

	private string VnasAliasPath => Path.Combine(_output, "Upload_to_vNAS", "vNAS_Alias.txt");

	public void Dispose() => Directory.Delete(_output, recursive: true);

	[Fact]
	public void fe_buddy_files_come_first_between_the_markers_then_the_custom_files()
	{
		string airways = FeBuddyFile("Airways.txt", ".J60F .FF A B C\r\n");
		string telephony = FeBuddyFile("Telephony.txt", "\r\n.idAVA .MSG AVIANCA\r\n.idAAL .MSG AMERICAN\r\n\r\n");

		VnasAliasResult result = VnasAliasFileWriter.Write(
			[
				Read(1, "ZOB-Alias.txt", ".FeUseOnly keep me first\r\n# ZOB\r\n\r\n.dtwdv .ECHO DTW\r\n\r\n\r\n"),
				Read(2, "Extra.txt", "\n\n.extra .ECHO extra\n"),
			],
			[airways, telephony],
			"2610",
			_output);

		Assert.Equal(VnasAliasPath, result.FilePath);
		Assert.Equal(
			Lines(
				".FeUseOnly keep me first",
				Start2610,
				"; ----- Airways.txt -----",
				".J60F .FF A B C",
				"; ----- Telephony.txt -----",
				".idAVA .MSG AVIANCA",
				".idAAL .MSG AMERICAN",
				End,
				"",
				"# ZOB",
				"",
				".dtwdv .ECHO DTW",
				"",
				".extra .ECHO extra"),
			File.ReadAllText(VnasAliasPath));

		Assert.Equal(2, result.CustomFileCount);
		Assert.Equal(2, result.CustomFilesMerged);
		Assert.Equal(2, result.CustomCommandCount);
		Assert.Equal(["Airways.txt", "Telephony.txt"], result.FeBuddyFiles);
		Assert.Equal(3, result.FeBuddyCommandCount);
		Assert.Equal(0, result.DuplicateCommandCount);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void the_file_is_utf8_without_a_byte_order_mark()
	{
		VnasAliasFileWriter.Write([Read(1, "a.txt", "\uFEFF.é .ECHO é")], [], "2610", _output);

		byte[] bytes = File.ReadAllBytes(VnasAliasPath);

		Assert.Equal((byte)'.', bytes[0]);
		Assert.Equal(Lines(".é .ECHO é"), Encoding.UTF8.GetString(bytes));
	}

	[Fact]
	public void only_the_first_fe_use_only_line_is_kept_and_it_goes_first()
	{
		VnasAliasFileWriter.Write(
			[
				Read(1, "a.txt", ".a .ECHO a"),
				Read(2, "b.txt", "  .FEUSEONLY from b\r\n.b .ECHO b"),
				Read(3, "c.txt", ".FeUseOnly from c\r\n.c .ECHO c"),
			],
			[],
			"2610",
			_output);

		Assert.Equal(
			Lines("  .FEUSEONLY from b", ".a .ECHO a", "", ".b .ECHO b", "", ".c .ECHO c"),
			File.ReadAllText(VnasAliasPath));
	}

	/// <summary>A file uploaded before the end line existed: FE-Buddy's section was last, so it runs to the end.</summary>
	[Fact]
	public void an_old_vnas_alias_file_with_the_fe_buddy_section_last_loses_it()
	{
		string oldUpload = Lines(
			".mine .ECHO mine",
			"",
			"; ===== FE-Buddy aliases (AIRAC 2609) start here. FE-Buddy replaces everything below this line every cycle. =====",
			"; ----- Airways.txt -----",
			".J60F .FF OLD");

		VnasAliasResult result = VnasAliasFileWriter.Write(
			[Read(1, "ZOB-Alias.txt", oldUpload)], [FeBuddyFile("Airways.txt", ".J60F .FF NEW")], "2610", _output);

		Assert.Equal(
			Lines(Start2610, "; ----- Airways.txt -----", ".J60F .FF NEW", End, "", ".mine .ECHO mine"),
			File.ReadAllText(VnasAliasPath));
		Assert.Equal(1, result.CustomCommandCount);
		Assert.Equal(0, result.DuplicateCommandCount);

		ServiceMessage notice = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, notice.Level);
		Assert.Equal(
			"custom alias file 1 (ZOB-Alias.txt) holds FE-Buddy aliases from an earlier vNAS_Alias.txt; they were left out, so they are not added twice.",
			notice.Text);
	}

	/// <summary>A file in today's layout: only the lines outside FE-Buddy's section are the user's.</summary>
	[Fact]
	public void an_old_vnas_alias_file_with_the_fe_buddy_section_first_keeps_what_is_around_it()
	{
		string oldUpload = Lines(
			".FeUseOnly mine",
			"; ===== FE-Buddy aliases (AIRAC 2609) start here. FE-Buddy replaces everything down to the end line every cycle. =====",
			"; ----- Airways.txt -----",
			".J60F .FF OLD",
			End,
			"",
			".mine .ECHO mine");

		VnasAliasResult result = VnasAliasFileWriter.Write(
			[Read(1, "ZOB-Alias.txt", oldUpload)], [FeBuddyFile("Airways.txt", ".J60F .FF NEW")], "2610", _output);

		Assert.Equal(
			Lines(".FeUseOnly mine", Start2610, "; ----- Airways.txt -----", ".J60F .FF NEW", End, "", ".mine .ECHO mine"),
			File.ReadAllText(VnasAliasPath));
		Assert.Equal(LogLevel.Info, Assert.Single(result.Messages).Level);
	}

	/// <summary>A facility that uploads the file, then uses it as next cycle's custom file, gets the same file back.</summary>
	[Fact]
	public void merging_the_written_file_again_gives_the_same_file()
	{
		string airways = FeBuddyFile("Airways.txt", ".J60F .FF A B C");
		VnasAliasFileWriter.Write([Read(1, "ZOB-Alias.txt", ".FeUseOnly x\r\n.mine .ECHO mine\r\n\r\n.also .ECHO also")], [airways], "2610", _output);
		string first = File.ReadAllText(VnasAliasPath);

		VnasAliasFileWriter.Write([Read(1, "vNAS_Alias.txt", first)], [airways], "2610", _output);

		Assert.Equal(first, File.ReadAllText(VnasAliasPath));
	}

	[Fact]
	public void a_custom_file_that_was_only_fe_buddy_aliases_adds_nothing()
	{
		string oldUpload = Lines(
			"; ===== FE-Buddy aliases (AIRAC 2609) start here. FE-Buddy replaces everything down to the end line every cycle. =====",
			".J60F .FF OLD",
			End);

		VnasAliasFileWriter.Write([Read(1, "vNAS_Alias.txt", oldUpload)], [FeBuddyFile("Airways.txt", ".J60F .FF NEW")], "2610", _output);

		Assert.Equal(Lines(Start2610, "; ----- Airways.txt -----", ".J60F .FF NEW", End), File.ReadAllText(VnasAliasPath));
	}

	[Fact]
	public void fe_buddy_files_alone_have_just_the_fe_buddy_section()
	{
		VnasAliasResult result = VnasAliasFileWriter.Write([], [FeBuddyFile("Navaids.txt", ".CLE .FF CLE")], "2610", _output);

		Assert.Equal(
			Lines(Start2610, "; ----- Navaids.txt -----", ".CLE .FF CLE", End),
			File.ReadAllText(VnasAliasPath));
		Assert.Equal(0, result.CustomFileCount);
	}

	[Fact]
	public void custom_files_alone_have_no_fe_buddy_section()
	{
		VnasAliasFileWriter.Write([Read(1, "a.txt", ".a .ECHO a")], [], "2610", _output);

		Assert.Equal(Lines(".a .ECHO a"), File.ReadAllText(VnasAliasPath));
	}

	[Fact]
	public void a_custom_file_that_could_not_be_read_is_left_out_with_an_advisory_warning()
	{
		VnasAliasResult result = VnasAliasFileWriter.Write(
			[
				AliasSourceLoad.Failed(new AliasSource(1, AliasSourceKind.Url, "https://github.com/o/r/blob/main/ZOB-Alias.txt"), "GitHub refused the credential 'ZOB GitHub'."),
				Read(2, "b.txt", ".b .ECHO b"),
			],
			[FeBuddyFile("Airways.txt", ".J60F .FF A")],
			"2610",
			_output);

		Assert.Equal(2, result.CustomFileCount);
		Assert.Equal(1, result.CustomFilesMerged);
		Assert.EndsWith(Lines(End, "", ".b .ECHO b"), File.ReadAllText(VnasAliasPath), StringComparison.Ordinal);

		ServiceMessage warning = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.True(warning.IsAdvisory);
		Assert.Equal(
			"Left custom alias file 1 (ZOB-Alias.txt) out of vNAS_Alias.txt: GitHub refused the credential 'ZOB GitHub'. " +
			"Uploading the file without it would remove its aliases from vNAS.",
			warning.Text);
	}

	[Fact]
	public void nothing_to_merge_writes_no_file_and_says_why()
	{
		VnasAliasResult result = VnasAliasFileWriter.Write(
			[AliasSourceLoad.Failed(new AliasSource(1, AliasSourceKind.File, @"C:\Gone.txt"), "C:\\Gone.txt was not found.")],
			[],
			"2610",
			_output);

		Assert.Null(result.FilePath);
		Assert.False(Directory.Exists(Path.Combine(_output, "Upload_to_vNAS")));
		Assert.Equal(2, result.Messages.Count);
		Assert.Equal(
			"vNAS_Alias.txt was not written: no custom alias file could be read, and no FE-Buddy alias file is marked for vNAS.",
			result.Messages[1].Text);
		Assert.True(result.Messages[1].IsAdvisory);
	}

	[Fact]
	public void nothing_to_merge_deletes_the_file_an_earlier_run_wrote()
	{
		VnasAliasFileWriter.Write([Read(1, "a.txt", ".a .ECHO a")], [], "2610", _output);
		Assert.True(File.Exists(VnasAliasPath));

		VnasAliasResult result = VnasAliasFileWriter.Write(
			[AliasSourceLoad.Failed(new AliasSource(1, AliasSourceKind.File, @"C:\Gone.txt"), "C:\\Gone.txt was not found.")], [], "2610", _output);

		Assert.Null(result.FilePath);
		Assert.False(File.Exists(VnasAliasPath));
		Assert.EndsWith("The one an earlier run wrote was deleted, so it cannot be uploaded by mistake.", result.Messages[^1].Text, StringComparison.Ordinal);
	}

	[Fact]
	public void an_earlier_file_that_cannot_be_deleted_is_not_to_be_uploaded()
	{
		VnasAliasFileWriter.Write([Read(1, "a.txt", ".a .ECHO a")], [], "2610", _output);

		using (File.Open(VnasAliasPath, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			VnasAliasResult result = VnasAliasFileWriter.Write([], [], "2610", _output);

			Assert.Contains("The one an earlier run wrote could not be deleted (", result.Messages[^1].Text, StringComparison.Ordinal);
			Assert.EndsWith("): do not upload it.", result.Messages[^1].Text, StringComparison.Ordinal);
		}
	}

	/// <summary>The files are named in merge order, so the custom file - the copy CRC uses - comes last.</summary>
	[Fact]
	public void a_command_from_a_custom_file_in_another_file_is_reported_once_with_the_files_it_is_in()
	{
		VnasAliasResult result = VnasAliasFileWriter.Write(
			[Read(1, "ZOB-Alias.txt", ".CLE .ECHO mine\r\n.cle .ECHO mine again\r\n.only .ECHO mine")],
			[FeBuddyFile("Navaids.txt", ".CLE .FF CLE"), FeBuddyFile("Airports.txt", ".aptCLE .FF CLE")],
			"2610",
			_output);

		Assert.Equal(1, result.DuplicateCommandCount);

		ServiceMessage notice = Assert.Single(result.Messages);
		Assert.True(notice.IsAdvisory);
		Assert.Equal(LogLevel.Info, notice.Level);
		Assert.Equal(
			"1 alias command(s) from your custom alias files are also in another file merged into vNAS_Alias.txt: " +
			".CLE (Navaids.txt, ZOB-Alias.txt). CRC uses the last copy of a command - the one from the last file named - " +
			"and your custom alias files come after FE-Buddy's, so a command of yours replaces FE-Buddy's. " +
			"To use FE-Buddy's instead, remove yours.",
			notice.Text);
	}

	[Fact]
	public void a_command_in_two_custom_files_is_reported()
	{
		VnasAliasResult result = VnasAliasFileWriter.Write(
			[Read(1, "a.txt", ".same .ECHO a"), Read(2, "b.txt", ".same .ECHO b")], [], "2610", _output);

		Assert.Equal(1, result.DuplicateCommandCount);
		Assert.Contains(".same (a.txt, b.txt)", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	/// <summary>
	/// A command only FE-Buddy's own files share - ORF NUTIY, in NASR as both a DP and a STAR - is
	/// already in Duplicate_Alias_Commands.txt, so the merge does not warn about it again.
	/// </summary>
	[Fact]
	public void a_command_only_fe_buddy_files_share_is_not_reported()
	{
		VnasAliasResult result = VnasAliasFileWriter.Write(
			[Read(1, "ZOB-Alias.txt", ".mine .ECHO mine")],
			[FeBuddyFile("Departures.txt", ".orfNUTIYf .FF A"), FeBuddyFile("Arrivals.txt", ".orfNUTIYf .FF B")],
			"2610",
			_output);

		Assert.Equal(0, result.DuplicateCommandCount);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void a_long_list_of_duplicates_is_cut_short()
	{
		string commands = string.Join("\r\n", Enumerable.Range(1, 12).Select(i => $".c{i} .ECHO {i}"));

		VnasAliasResult result = VnasAliasFileWriter.Write(
			[Read(1, "a.txt", commands)], [FeBuddyFile("Airways.txt", commands)], "2610", _output);

		Assert.Equal(12, result.DuplicateCommandCount);
		string text = Assert.Single(result.Messages).Text;
		Assert.Contains(".c10 (Airways.txt, a.txt), ...", text, StringComparison.Ordinal);
		Assert.DoesNotContain(".c11", text, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("", 0)]
	[InlineData(".", 0)]
	[InlineData(".a", 1)]
	[InlineData("  .a .ECHO\r\n# .b\r\n; .c\r\n\t.d x\n.e", 3)]
	public void count_commands_counts_lines_starting_with_a_dot(string text, int expected) =>
		Assert.Equal(expected, VnasAliasFileWriter.CountCommands(text));

	[Fact]
	public void bad_arguments_are_refused()
	{
		Assert.Throws<ArgumentNullException>(() => VnasAliasFileWriter.Write(null!, [], "2610", _output));
		Assert.Throws<ArgumentNullException>(() => VnasAliasFileWriter.Write([], null!, "2610", _output));
		Assert.Throws<ArgumentException>(() => VnasAliasFileWriter.Write([], [], " ", _output));
		Assert.Throws<ArgumentException>(() => VnasAliasFileWriter.Write([], [], "2610", ""));
		Assert.Throws<ArgumentNullException>(() => VnasAliasFileWriter.CountCommands(null!));
	}

	private static AliasSourceLoad Read(int number, string fileName, string text) =>
		AliasSourceLoad.Read(new AliasSource(number, AliasSourceKind.File, Path.Combine(@"C:\Custom", fileName)), text);

	private string FeBuddyFile(string name, string text)
	{
		string path = Path.Combine(_output, "Aliases", name);
		File.WriteAllText(path, text);
		return path;
	}

	/// <summary>The lines as the writer ends them: each followed by a line break.</summary>
	private static string Lines(params string[] lines) => string.Concat(lines.Select(line => line + Environment.NewLine));
}
