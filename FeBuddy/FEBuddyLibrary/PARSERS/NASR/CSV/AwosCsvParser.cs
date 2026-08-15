using FEBuddyLibrary.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FEBuddyLibrary.Models.NASR.CSV.AwosCsvDataModel;

namespace FEBuddyLibrary.Parsers.NASR.CSV
{
    public class AwosCsvParser
    {
        public AwosCsvDataCollection ParseAwos(string filePath)
        {
            var result = new AwosCsvDataCollection();

            result.Awos = FebCsvHelper.ProcessLines(
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
                    LatDeg = FebCsvHelper.ParseNullableInt(fields["LAT_DEG"]),
                    LatMin = FebCsvHelper.ParseNullableInt(fields["LAT_MIN"]),
                    LatSec = FebCsvHelper.ParseNullableDouble(fields["LAT_SEC"]),
                    LatHemis = fields["LAT_HEMIS"],
                    LatDecimal = FebCsvHelper.ParseNullableDouble(fields["LAT_DECIMAL"]),
                    LongDeg = FebCsvHelper.ParseNullableInt(fields["LONG_DEG"]),
                    LongMin = FebCsvHelper.ParseNullableInt(fields["LONG_MIN"]),
                    LongSec = FebCsvHelper.ParseNullableDouble(fields["LONG_SEC"]),
                    LongHemis = fields["LONG_HEMIS"],
                    LongDecimal = FebCsvHelper.ParseNullableDouble(fields["LONG_DECIMAL"]),
                    Elev = FebCsvHelper.ParseNullableDouble(fields["ELEV"]),
                    SurveyMethodCode = fields["SURVEY_METHOD_CODE"],
                    PhoneNo = fields["PHONE_NO"],
                    SecondPhoneNo = fields["SECOND_PHONE_NO"],
                    SiteNo = fields["SITE_NO"],
                    SiteTypeCode = fields["SITE_TYPE_CODE"],
                    Remark = fields["REMARK"],
                });

            return result;
        }

    }

    public class AwosCsvDataCollection
    {
        public List<Awos> Awos { get; set; } = new();
    }
}