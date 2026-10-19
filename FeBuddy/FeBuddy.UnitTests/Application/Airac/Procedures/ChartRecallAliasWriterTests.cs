using System.Text;

using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ChartRecallAliasWriter"/>: the file lands in the <c>Aliases</c> folder, the help
/// command first (under its name, or left out), then one line per command in order, UTF-8 without a
/// BOM, and nothing is written when there is no chart command.
/// </summary>
public sealed class ChartRecallAliasWriterTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_ChartRecallAliasWriter_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	private ProcedureSettings Settings() => new()
	{
		OutputDirectory = _outputDirectory,
	};

	private static readonly ChartRecallAliasLine[] TwoLines =
	[
		new(".dtwI22Lc", "https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF", "DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L"),
		new(".dtwL22Lc", "https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF", "DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L"),
	];

	[Fact]
	public void generate_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => ChartRecallAliasWriter.Generate(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => ChartRecallAliasWriter.Generate(TwoLines, null!));
	}

	[Fact]
	public void the_file_is_faa_chart_recall_txt_in_the_aliases_folder()
	{
		ChartRecallAliasWriteResult result = ChartRecallAliasWriter.Generate(TwoLines, Settings());

		Assert.Equal(Path.Combine(_outputDirectory, "Aliases", "Faa_Chart_Recall.txt"), result.FilePath);
		Assert.Equal(2, result.CommandCount);
	}

	[Fact]
	public void each_line_is_written_in_order_after_the_help_command_as_utf8_without_a_bom()
	{
		ChartRecallAliasWriteResult result = ChartRecallAliasWriter.Generate(TwoLines, Settings());

		byte[] bytes = File.ReadAllBytes(result.FilePath!);
		Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..]);
		Assert.Equal(
			[ChartRecallHelpCommand.Line("helpCHARTRECALL"), .. TwoLines.Select(line => line.Text)],
			File.ReadAllLines(result.FilePath!, Encoding.UTF8));

		// The help command isn't a chart command.
		Assert.Equal(2, result.CommandCount);
	}

	[Fact]
	public void a_renamed_help_command_is_written_under_its_new_name()
	{
		ChartRecallAliasWriteResult result = ChartRecallAliasWriter.Generate(TwoLines, Settings() with { HelpCommand = "helpCR" });

		Assert.StartsWith(".helpCR .note ", File.ReadLines(result.FilePath!).First(), StringComparison.Ordinal);
	}

	[Fact]
	public void without_the_help_command_only_the_chart_commands_are_written()
	{
		ChartRecallAliasWriteResult result = ChartRecallAliasWriter.Generate(TwoLines, Settings() with { HelpCommand = null });

		Assert.Equal(TwoLines.Select(line => line.Text), File.ReadAllLines(result.FilePath!, Encoding.UTF8));
	}

	[Fact]
	public void no_lines_write_no_file_even_with_the_help_command()
	{
		ChartRecallAliasWriteResult result = ChartRecallAliasWriter.Generate([], Settings());

		Assert.Null(result.FilePath);
		Assert.Equal(0, result.CommandCount);
		Assert.False(Directory.Exists(_outputDirectory));
	}
}
