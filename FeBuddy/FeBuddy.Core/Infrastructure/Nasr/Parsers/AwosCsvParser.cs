using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.AwosCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

public class AwosCsvParser
{
	public AwosCsvDataCollection ParseAwos(string filePath)
	{
		var result = new AwosCsvDataCollection
		{
			Awos = NasrCsvReader.ProcessLines(
				filePath,
				fields => new Awos
				{
					EffDate = fields["EFF_DATE"],
					AsosAwosId = fields["ASOS_AWOS_ID"],
					AsosAwosType = fields["ASOS_AWOS_TYPE"],
					StateCode = fields["STATE_CODE"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					CommissionedDate = fields["COMMISSIONED_DATE"],
					NavaidFlag = fields["NAVAID_FLAG"],
					LatDeg = NasrCsvReader.ParseNullableInt(fields["LAT_DEG"]),
					LatMin = NasrCsvReader.ParseNullableInt(fields["LAT_MIN"]),
					LatSec = NasrCsvReader.ParseNullableDouble(fields["LAT_SEC"]),
					LatHemis = fields["LAT_HEMIS"],
					LatDecimal = NasrCsvReader.ParseNullableDouble(fields["LAT_DECIMAL"]),
					LongDeg = NasrCsvReader.ParseNullableInt(fields["LONG_DEG"]),
					LongMin = NasrCsvReader.ParseNullableInt(fields["LONG_MIN"]),
					LongSec = NasrCsvReader.ParseNullableDouble(fields["LONG_SEC"]),
					LongHemis = fields["LONG_HEMIS"],
					LongDecimal = NasrCsvReader.ParseNullableDouble(fields["LONG_DECIMAL"]),
					Elev = NasrCsvReader.ParseNullableDouble(fields["ELEV"]),
					SurveyMethodCode = fields["SURVEY_METHOD_CODE"],
					PhoneNo = fields["PHONE_NO"],
					SecondPhoneNo = fields["SECOND_PHONE_NO"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

}

public class AwosCsvDataCollection
{
	public List<Awos> Awos { get; set; } = [];
}