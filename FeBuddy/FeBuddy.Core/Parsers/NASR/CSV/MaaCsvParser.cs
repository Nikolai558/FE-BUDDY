using FeBuddy.Core.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Models.NASR.CSV.MaaCsvDataModel;

namespace FeBuddy.Core.Parsers.NASR.CSV
{
    public class MaaCsvParser
    {
        public MaaCsvDataCollection ParseMaaBase(string filePath)
        {
            var result = new MaaCsvDataCollection();

            result.MaaBase = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MaaBase
                {
                    EffDate = fields["EFF_DATE"],
                    MaaId = fields["MAA_ID"],
                    MaaTypeName = fields["MAA_TYPE_NAME"],
                    NavId = fields["NAV_ID"],
                    NavType = fields["NAV_TYPE"],
                    NavRadial = FebCsvHelper.ParseNullableDouble(fields["NAV_RADIAL"]),
                    NavDistance = FebCsvHelper.ParseNullableDouble(fields["NAV_DISTANCE"]),
                    StateCode = fields["STATE_CODE"],
                    City = fields["CITY"],
                    BaseLatitude = fields["LATITUDE"],
                    BaseLongitude = fields["LONGITUDE"],
                    ArptIds = fields["ARPT_IDS"],
                    NearestArpt = fields["NEAREST_ARPT"],
                    NearestArptDist = FebCsvHelper.ParseNullableDouble(fields["NEAREST_ARPT_DIST"]),
                    NearestArptDir = fields["NEAREST_ARPT_DIR"],
                    MaaName = fields["MAA_NAME"],
                    MaxAlt = fields["MAX_ALT"],
                    MinAlt = fields["MIN_ALT"],
                    MaaRadius = FebCsvHelper.ParseNullableDouble(fields["MAA_RADIUS"]),
                    Description = fields["DESCRIPTION"],
                    MaaUse = fields["MAA_USE"],
                    CheckNotams = fields["CHECK_NOTAMS"],
                    TimeOfUse = fields["TIME_OF_USE"],
                    UserGroupName = fields["USER_GROUP_NAME"],
                });

            return result;
        }

        public MaaCsvDataCollection ParseMaaCon(string filePath)
        {
            var result = new MaaCsvDataCollection();

            result.MaaCon = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MaaCon
                {
                    EffDate = fields["EFF_DATE"],
                    MaaId = fields["MAA_ID"],
                    FreqSeq = FebCsvHelper.ParseInt(fields["FREQ_SEQ"]),
                    FacId = fields["FAC_ID"],
                    FacName = fields["FAC_NAME"],
                    CommercialFreq = FebCsvHelper.ParseDouble(fields["COMMERCIAL_FREQ"]),
                    CommercialChartFlag = fields["COMMERCIAL_CHART_FLAG"],
                    MilFreq = FebCsvHelper.ParseNullableDouble(fields["MIL_FREQ"]),
                    MilChartFlag = fields["MIL_CHART_FLAG"],
                });

            return result;
        }

        public MaaCsvDataCollection ParseMaaRmk(string filePath)
        {
            var result = new MaaCsvDataCollection();

            result.MaaRmk = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MaaRmk
                {
                    EffDate = fields["EFF_DATE"],
                    MaaId = fields["MAA_ID"],
                    TabName = fields["TAB_NAME"],
                    RefColName = fields["REF_COL_NAME"],
                    RefColSeqNo = FebCsvHelper.ParseInt(fields["REF_COL_SEQ_NO"]),
                    Remark = fields["REMARK"],
                });

            return result;
        }

        public MaaCsvDataCollection ParseMaaShp(string filePath)
        {
            var result = new MaaCsvDataCollection();

            result.MaaShp = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MaaShp
                {
                    EffDate = fields["EFF_DATE"],
                    MaaId = fields["MAA_ID"],
                    PointSeq = FebCsvHelper.ParseInt(fields["POINT_SEQ"]),
                    ShpLatitude = fields["LATITUDE"],
                    ShpLongitude = fields["LONGITUDE"],
                });

            return result;
        }

    }

    public class MaaCsvDataCollection
    {
        public List<MaaBase> MaaBase { get; set; } = new();
        public List<MaaCon> MaaCon { get; set; } = new();
        public List<MaaRmk> MaaRmk { get; set; } = new();
        public List<MaaShp> MaaShp { get; set; } = new();
    }
}