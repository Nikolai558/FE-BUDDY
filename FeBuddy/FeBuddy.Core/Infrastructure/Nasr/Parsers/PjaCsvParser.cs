using static FeBuddy.Core.Infrastructure.Nasr.Models.PjaCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>PJA</c> CSV files (parachute jump areas), one file per method.
/// </summary>
public class PjaCsvParser
{
	/// <summary>Reads <c>PJA_BASE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="PjaCsvDataCollection.PjaBase"/> filled in.</returns>
	public PjaCsvDataCollection ParsePjaBase(string filePath)
	{
		var result = new PjaCsvDataCollection
		{
			PjaBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new PjaBase
				{
					EffDate = fields["EFF_DATE"],
					PjaId = fields["PJA_ID"],
					NavId = fields["NAV_ID"],
					NavType = fields["NAV_TYPE"],
					Radial = NasrCsvReader.ParseNullableDouble(fields["RADIAL"]),
					Distance = NasrCsvReader.ParseNullableDouble(fields["DISTANCE"]),
					NavaidName = fields["NAVAID_NAME"],
					StateCode = fields["STATE_CODE"],
					City = fields["CITY"],
					Latitude = fields["LATITUDE"],
					LatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
					Longitude = fields["LONGITUDE"],
					LongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
					ArptId = fields["ARPT_ID"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					DropZoneName = fields["DROP_ZONE_NAME"],
					MaxAltitude = NasrCsvReader.ParseNullableInt(fields["MAX_ALTITUDE"]),
					MaxAltitudeTypeCode = fields["MAX_ALTITUDE_TYPE_CODE"],
					PjaRadius = NasrCsvReader.ParseNullableDouble(fields["PJA_RADIUS"]),
					ChartRequestFlag = fields["CHART_REQUEST_FLAG"],
					PublishCriteria = fields["PUBLISH_CRITERIA"],
					Description = fields["DESCRIPTION"],
					TimeOfUse = fields["TIME_OF_USE"],
					FssId = fields["FSS_ID"],
					FssName = fields["FSS_NAME"],
					PjaUse = fields["PJA_USE"],
					Volume = fields["VOLUME"],
					PjaUser = fields["PJA_USER"],
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>PJA_CON.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="PjaCsvDataCollection.PjaCon"/> filled in.</returns>
	public PjaCsvDataCollection ParsePjaCon(string filePath)
	{
		var result = new PjaCsvDataCollection
		{
			PjaCon = NasrCsvReader.ProcessLines(
				filePath,
				fields => new PjaCon
				{
					EffDate = fields["EFF_DATE"],
					PjaId = fields["PJA_ID"],
					FacId = fields["FAC_ID"],
					FacName = fields["FAC_NAME"],
					LocId = fields["LOC_ID"],
					CommercialFreq = NasrCsvReader.ParseDouble(fields["COMMERCIAL_FREQ"]),
					CommercialChartFlag = fields["COMMERCIAL_CHART_FLAG"],
					MilFreq = NasrCsvReader.ParseNullableDouble(fields["MIL_FREQ"]),
					MilChartFlag = fields["MIL_CHART_FLAG"],
					Sector = fields["SECTOR"],
					ContactFreqAltitude = fields["CONTACT_FREQ_ALTITUDE"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>PJA</c> CSV files, one list per file.
/// </summary>
public class PjaCsvDataCollection
{
	/// <summary>The rows of <c>PJA_BASE.csv</c>.</summary>
	public List<PjaBase> PjaBase { get; set; } = [];
	/// <summary>The rows of <c>PJA_CON.csv</c>.</summary>
	public List<PjaCon> PjaCon { get; set; } = [];
}