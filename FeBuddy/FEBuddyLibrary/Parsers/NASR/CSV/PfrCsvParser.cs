using FEBuddyLibrary.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FEBuddyLibrary.Models.NASR.CSV.PfrCsvDataModel;

namespace FEBuddyLibrary.Parsers.NASR.CSV
{
    public class PfrCsvParser
    {
        public PfrCsvDataCollection ParsePfrBase(string filePath)
        {
            var result = new PfrCsvDataCollection();

            result.PfrBase = FebCsvHelper.ProcessLines(
                filePath,
                fields => new PfrBase
                {
                    EffDate = fields["EFF_DATE"],
                    OriginId = fields["ORIGIN_ID"],
                    OriginCity = fields["ORIGIN_CITY"],
                    OriginStateCode = fields["ORIGIN_STATE_CODE"],
                    OriginCountryCode = fields["ORIGIN_COUNTRY_CODE"],
                    DstnId = fields["DSTN_ID"],
                    DstnCity = fields["DSTN_CITY"],
                    DstnStateCode = fields["DSTN_STATE_CODE"],
                    DstnCountryCode = fields["DSTN_COUNTRY_CODE"],
                    PfrTypeCode = fields["PFR_TYPE_CODE"],
                    RouteNo = FebCsvHelper.ParseInt(fields["ROUTE_NO"]),
                    SpecialAreaDescrip = fields["SPECIAL_AREA_DESCRIP"],
                    AltDescrip = fields["ALT_DESCRIP"],
                    BaseAircraft = fields["AIRCRAFT"],
                    Hours = fields["HOURS"],
                    RouteDirDescrip = fields["ROUTE_DIR_DESCRIP"],
                    Designator = fields["DESIGNATOR"],
                    NarType = fields["NAR_TYPE"],
                    InlandFacFix = fields["INLAND_FAC_FIX"],
                    CoastalFix = fields["COASTAL_FIX"],
                    Destination = fields["DESTINATION"],
                    BaseRouteString = fields["ROUTE_STRING"],
                });

            return result;
        }

        public PfrCsvDataCollection ParsePfrRmtFmt(string filePath)
        {
            var result = new PfrCsvDataCollection();

            result.PfrRmtFmt = FebCsvHelper.ProcessLines(
                filePath,
                fields => new PfrRmtFmt
                {
                    Orig = fields["Orig"],
                    RmtFmtRouteString = fields["Route String"],
                    Dest = fields["Dest"],
                    Hours1 = fields["Hours1"],
                    Type = fields["Type"],
                    Area = fields["Area"],
                    Altitude = fields["Altitude"],
                    RmtFmtAircraft = fields["Aircraft"],
                    Direction = fields["Direction"],
                    Seq = FebCsvHelper.ParseInt(fields["Seq"]),
                    Dcntr = fields["DCNTR"],
                    Acntr = fields["ACNTR"],
                });

            return result;
        }

        public PfrCsvDataCollection ParsePfrSeg(string filePath)
        {
            var result = new PfrCsvDataCollection();

            result.PfrSeg = FebCsvHelper.ProcessLines(
                filePath,
                fields => new PfrSeg
                {
                    EffDate = fields["EFF_DATE"],
                    OriginId = fields["ORIGIN_ID"],
                    DstnId = fields["DSTN_ID"],
                    PfrTypeCode = fields["PFR_TYPE_CODE"],
                    RouteNo = FebCsvHelper.ParseInt(fields["ROUTE_NO"]),
                    SegmentSeq = FebCsvHelper.ParseInt(fields["SEGMENT_SEQ"]),
                    SegValue = fields["SEG_VALUE"],
                    SegType = fields["SEG_TYPE"],
                    StateCode = fields["STATE_CODE"],
                    CountryCode = fields["COUNTRY_CODE"],
                    IcaoRegionCode = fields["ICAO_REGION_CODE"],
                    NavType = fields["NAV_TYPE"],
                    NextSeg = fields["NEXT_SEG"],
                });

            return result;
        }

    }

    public class PfrCsvDataCollection
    {
        public List<PfrBase> PfrBase { get; set; } = new();
        public List<PfrRmtFmt> PfrRmtFmt { get; set; } = new();
        public List<PfrSeg> PfrSeg { get; set; } = new();
    }
}