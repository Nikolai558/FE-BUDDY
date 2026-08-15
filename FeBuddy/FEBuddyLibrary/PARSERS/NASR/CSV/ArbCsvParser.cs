using FEBuddyLibrary.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FEBuddyLibrary.Models.NASR.CSV.ArbCsvDataModel;

namespace FEBuddyLibrary.Parsers.NASR.CSV
{
    public class ArbCsvParser
    {
        public ArbCsvDataCollection ParseArbBase(string filePath)
        {
            var result = new ArbCsvDataCollection();

            result.ArbBase = FebCsvHelper.ProcessLines(
                filePath,
                fields => new ArbBase
                {
                    EffDate = fields["EFF_DATE"],
                    LocationId = fields["LOCATION_ID"],
                    LocationName = fields["LOCATION_NAME"],
                    ComputerId = fields["COMPUTER_ID"],
                    IcaoId = fields["ICAO_ID"],
                    LocationType = fields["LOCATION_TYPE"],
                    City = fields["CITY"],
                    State = fields["STATE"],
                    CountryCode = fields["COUNTRY_CODE"],
                    BaseLatDeg = FebCsvHelper.ParseInt(fields["LAT_DEG"]),
                    BaseLatMin = FebCsvHelper.ParseInt(fields["LAT_MIN"]),
                    BaseLatSec = FebCsvHelper.ParseDouble(fields["LAT_SEC"]),
                    BaseLatHemis = fields["LAT_HEMIS"],
                    BaseLatDecimal = FebCsvHelper.ParseDouble(fields["LAT_DECIMAL"]),
                    BaseLongDeg = FebCsvHelper.ParseInt(fields["LONG_DEG"]),
                    BaseLongMin = FebCsvHelper.ParseInt(fields["LONG_MIN"]),
                    BaseLongSec = FebCsvHelper.ParseDouble(fields["LONG_SEC"]),
                    BaseLongHemis = fields["LONG_HEMIS"],
                    BaseLongDecimal = FebCsvHelper.ParseDouble(fields["LONG_DECIMAL"]),
                    CrossRef = fields["CROSS_REF"],
                });

            return result;
        }

        public ArbCsvDataCollection ParseArbSeg(string filePath)
        {
            var result = new ArbCsvDataCollection();

            result.ArbSeg = FebCsvHelper.ProcessLines(
                filePath,
                fields => new ArbSeg
                {
                    EffDate = fields["EFF_DATE"],
                    RecId = fields["REC_ID"],
                    LocationId = fields["LOCATION_ID"],
                    LocationName = fields["LOCATION_NAME"],
                    Altitude = fields["ALTITUDE"],
                    Type = fields["TYPE"],
                    PointSeq = FebCsvHelper.ParseInt(fields["POINT_SEQ"]),
                    SegLatDeg = FebCsvHelper.ParseInt(fields["LAT_DEG"]),
                    SegLatMin = FebCsvHelper.ParseInt(fields["LAT_MIN"]),
                    SegLatSec = FebCsvHelper.ParseDouble(fields["LAT_SEC"]),
                    SegLatHemis = fields["LAT_HEMIS"],
                    SegLatDecimal = FebCsvHelper.ParseDouble(fields["LAT_DECIMAL"]),
                    SegLongDeg = FebCsvHelper.ParseInt(fields["LONG_DEG"]),
                    SegLongMin = FebCsvHelper.ParseInt(fields["LONG_MIN"]),
                    SegLongSec = FebCsvHelper.ParseDouble(fields["LONG_SEC"]),
                    SegLongHemis = fields["LONG_HEMIS"],
                    SegLongDecimal = FebCsvHelper.ParseDouble(fields["LONG_DECIMAL"]),
                    BndryPtDescrip = fields["BNDRY_PT_DESCRIP"],
                    NasDescripFlag = fields["NAS_DESCRIP_FLAG"],
                });

            return result;
        }

    }

    public class ArbCsvDataCollection
    {
        public List<ArbBase> ArbBase { get; set; } = new();
        public List<ArbSeg> ArbSeg { get; set; } = new();
    }
}