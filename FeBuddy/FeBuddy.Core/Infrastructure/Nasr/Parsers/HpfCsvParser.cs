using static FeBuddy.Core.Infrastructure.Nasr.Models.HpfCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>HPF</c> CSV files (holding patterns), one file per method.
/// </summary>
public class HpfCsvParser
{
	/// <summary>Reads <c>HPF_BASE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="HpfCsvDataCollection.HpfBase"/> filled in.</returns>
	public HpfCsvDataCollection ParseHpfBase(string filePath)
	{
		var result = new HpfCsvDataCollection
		{
			HpfBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new HpfBase
				{
					EffDate = fields["EFF_DATE"],
					HpName = fields["HP_NAME"],
					HpNo = NasrCsvReader.ParseInt(fields["HP_NO"]),
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					FixId = fields["FIX_ID"],
					IcaoRegionCode = fields["ICAO_REGION_CODE"],
					NavId = fields["NAV_ID"],
					NavType = fields["NAV_TYPE"],
					HoldDirection = fields["HOLD_DIRECTION"],
					HoldDegOrCrs = fields["HOLD_DEG_OR_CRS"],
					Azimuth = fields["AZIMUTH"],
					CourseInboundDeg = NasrCsvReader.ParseNullableInt(fields["COURSE_INBOUND_DEG"]),
					TurnDirection = fields["TURN_DIRECTION"],
					LegLengthDist = NasrCsvReader.ParseNullableInt(fields["LEG_LENGTH_DIST"]),
				})
		};

		return result;
	}

	/// <summary>Reads <c>HPF_CHRT.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="HpfCsvDataCollection.HpfChrt"/> filled in.</returns>
	public HpfCsvDataCollection ParseHpfChrt(string filePath)
	{
		var result = new HpfCsvDataCollection
		{
			HpfChrt = NasrCsvReader.ProcessLines(
				filePath,
				fields => new HpfChrt
				{
					EffDate = fields["EFF_DATE"],
					HpName = fields["HP_NAME"],
					HpNo = NasrCsvReader.ParseInt(fields["HP_NO"]),
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					ChartingTypeDesc = fields["CHARTING_TYPE_DESC"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>HPF_RMK.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="HpfCsvDataCollection.HpfRmk"/> filled in.</returns>
	public HpfCsvDataCollection ParseHpfRmk(string filePath)
	{
		var result = new HpfCsvDataCollection
		{
			HpfRmk = NasrCsvReader.ProcessLines(
				filePath,
				fields => new HpfRmk
				{
					EffDate = fields["EFF_DATE"],
					HpName = fields["HP_NAME"],
					HpNo = NasrCsvReader.ParseInt(fields["HP_NO"]),
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					TabName = fields["TAB_NAME"],
					RefColName = fields["REF_COL_NAME"],
					RefColSeqNo = NasrCsvReader.ParseInt(fields["REF_COL_SEQ_NO"]),
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>HPF_SPD_ALT.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="HpfCsvDataCollection.HpfSpdAlt"/> filled in.</returns>
	public HpfCsvDataCollection ParseHpfSpdAlt(string filePath)
	{
		var result = new HpfCsvDataCollection
		{
			HpfSpdAlt = NasrCsvReader.ProcessLines(
				filePath,
				fields => new HpfSpdAlt
				{
					EffDate = fields["EFF_DATE"],
					HpName = fields["HP_NAME"],
					HpNo = NasrCsvReader.ParseInt(fields["HP_NO"]),
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					SpeedRange = fields["SPEED_RANGE"],
					Altitude = fields["ALTITUDE"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>HPF</c> CSV files, one list per file.
/// </summary>
public class HpfCsvDataCollection
{
	/// <summary>The rows of <c>HPF_BASE.csv</c>.</summary>
	public List<HpfBase> HpfBase { get; set; } = [];
	/// <summary>The rows of <c>HPF_CHRT.csv</c>.</summary>
	public List<HpfChrt> HpfChrt { get; set; } = [];
	/// <summary>The rows of <c>HPF_RMK.csv</c>.</summary>
	public List<HpfRmk> HpfRmk { get; set; } = [];
	/// <summary>The rows of <c>HPF_SPD_ALT.csv</c>.</summary>
	public List<HpfSpdAlt> HpfSpdAlt { get; set; } = [];
}