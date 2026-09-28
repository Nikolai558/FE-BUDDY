using static FeBuddy.Core.Infrastructure.Nasr.Models.RdrCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>RDR</c> CSV files (radar facilities), one file per method.
/// </summary>
public class RdrCsvParser
{
	/// <summary>Reads <c>RDR.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="RdrCsvDataCollection.Rdr"/> filled in.</returns>
	public RdrCsvDataCollection ParseRdr(string filePath)
	{
		var result = new RdrCsvDataCollection
		{
			Rdr = NasrCsvReader.ProcessLines(
				filePath,
				fields => new Rdr
				{
					EffDate = fields["EFF_DATE"],
					FacilityId = fields["FACILITY_ID"],
					FacilityType = fields["FACILITY_TYPE"],
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					RadarType = fields["RADAR_TYPE"],
					RadarNo = NasrCsvReader.ParseInt(fields["RADAR_NO"]),
					RadarHrs = fields["RADAR_HRS"],
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>RDR</c> CSV files, one list per file.
/// </summary>
public class RdrCsvDataCollection
{
	/// <summary>The rows of <c>RDR.csv</c>.</summary>
	public List<Rdr> Rdr { get; set; } = [];
}