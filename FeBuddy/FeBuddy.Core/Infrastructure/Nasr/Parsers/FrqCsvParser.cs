using static FeBuddy.Core.Infrastructure.Nasr.Models.FrqCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>FRQ</c> CSV files (frequencies), one file per method.
/// </summary>
public class FrqCsvParser
{
	/// <summary>Reads <c>FRQ.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="FrqCsvDataCollection.Frq"/> filled in.</returns>
	public FrqCsvDataCollection ParseFrq(string filePath)
	{
		var result = new FrqCsvDataCollection
		{
			Frq = NasrCsvReader.ProcessLines(
				filePath,
				fields => new Frq
				{
					EffDate = fields["EFF_DATE"],
					Facility = fields["FACILITY"],
					FacName = fields["FAC_NAME"],
					FacilityType = fields["FACILITY_TYPE"],
					ArtccOrFssId = fields["ARTCC_OR_FSS_ID"],
					Cpdlc = fields["CPDLC"],
					TowerHrs = fields["TOWER_HRS"],
					ServicedFacility = fields["SERVICED_FACILITY"],
					ServicedFacName = fields["SERVICED_FAC_NAME"],
					ServicedSiteType = fields["SERVICED_SITE_TYPE"],
					LatDecimal = NasrCsvReader.ParseNullableDouble(fields["LAT_DECIMAL"]),
					LongDecimal = NasrCsvReader.ParseNullableDouble(fields["LONG_DECIMAL"]),
					ServicedCity = fields["SERVICED_CITY"],
					ServicedState = fields["SERVICED_STATE"],
					ServicedCountry = fields["SERVICED_COUNTRY"],
					TowerOrCommCall = fields["TOWER_OR_COMM_CALL"],
					PrimaryApproachRadioCall = fields["PRIMARY_APPROACH_RADIO_CALL"],
					Freq = fields["FREQ"],
					Sectorization = fields["SECTORIZATION"],
					FreqUse = fields["FREQ_USE"],
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>FRQ</c> CSV files, one list per file.
/// </summary>
public class FrqCsvDataCollection
{
	/// <summary>The rows of <c>FRQ.csv</c>.</summary>
	public List<Frq> Frq { get; set; } = [];
}