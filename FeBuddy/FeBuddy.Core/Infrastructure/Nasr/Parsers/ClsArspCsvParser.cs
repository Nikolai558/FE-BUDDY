using static FeBuddy.Core.Infrastructure.Nasr.Models.ClsArspCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>CLS_ARSP</c> CSV files (class airspace at airports), one file per method.
/// </summary>
public class ClsArspCsvParser
{
	/// <summary>Reads <c>CLS_ARSP.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="ClsArspCsvDataCollection.ClsArsp"/> filled in.</returns>
	public ClsArspCsvDataCollection ParseClsArsp(string filePath)
	{
		var result = new ClsArspCsvDataCollection
		{
			ClsArsp = NasrCsvReader.ProcessLines(
				filePath,
				fields => new ClsArsp
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					ClassBAirspace = fields["CLASS_B_AIRSPACE"],
					ClassCAirspace = fields["CLASS_C_AIRSPACE"],
					ClassDAirspace = fields["CLASS_D_AIRSPACE"],
					ClassEAirspace = fields["CLASS_E_AIRSPACE"],
					AirspaceHrs = fields["AIRSPACE_HRS"],
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>CLS_ARSP</c> CSV files, one list per file.
/// </summary>
public class ClsArspCsvDataCollection
{
	/// <summary>The rows of <c>CLS_ARSP.csv</c>.</summary>
	public List<ClsArsp> ClsArsp { get; set; } = [];
}