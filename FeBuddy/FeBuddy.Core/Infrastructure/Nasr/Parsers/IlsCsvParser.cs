using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.IlsCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

public class IlsCsvParser
{
	public IlsCsvDataCollection ParseIlsBase(string filePath)
	{
		var result = new IlsCsvDataCollection
		{
			IlsBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new IlsBase
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					RwyEndId = fields["RWY_END_ID"],
					IlsLocId = fields["ILS_LOC_ID"],
					SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
					StateName = fields["STATE_NAME"],
					RegionCode = fields["REGION_CODE"],
					RwyLen = NasrCsvReader.ParseInt(fields["RWY_LEN"]),
					RwyWidth = NasrCsvReader.ParseInt(fields["RWY_WIDTH"]),
					Category = fields["CATEGORY"],
					Owner = fields["OWNER"],
					Operator = fields["OPERATOR"],
					ApchBear = NasrCsvReader.ParseDouble(fields["APCH_BEAR"]),
					MagVar = NasrCsvReader.ParseInt(fields["MAG_VAR"]),
					MagVarHemis = fields["MAG_VAR_HEMIS"],
					BaseComponentStatus = fields["COMPONENT_STATUS"],
					BaseComponentStatusDate = fields["COMPONENT_STATUS_DATE"],
					BaseLatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
					BaseLatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
					BaseLatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
					BaseLatHemis = fields["LAT_HEMIS"],
					BaseLatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
					BaseLongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
					BaseLongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
					BaseLongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
					BaseLongHemis = fields["LONG_HEMIS"],
					BaseLongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
					BaseLatLongSourceCode = fields["LAT_LONG_SOURCE_CODE"],
					BaseSiteElevation = NasrCsvReader.ParseNullableDouble(fields["SITE_ELEVATION"]),
					LocFreq = NasrCsvReader.ParseDouble(fields["LOC_FREQ"]),
					BkCourseStatusCode = fields["BK_COURSE_STATUS_CODE"],
				})
		};

		return result;
	}

	public IlsCsvDataCollection ParseIlsDme(string filePath)
	{
		var result = new IlsCsvDataCollection
		{
			IlsDme = NasrCsvReader.ProcessLines(
				filePath,
				fields => new IlsDme
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					RwyEndId = fields["RWY_END_ID"],
					IlsLocId = fields["ILS_LOC_ID"],
					SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
					DmeComponentStatus = fields["COMPONENT_STATUS"],
					DmeComponentStatusDate = fields["COMPONENT_STATUS_DATE"],
					DmeLatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
					DmeLatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
					DmeLatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
					DmeLatHemis = fields["LAT_HEMIS"],
					DmeLatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
					DmeLongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
					DmeLongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
					DmeLongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
					DmeLongHemis = fields["LONG_HEMIS"],
					DmeLongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
					DmeLatLongSourceCode = fields["LAT_LONG_SOURCE_CODE"],
					DmeSiteElevation = NasrCsvReader.ParseNullableDouble(fields["SITE_ELEVATION"]),
					Channel = fields["CHANNEL"],
				})
		};

		return result;
	}

	public IlsCsvDataCollection ParseIlsGs(string filePath)
	{
		var result = new IlsCsvDataCollection
		{
			IlsGs = NasrCsvReader.ProcessLines(
				filePath,
				fields => new IlsGs
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					RwyEndId = fields["RWY_END_ID"],
					IlsLocId = fields["ILS_LOC_ID"],
					SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
					GsComponentStatus = fields["COMPONENT_STATUS"],
					GsComponentStatusDate = fields["COMPONENT_STATUS_DATE"],
					GsLatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
					GsLatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
					GsLatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
					GsLatHemis = fields["LAT_HEMIS"],
					GsLatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
					GsLongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
					GsLongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
					GsLongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
					GsLongHemis = fields["LONG_HEMIS"],
					GsLongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
					GsLatLongSourceCode = fields["LAT_LONG_SOURCE_CODE"],
					GsSiteElevation = NasrCsvReader.ParseNullableDouble(fields["SITE_ELEVATION"]),
					GSTypeCode = fields["G_S_TYPE_CODE"],
					GSAngle = NasrCsvReader.ParseDouble(fields["G_S_ANGLE"]),
					GSFreq = NasrCsvReader.ParseDouble(fields["G_S_FREQ"]),
				})
		};

		return result;
	}

	public IlsCsvDataCollection ParseIlsMkr(string filePath)
	{
		var result = new IlsCsvDataCollection
		{
			IlsMkr = NasrCsvReader.ProcessLines(
				filePath,
				fields => new IlsMkr
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					RwyEndId = fields["RWY_END_ID"],
					IlsLocId = fields["ILS_LOC_ID"],
					SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
					MkrIlsCompTypeCode = fields["ILS_COMP_TYPE_CODE"],
					MkrComponentStatus = fields["COMPONENT_STATUS"],
					MkrComponentStatusDate = fields["COMPONENT_STATUS_DATE"],
					MkrLatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
					MkrLatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
					MkrLatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
					MkrLatHemis = fields["LAT_HEMIS"],
					MkrLatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
					MkrLongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
					MkrLongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
					MkrLongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
					MkrLongHemis = fields["LONG_HEMIS"],
					MkrLongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
					MkrLatLongSourceCode = fields["LAT_LONG_SOURCE_CODE"],
					MkrSiteElevation = NasrCsvReader.ParseNullableDouble(fields["SITE_ELEVATION"]),
					MkrFacTypeCode = fields["MKR_FAC_TYPE_CODE"],
					MarkerIdBeacon = fields["MARKER_ID_BEACON"],
					CompassLocatorName = fields["COMPASS_LOCATOR_NAME"],
					Freq = NasrCsvReader.ParseNullableDouble(fields["FREQ"]),
					NavId = fields["NAV_ID"],
					NavType = fields["NAV_TYPE"],
					LowPoweredNdbStatus = fields["LOW_POWERED_NDB_STATUS"],
				})
		};

		return result;
	}

	public IlsCsvDataCollection ParseIlsRmk(string filePath)
	{
		var result = new IlsCsvDataCollection
		{
			IlsRmk = NasrCsvReader.ProcessLines(
				filePath,
				fields => new IlsRmk
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					RwyEndId = fields["RWY_END_ID"],
					IlsLocId = fields["ILS_LOC_ID"],
					SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
					TabName = fields["TAB_NAME"],
					RmkIlsCompTypeCode = fields["ILS_COMP_TYPE_CODE"],
					RefColName = fields["REF_COL_NAME"],
					RefColSeqNo = NasrCsvReader.ParseInt(fields["REF_COL_SEQ_NO"]),
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

}

public class IlsCsvDataCollection
{
	public List<IlsBase> IlsBase { get; set; } = [];
	public List<IlsDme> IlsDme { get; set; } = [];
	public List<IlsGs> IlsGs { get; set; } = [];
	public List<IlsMkr> IlsMkr { get; set; } = [];
	public List<IlsRmk> IlsRmk { get; set; } = [];
}