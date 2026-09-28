using System.Text;

using FeBuddy.Core.Application.Airac.VnasAlias;
using FeBuddy.Core.Application.Airac.VnasAlias.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Airac.VnasAlias;

/// <summary>
/// Covers <see cref="VnasAliasFileWriter"/>: <c>vNAS_Alias.txt</c> is the custom alias files, then
/// FE-Buddy's marked alias files under one marker line; a <c>.FeUseOnly</c> line stays first; an old
/// <c>vNAS_Alias.txt</c> used as a custom file loses its FE-Buddy section; commands in more than one
/// file are reported; and a custom file that could not be read is left out with a warning.
/// </summary>
public sealed class VnasAliasFileWriterTests : IDisposable
{
	private readonly string _output = Path.Combine(Path.GetTempPath(), "FeBuddyTests_VnasAlias_" + Guid.NewGuid().ToString("N"));

	public VnasAliasFileWriterTests() => Directory.CreateDirectory(Path.Combine(_output, "Aliases"));

	private string VnasAliasPath => Path.Combine(_output, "Upload_to_vNAS", "vNAS_Alias.txt");

	public void Dispose() => Directory.Delete(_output, recursive: true);

	[Fact]
	public void custom_files_come_first_then_fe_buddy_files_under_the_marker()
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
				"# ZOB",
				"",
				".dtwdv .ECHO DTW",
				"",
				".extra .ECHO extra",
				"",
				"; ===== FE-Buddy aliases (AIRAC 2610) start here. FE-Buddy replaces everything below this line every cycle. =====",
				"; ----- Airways.txt -----",
				".J60F .FF A B C",
				"; ----- Telephony.txt -----",
				".idAVA .MSG AVIANCA",
				".idAAL .MSG AMERICAN"),
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

	[Fact]
	public void an_old_vnas_alias_file_used_as_a_custom_file_loses_its_fe_buddy_section()
	{
		string oldUpload = Lines(
			".mine .ECHO mine",
			"",
			"; ===== FE-Buddy aliases (AIRAC 2609) start here. FE-Buddy replaces everything below this line every cycle. =====",
			"; ----- Airways.txt -----",
			".J60F .FF OLD");

		VnasAliasResult result = VnasAliasFileWriter.Write(
			[Read(1, "ZOB-Alias.txt", oldUpload)], [FeBuddyFile("Airways.txt", ".J60F .FF NEW")], "2610", _output);

		string written = File.ReadAllText(VnasAliasPath);
		Assert.DoesNotContain("OLD", written, StringComparison.Ordinal);
		Assert.DoesNotContain("2609", written, StringComparison.Ordinal);
		Assert.StartsWith(Lines(".mine .ECHO mine", "") + "; ===== FE-Buddy aliases (AIRAC 2610)", written, StringComparison.Ordinal);
		Assert.Equal(1, result.CustomCommandCount);
		Assert.Equal(0, result.DuplicateCommandCount);

		ServiceMessage notice = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, notice.Level);
		Assert.Contains("custom alias file 1 (ZOB-Alias.txt) ends with FE-Buddy aliases", notice.Text, StringComparison.Ordinal);
	}

	[Fact]
	public void fe_buddy_files_alone_have_just_the_fe_buddy_section()
	{
		VnasAliasResult result = VnasAliasFileWriter.Write([], [FeBuddyFile("Navaids.txt", ".CLE .FF CLE")], "2610", _output);

		Assert.Equal(
			Lines(
				"; ===== FE-Buddy aliases (AIRAC 2610) start here. FE-Buddy replaces everything below this line every cycle. =====",
				"; ----- Navaids.txt -----",
				".CLE .FF CLE"),
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
		Assert.StartsWith(Lines(".b .ECHO b", ""), File.ReadAllText(VnasAliasPath), StringComparison.Ordinal);

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

	[Fact]
	public void a_command_from_a_custom_file_in_another_file_is_reported_once_with_the_files_it_is_in()
	{
		VnasAliasResult result = VnasAliasFileWriter.Write(
			[Read(1, "ZOB-Alias.txt", ".CLE .ECHO mine\r\n.cle .ECHO mine again\r\n.only .ECHO mine")],
			[FeBuddyFile("Navaids.txt", ".CLE .FF CLE"), FeBuddyFile("Airports.txt", ".aptCLE .FF CLE")],
			"2610",
			_output);

		Assert.Equal(1, result.DuplicateCommandCount);

		ServiceMessage warning = Assert.Single(result.Messages);
		Assert.True(warning.IsAdvisory);
		Assert.Equal(
			"1 alias command(s) from your custom alias files are also in another file merged into vNAS_Alias.txt, so CRC can only run " +
			"one of each: .CLE (ZOB-Alias.txt, Navaids.txt). Remove the extra copies from your custom alias files, or untick the FE-Buddy file.",
			warning.Text);
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
		Assert.Contains(".c10 (a.txt, Airways.txt), ...", text, StringComparison.Ordinal);
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
