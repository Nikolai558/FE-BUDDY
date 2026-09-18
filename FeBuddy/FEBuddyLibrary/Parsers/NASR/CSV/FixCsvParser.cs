using FEBuddyLibrary.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FEBuddyLibrary.Models.NASR.CSV.FixCsvDataModel;

namespace FEBuddyLibrary.Parsers.NASR.CSV
{
    public class FixCsvParser
    {
        public FixCsvDataCollection ParseFixBase(string filePath)
        {
            var result = new FixCsvDataCollection();

            result.FixBase = FebCsvHelper.ProcessLines(
                filePath,
                fields => new FixBase
                {
                    EffDate = fields["EFF_DATE"],
                    FixId = fields["FIX_ID"],
                    IcaoRegionCode = fields["ICAO_REGION_CODE"],
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
                    FixIdOld = fields["FIX_ID_OLD"],
                    ChartingRemark = fields["CHARTING_REMARK"],
                    FixUseCode = fields["FIX_USE_CODE"],
                    ArtccIdHigh = fields["ARTCC_ID_HIGH"],
                    ArtccIdLow = fields["ARTCC_ID_LOW"],
                    PitchFlag = fields["PITCH_FLAG"],
                    CatchFlag = fields["CATCH_FLAG"],
                    SuaAtcaaFlag = fields["SUA_ATCAA_FLAG"],
                    MinRecepAlt = FebCsvHelper.ParseNullableInt(fields["MIN_RECEP_ALT"]),
                    Compulsory = fields["COMPULSORY"],
                    Charts = fields["CHARTS"],
                });

            return result;
        }

        public FixCsvDataCollection ParseFixChrt(string filePath)
        {
            var result = new FixCsvDataCollection();

            result.FixChrt = FebCsvHelper.ProcessLines(
                filePath,
                fields => new FixChrt
                {
                    EffDate = fields["EFF_DATE"],
                    FixId = fields["FIX_ID"],
                    IcaoRegionCode = fields["ICAO_REGION_CODE"],
                    StateCode = fields["STATE_CODE"],
                    CountryCode = fields["COUNTRY_CODE"],
                    ChartingTypeDesc = fields["CHARTING_TYPE_DESC"],
                });

            return result;
        }

        public FixCsvDataCollection ParseFixNav(string filePath)
        {
            var result = new FixCsvDataCollection();

            result.FixNav = FebCsvHelper.ProcessLines(
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
                    Bearing = FebCsvHelper.ParseNullableDouble(fields["BEARING"]),
                    Distance = FebCsvHelper.ParseNullableDouble(fields["DISTANCE"]),
                });

            return result;
        }

    }

    public class FixCsvDataCollection
    {
        public List<FixBase> FixBase { get; set; } = new();
        public List<FixChrt> FixChrt { get; set; } = new();
        public List<FixNav> FixNav { get; set; } = new();
    }
}