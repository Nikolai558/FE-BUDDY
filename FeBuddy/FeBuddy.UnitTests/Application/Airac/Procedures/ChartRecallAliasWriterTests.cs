using System.Text;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="ChartRecallAliasWriter"/>: the file lands in the <c>Aliases</c> folder, even
/// when marked for vNAS, one line per command in order, UTF-8 without a BOM,
/// and nothing is written when there is no command.
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

	private ProcedureSettings Settings(bool uploadToVnas = false) => new()
	{
		OutputDirectory = _outputDirectory,
		Vnas = uploadToVnas ? new VnasFileChoices([ProcedureOutputFiles.Alias], []) : VnasFileChoices.None,
	};

	private static readonly ChartRecallAliasLine[] TwoLines =
	[
		new("DTW", "IAP", ".dtwI22Lc", "https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF", "DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L"),
		new("DTW", "IAP", ".dtwL22Lc", "https://aeronav.faa.gov/d-tpp/2609/00058IL22L.PDF", "DETROIT METRO WAYNE COUNTY-ILS OR LOC RWY 22L"),
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
	public void a_file_marked_for_vnas_still_goes_in_the_aliases_folder()
	{
		ChartRecallAliasWriteResult result = ChartRecallAliasWriter.Generate(TwoLines, Settings(uploadToVnas: true));

		Assert.Equal(Path.Combine(_outputDirectory, "Aliases", "Faa_Chart_Recall.txt"), result.FilePath);
		Assert.False(Directory.Exists(Path.Combine(_outputDirectory, "Upload_to_vNAS")));
	}

	[Fact]
	public void each_line_is_written_in_order_as_utf8_without_a_bom()
	{
		ChartRecallAliasWriteResult result = ChartRecallAliasWriter.Generate(TwoLines, Settings());

		byte[] bytes = File.ReadAllBytes(result.FilePath!);
		Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..]);
		Assert.Equal(TwoLines.Select(line => line.Text), File.ReadAllLines(result.FilePath!, Encoding.UTF8));
	}

	[Fact]
	public void no_lines_write_no_file()
	{
		ChartRecallAliasWriteResult result = ChartRecallAliasWriter.Generate([], Settings());

		Assert.Null(result.FilePath);
		Assert.Equal(0, result.CommandCount);
		Assert.False(Directory.Exists(_outputDirectory));
	}
}
