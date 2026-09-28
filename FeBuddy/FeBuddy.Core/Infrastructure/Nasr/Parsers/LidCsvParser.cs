using static FeBuddy.Core.Infrastructure.Nasr.Models.LidCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>LID</c> CSV files (location identifiers), one file per method.
/// </summary>
public class LidCsvParser
{
	/// <summary>Reads <c>LID.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="LidCsvDataCollection.Lid"/> filled in.</returns>
	public LidCsvDataCollection ParseLid(string filePath)
	{
		var result = new LidCsvDataCollection
		{
			Lid = NasrCsvReader.ProcessLines(
				filePath,
				fields => new Lid
				{
					EffDate = fields["EFF_DATE"],
					CountryCode = fields["COUNTRY_CODE"],
					LocId = fields["LOC_ID"],
					RegionCode = fields["REGION_CODE"],
					State = fields["STATE"],
					City = fields["CITY"],
					LidGroup = fields["LID_GROUP"],
					FacType = fields["FAC_TYPE"],
					FacName = fields["FAC_NAME"],
					RespArtccId = fields["RESP_ARTCC_ID"],
					ArtccComputerId = fields["ARTCC_COMPUTER_ID"],
					FssId = fields["FSS_ID"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>LID</c> CSV files, one list per file.
/// </summary>
public class LidCsvDataCollection
{
	/// <summary>The rows of <c>LID.csv</c>.</summary>
	public List<Lid> Lid { get; set; } = [];
}