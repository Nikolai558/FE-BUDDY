using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.ArbCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers
{
    public class ArbCsvParser
    {
        public ArbCsvDataCollection ParseArbBase(string filePath)
        {
            var result = new ArbCsvDataCollection();

            result.ArbBase = NasrCsvReader.ProcessLines(
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
                    BaseLatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
                    BaseLatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
                    BaseLatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
                    BaseLatHemis = fields["LAT_HEMIS"],
                    BaseLatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
                    BaseLongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
                    BaseLongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
                    BaseLongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
                    BaseLongHemis = fields["LONG_HEMIS"],
                    BaseLongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
                    CrossRef = fields["CROSS_REF"],
                });

            return result;
        }

        public ArbCsvDataCollection ParseArbSeg(string filePath)
        {
            var result = new ArbCsvDataCollection();

            result.ArbSeg = NasrCsvReader.ProcessLines(
                filePath,
                fields => new ArbSeg
                {
                    EffDate = fields["EFF_DATE"],
                    RecId = fields["REC_ID"],
                    LocationId = fields["LOCATION_ID"],
                    LocationName = fields["LOCATION_NAME"],
                    Altitude = fields["ALTITUDE"],
                    Type = fields["TYPE"],
                    PointSeq = NasrCsvReader.ParseInt(fields["POINT_SEQ"]),
                    SegLatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
                    SegLatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
                    SegLatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
                    SegLatHemis = fields["LAT_HEMIS"],
                    SegLatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
                    SegLongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
                    SegLongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
                    SegLongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
                    SegLongHemis = fields["LONG_HEMIS"],
                    SegLongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
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