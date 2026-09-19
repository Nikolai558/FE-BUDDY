using FeBuddy.Core.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Models.NASR.CSV.MtrCsvDataModel;

namespace FeBuddy.Core.Parsers.NASR.CSV
{
    public class MtrCsvParser
    {
        public MtrCsvDataCollection ParseMtrAgy(string filePath)
        {
            var result = new MtrCsvDataCollection();

            result.MtrAgy = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MtrAgy
                {
                    EffDate = fields["EFF_DATE"],
                    RouteTypeCode = fields["ROUTE_TYPE_CODE"],
                    RouteId = fields["ROUTE_ID"],
                    Artcc = fields["ARTCC"],
                    AgencyType = fields["AGENCY_TYPE"],
                    AgencyName = fields["AGENCY_NAME"],
                    Station = fields["STATION"],
                    Address = fields["ADDRESS"],
                    City = fields["CITY"],
                    StateCode = fields["STATE_CODE"],
                    ZipCode = fields["ZIP_CODE"],
                    CommercialNo = fields["COMMERCIAL_NO"],
                    DsnNo = fields["DSN_NO"],
                    Hours = fields["HOURS"],
                });

            return result;
        }

        public MtrCsvDataCollection ParseMtrBase(string filePath)
        {
            var result = new MtrCsvDataCollection();

            result.MtrBase = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MtrBase
                {
                    EffDate = fields["EFF_DATE"],
                    RouteTypeCode = fields["ROUTE_TYPE_CODE"],
                    RouteId = fields["ROUTE_ID"],
                    Artcc = fields["ARTCC"],
                    Fss = fields["FSS"],
                    TimeOfUse = fields["TIME_OF_USE"],
                });

            return result;
        }

        public MtrCsvDataCollection ParseMtrPt(string filePath)
        {
            var result = new MtrCsvDataCollection();

            result.MtrPt = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MtrPt
                {
                    EffDate = fields["EFF_DATE"],
                    RouteTypeCode = fields["ROUTE_TYPE_CODE"],
                    RouteId = fields["ROUTE_ID"],
                    Artcc = fields["ARTCC"],
                    RoutePtSeq = FebCsvHelper.ParseInt(fields["ROUTE_PT_SEQ"]),
                    RoutePtId = fields["ROUTE_PT_ID"],
                    NextRoutePtId = fields["NEXT_ROUTE_PT_ID"],
                    SegmentText = fields["SEGMENT_TEXT"],
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
                    NavId = fields["NAV_ID"],
                    NavaidBearing = FebCsvHelper.ParseNullableInt(fields["NAVAID_BEARING"]),
                    NavaidDist = FebCsvHelper.ParseNullableInt(fields["NAVAID_DIST"]),
                });

            return result;
        }

        public MtrCsvDataCollection ParseMtrSop(string filePath)
        {
            var result = new MtrCsvDataCollection();

            result.MtrSop = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MtrSop
                {
                    EffDate = fields["EFF_DATE"],
                    RouteTypeCode = fields["ROUTE_TYPE_CODE"],
                    RouteId = fields["ROUTE_ID"],
                    Artcc = fields["ARTCC"],
                    SopSeqNo = FebCsvHelper.ParseInt(fields["SOP_SEQ_NO"]),
                    SopText = fields["SOP_TEXT"],
                });

            return result;
        }

        public MtrCsvDataCollection ParseMtrTerr(string filePath)
        {
            var result = new MtrCsvDataCollection();

            result.MtrTerr = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MtrTerr
                {
                    EffDate = fields["EFF_DATE"],
                    RouteTypeCode = fields["ROUTE_TYPE_CODE"],
                    RouteId = fields["ROUTE_ID"],
                    Artcc = fields["ARTCC"],
                    TerrainSeqNo = FebCsvHelper.ParseInt(fields["TERRAIN_SEQ_NO"]),
                    TerrainText = fields["TERRAIN_TEXT"],
                });

            return result;
        }

        public MtrCsvDataCollection ParseMtrWdth(string filePath)
        {
            var result = new MtrCsvDataCollection();

            result.MtrWdth = FebCsvHelper.ProcessLines(
                filePath,
                fields => new MtrWdth
                {
                    EffDate = fields["EFF_DATE"],
                    RouteTypeCode = fields["ROUTE_TYPE_CODE"],
                    RouteId = fields["ROUTE_ID"],
                    Artcc = fields["ARTCC"],
                    WidthSeqNo = FebCsvHelper.ParseInt(fields["WIDTH_SEQ_NO"]),
                    WidthText = fields["WIDTH_TEXT"],
                });

            return result;
        }

    }

    public class MtrCsvDataCollection
    {
        public List<MtrAgy> MtrAgy { get; set; } = new();
        public List<MtrBase> MtrBase { get; set; } = new();
        public List<MtrPt> MtrPt { get; set; } = new();
        public List<MtrSop> MtrSop { get; set; } = new();
        public List<MtrTerr> MtrTerr { get; set; } = new();
        public List<MtrWdth> MtrWdth { get; set; } = new();
    }
}