using static FeBuddy.Core.Infrastructure.Nasr.Models.FixCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>FIX</c> CSV files (fixes and reporting points), one file per method.
/// </summary>
public class FixCsvParser
{
	/// <summary>Reads <c>FIX_BASE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="FixCsvDataCollection.FixBase"/> filled in.</returns>
	public FixCsvDataCollection ParseFixBase(string filePath)
	{
		var result = new FixCsvDataCollection
		{
			FixBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new FixBase
				{
					EffDate = fields["EFF_DATE"],
					FixId = fields["FIX_ID"],
					IcaoRegionCode = fields["ICAO_REGION_CODE"],
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
					FixIdOld = fields["FIX_ID_OLD"],
					ChartingRemark = fields["CHARTING_REMARK"],
					FixUseCode = fields["FIX_USE_CODE"],
					ArtccIdHigh = fields["ARTCC_ID_HIGH"],
					ArtccIdLow = fields["ARTCC_ID_LOW"],
					PitchFlag = fields["PITCH_FLAG"],
					CatchFlag = fields["CATCH_FLAG"],
					SuaAtcaaFlag = fields["SUA_ATCAA_FLAG"],
					MinRecepAlt = NasrCsvReader.ParseNullableInt(fields["MIN_RECEP_ALT"]),
					Compulsory = fields["COMPULSORY"],
					Charts = fields["CHARTS"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>FIX_CHRT.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="FixCsvDataCollection.FixChrt"/> filled in.</returns>
	public FixCsvDataCollection ParseFixChrt(string filePath)
	{
		var result = new FixCsvDataCollection
		{
			FixChrt = NasrCsvReader.ProcessLines(
				filePath,
				fields => new FixChrt
				{
					EffDate = fields["EFF_DATE"],
					FixId = fields["FIX_ID"],
					IcaoRegionCode = fields["ICAO_REGION_CODE"],
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					ChartingTypeDesc = fields["CHARTING_TYPE_DESC"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>FIX_NAV.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="FixCsvDataCollection.FixNav"/> filled in.</returns>
	public FixCsvDataCollection ParseFixNav(string filePath)
	{
		var result = new FixCsvDataCollection
		{
			FixNav = NasrCsvReader.ProcessLines(
				filePath,
				fields => new FixNav
				{
					EffDate = fields["EFF_DATE"],
					FixId = fields["FIX_ID"],
					IcaoRegionCode = fields["ICAO_REGION_CODE"],
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					NavId = fields["NAV_ID"],
					NavType = fields["NAV_TYPE"],
					Bearing = NasrCsvReader.ParseNullableDouble(fields["BEARING"]),
					Distance = NasrCsvReader.ParseNullableDouble(fields["DISTANCE"]),
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>FIX</c> CSV files, one list per file.
/// </summary>
public class FixCsvDataCollection
{
	/// <summary>The rows of <c>FIX_BASE.csv</c>.</summary>
	public List<FixBase> FixBase { get; set; } = [];
	/// <summary>The rows of <c>FIX_CHRT.csv</c>.</summary>
	public List<FixChrt> FixChrt { get; set; } = [];
	/// <summary>The rows of <c>FIX_NAV.csv</c>.</summary>
	public List<FixNav> FixNav { get; set; } = [];
}