using FEBuddyLibrary.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FEBuddyLibrary.Models.NASR.CSV.PjaCsvDataModel;

namespace FEBuddyLibrary.Parsers.NASR.CSV
{
    public class PjaCsvParser
    {
        public PjaCsvDataCollection ParsePjaBase(string filePath)
        {
            var result = new PjaCsvDataCollection();

            result.PjaBase = FebCsvHelper.ProcessLines(
                filePath,
                fields => new PjaBase
                {
                    EffDate = fields["EFF_DATE"],
                    PjaId = fields["PJA_ID"],
                    NavId = fields["NAV_ID"],
                    NavType = fields["NAV_TYPE"],
                    Radial = FebCsvHelper.ParseNullableDouble(fields["RADIAL"]),
                    Distance = FebCsvHelper.ParseNullableDouble(fields["DISTANCE"]),
                    NavaidName = fields["NAVAID_NAME"],
                    StateCode = fields["STATE_CODE"],
                    City = fields["CITY"],
                    Latitude = fields["LATITUDE"],
                    LatDecimal = FebCsvHelper.ParseDouble(fields["LAT_DECIMAL"]),
                    Longitude = fields["LONGITUDE"],
                    LongDecimal = FebCsvHelper.ParseDouble(fields["LONG_DECIMAL"]),
                    ArptId = fields["ARPT_ID"],
                    SiteNo = fields["SITE_NO"],
                    SiteTypeCode = fields["SITE_TYPE_CODE"],
                    DropZoneName = fields["DROP_ZONE_NAME"],
                    MaxAltitude = FebCsvHelper.ParseNullableInt(fields["MAX_ALTITUDE"]),
                    MaxAltitudeTypeCode = fields["MAX_ALTITUDE_TYPE_CODE"],
                    PjaRadius = FebCsvHelper.ParseNullableDouble(fields["PJA_RADIUS"]),
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
                });

            return result;
        }

        public PjaCsvDataCollection ParsePjaCon(string filePath)
        {
            var result = new PjaCsvDataCollection();

            result.PjaCon = FebCsvHelper.ProcessLines(
                filePath,
                fields => new PjaCon
                {
                    EffDate = fields["EFF_DATE"],
                    PjaId = fields["PJA_ID"],
                    FacId = fields["FAC_ID"],
                    FacName = fields["FAC_NAME"],
                    LocId = fields["LOC_ID"],
                    CommercialFreq = FebCsvHelper.ParseDouble(fields["COMMERCIAL_FREQ"]),
                    CommercialChartFlag = fields["COMMERCIAL_CHART_FLAG"],
                    MilFreq = FebCsvHelper.ParseNullableDouble(fields["MIL_FREQ"]),
                    MilChartFlag = fields["MIL_CHART_FLAG"],
                    Sector = fields["SECTOR"],
                    ContactFreqAltitude = fields["CONTACT_FREQ_ALTITUDE"],
                });

            return result;
        }

    }

    public class PjaCsvDataCollection
    {
        public List<PjaBase> PjaBase { get; set; } = new();
        public List<PjaCon> PjaCon { get; set; } = new();
    }
}