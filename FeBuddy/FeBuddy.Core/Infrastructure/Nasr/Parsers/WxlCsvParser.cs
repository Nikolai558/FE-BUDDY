using static FeBuddy.Core.Infrastructure.Nasr.Models.WxlCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>WXL</c> CSV files (weather reporting locations), one file per method.
/// </summary>
public class WxlCsvParser
{
	/// <summary>Reads <c>WXL_BASE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="WxlCsvDataCollection.WxlBase"/> filled in.</returns>
	public WxlCsvDataCollection ParseWxlBase(string filePath)
	{
		var result = new WxlCsvDataCollection
		{
			WxlBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new WxlBase
				{
					EffDate = fields["EFF_DATE"],
					WeaId = fields["WEA_ID"],
					City = fields["CITY"],
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					LatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
					LatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
					LatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
					LatHemis = fields["LAT_HEMIS"],
					LatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
					LongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
					LongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
					LongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
					LongHemis = fields["LONG_HEMIS"],
					LongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
					Elev = NasrCsvReader.ParseInt(fields["ELEV"]),
					SurveyMethodCode = fields["SURVEY_METHOD_CODE"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>WXL_SVC.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="WxlCsvDataCollection.WxlSvc"/> filled in.</returns>
	public WxlCsvDataCollection ParseWxlSvc(string filePath)
	{
		var result = new WxlCsvDataCollection
		{
			WxlSvc = NasrCsvReader.ProcessLines(
				filePath,
				fields => new WxlSvc
				{
					EffDate = fields["EFF_DATE"],
					WeaId = fields["WEA_ID"],
					City = fields["CITY"],
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					WeaSvcTypeCode = fields["WEA_SVC_TYPE_CODE"],
					WeaAffectArea = fields["WEA_AFFECT_AREA"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>WXL</c> CSV files, one list per file.
/// </summary>
public class WxlCsvDataCollection
{
	/// <summary>The rows of <c>WXL_BASE.csv</c>.</summary>
	public List<WxlBase> WxlBase { get; set; } = [];
	/// <summary>The rows of <c>WXL_SVC.csv</c>.</summary>
	public List<WxlSvc> WxlSvc { get; set; } = [];
}