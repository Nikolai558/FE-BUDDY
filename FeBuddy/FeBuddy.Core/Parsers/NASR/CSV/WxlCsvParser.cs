using FeBuddy.Core.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Models.NASR.CSV.WxlCsvDataModel;

namespace FeBuddy.Core.Parsers.NASR.CSV
{
    public class WxlCsvParser
    {
        public WxlCsvDataCollection ParseWxlBase(string filePath)
        {
            var result = new WxlCsvDataCollection();

            result.WxlBase = FebCsvHelper.ProcessLines(
                filePath,
                fields => new WxlBase
                {
                    EffDate = fields["EFF_DATE"],
                    WeaId = fields["WEA_ID"],
                    City = fields["CITY"],
                    StateCode = fields["STATE_CODE"],
                    CountryCode = fields["COUNTRY_CODE"],
                    LatDeg = FebCsvHelper.ParseInt(fields["LAT_DEG"]),
                    LatMin = FebCsvHelper.ParseInt(fields["LAT_MIN"]),
                    LatSec = FebCsvHelper.ParseDouble(fields["LAT_SEC"]),
                    LatHemis = fields["LAT_HEMIS"],
                    LatDecimal = FebCsvHelper.ParseDouble(fields["LAT_DECIMAL"]),
                    LongDeg = FebCsvHelper.ParseInt(fields["LONG_DEG"]),
                    LongMin = FebCsvHelper.ParseInt(fields["LONG_MIN"]),
                    LongSec = FebCsvHelper.ParseDouble(fields["LONG_SEC"]),
                    LongHemis = fields["LONG_HEMIS"],
                    LongDecimal = FebCsvHelper.ParseDouble(fields["LONG_DECIMAL"]),
                    Elev = FebCsvHelper.ParseInt(fields["ELEV"]),
                    SurveyMethodCode = fields["SURVEY_METHOD_CODE"],
                });

            return result;
        }

        public WxlCsvDataCollection ParseWxlSvc(string filePath)
        {
            var result = new WxlCsvDataCollection();

            result.WxlSvc = FebCsvHelper.ProcessLines(
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
                });

            return result;
        }

    }

    public class WxlCsvDataCollection
    {
        public List<WxlBase> WxlBase { get; set; } = new();
        public List<WxlSvc> WxlSvc { get; set; } = new();
    }
}