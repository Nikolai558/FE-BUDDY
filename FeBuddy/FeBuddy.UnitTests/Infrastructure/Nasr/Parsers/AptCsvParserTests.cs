using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.Nasr.Parsers;

/// <summary>
/// Regression coverage for the exact bug report: some NASR AIRAC cycles ship an
/// <c>APT_RWY.csv</c> without the ACN-PCN pavement classification columns (the FAA has added
/// and removed them between cycles), which previously threw
/// <see cref="KeyNotFoundException"/> and aborted the entire run.
/// </summary>
public class AptCsvParserTests : IDisposable
{
	private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"FeBuddyTests_AptRwy_{Guid.NewGuid():N}.csv");

	public void Dispose()
	{
		if (File.Exists(_tempFile))
		{
			File.Delete(_tempFile);
		}
	}

	[Fact]
	public void parse_apt_rwy_does_not_throw_when_pavement_classification_columns_are_absent()
	{
		// A header row with every column ParseAptRwy reads EXCEPT the six pavement
		// classification columns (PAVEMENT_CLASSIFICATION, PCN_PCR_NUMBER,
		// PAVEMENT_TYPE_CODE, SUBGRADE_STRENGTH_CODE, TIRE_PRES_CODE, DTRM_METHOD_CODE),
		// matching the real-world cycle that reproduced the bug report.
		string header = string.Join(",",
			"EFF_DATE", "SITE_NO", "SITE_TYPE_CODE", "STATE_CODE", "ARPT_ID", "CITY",
			"COUNTRY_CODE", "RWY_ID", "RWY_LEN", "RWY_WIDTH", "SURFACE_TYPE_CODE", "COND",
			"TREATMENT_CODE", "RWY_LGT_CODE", "RWY_LEN_SOURCE", "LENGTH_SOURCE_DATE",
			"GROSS_WT_SW", "GROSS_WT_DW", "GROSS_WT_DTW", "GROSS_WT_DDTW");

		string row = string.Join(",",
			"2026/09/03", "01975.12", "A", "OH", "DUB", "COLUMBUS", "US", "10L/28R", "5500",
			"100", "CONC", "G", "", "MED", "GPS", "2026/09/03", "30000", "60000", "", "");

		File.WriteAllText(_tempFile, header + Environment.NewLine + row + Environment.NewLine);

		AptCsvParser parser = new();

		AptCsvDataCollection result = parser.ParseAptRwy(_tempFile);

		AptCsvDataModel.AptRwy runway = Assert.Single(result.AptRwy);
		Assert.Equal("DUB", runway.ArptId);
		Assert.Equal(5500, runway.RwyLen);
		Assert.Equal(string.Empty, runway.PavementClassification);
		Assert.Null(runway.PcnPcrNumber);
		Assert.Equal(string.Empty, runway.PavementTypeCode);
		Assert.Equal(string.Empty, runway.SubgradeStrengthCode);
		Assert.Equal(string.Empty, runway.TirePresCode);
		Assert.Equal(string.Empty, runway.DtrmMethodCode);
	}

	[Fact]
	public void parse_apt_rwy_still_reads_pavement_classification_when_present()
	{
		string header = string.Join(",",
			"EFF_DATE", "SITE_NO", "SITE_TYPE_CODE", "STATE_CODE", "ARPT_ID", "CITY",
			"COUNTRY_CODE", "RWY_ID", "RWY_LEN", "RWY_WIDTH", "SURFACE_TYPE_CODE", "COND",
			"TREATMENT_CODE", "PAVEMENT_CLASSIFICATION", "PCN_PCR_NUMBER", "PAVEMENT_TYPE_CODE",
			"SUBGRADE_STRENGTH_CODE", "TIRE_PRES_CODE", "DTRM_METHOD_CODE", "RWY_LGT_CODE",
			"RWY_LEN_SOURCE", "LENGTH_SOURCE_DATE", "GROSS_WT_SW", "GROSS_WT_DW", "GROSS_WT_DTW", "GROSS_WT_DDTW");

		string row = string.Join(",",
			"2026/09/03", "01975.12", "A", "OH", "DUB", "COLUMBUS", "US", "10L/28R", "5500",
			"100", "CONC", "G", "", "80", "45", "R", "B", "T", "U", "MED", "GPS", "2026/09/03",
			"30000", "60000", "", "");

		File.WriteAllText(_tempFile, header + Environment.NewLine + row + Environment.NewLine);

		AptCsvParser parser = new();

		AptCsvDataCollection result = parser.ParseAptRwy(_tempFile);

		AptCsvDataModel.AptRwy runway = Assert.Single(result.AptRwy);
		Assert.Equal("80", runway.PavementClassification);
		Assert.Equal(45, runway.PcnPcrNumber);
	}
}
