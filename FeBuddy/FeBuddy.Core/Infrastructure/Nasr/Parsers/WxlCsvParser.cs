using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.WxlCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

public class WxlCsvParser
{
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

public class WxlCsvDataCollection
{
	public List<WxlBase> WxlBase { get; set; } = [];
	public List<WxlSvc> WxlSvc { get; set; } = [];
}