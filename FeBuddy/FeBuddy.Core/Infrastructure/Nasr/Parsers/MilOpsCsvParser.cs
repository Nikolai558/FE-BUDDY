using static FeBuddy.Core.Infrastructure.Nasr.Models.MilOpsCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>MIL_OPS</c> CSV files (military operations at airports), one file per method.
/// </summary>
public class MilOpsCsvParser
{
	/// <summary>Reads <c>MIL_OPS.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="MilOpsCsvDataCollection.MilOps"/> filled in.</returns>
	public MilOpsCsvDataCollection ParseMilOps(string filePath)
	{
		var result = new MilOpsCsvDataCollection
		{
			MilOps = NasrCsvReader.ProcessLines(
				filePath,
				fields => new MilOps
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					MilOpsOperCode = fields["MIL_OPS_OPER_CODE"],
					MilOpsCall = fields["MIL_OPS_CALL"],
					MilOpsHrs = fields["MIL_OPS_HRS"],
					AmcpHrs = fields["AMCP_HRS"],
					PmsvHrs = fields["PMSV_HRS"],
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>MIL_OPS</c> CSV files, one list per file.
/// </summary>
public class MilOpsCsvDataCollection
{
	/// <summary>The rows of <c>MIL_OPS.csv</c>.</summary>
	public List<MilOps> MilOps { get; set; } = [];
}