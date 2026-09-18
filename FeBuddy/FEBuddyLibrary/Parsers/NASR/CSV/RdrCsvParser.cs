using FEBuddyLibrary.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FEBuddyLibrary.Models.NASR.CSV.RdrCsvDataModel;

namespace FEBuddyLibrary.Parsers.NASR.CSV
{
    public class RdrCsvParser
    {
        public RdrCsvDataCollection ParseRdr(string filePath)
        {
            var result = new RdrCsvDataCollection();

            result.Rdr = FebCsvHelper.ProcessLines(
                filePath,
                fields => new Rdr
                {
                    EffDate = fields["EFF_DATE"],
                    FacilityId = fields["FACILITY_ID"],
                    FacilityType = fields["FACILITY_TYPE"],
                    StateCode = fields["STATE_CODE"],
                    CountryCode = fields["COUNTRY_CODE"],
                    RadarType = fields["RADAR_TYPE"],
                    RadarNo = FebCsvHelper.ParseInt(fields["RADAR_NO"]),
                    RadarHrs = fields["RADAR_HRS"],
                    Remark = fields["REMARK"],
                });

            return result;
        }

    }

    public class RdrCsvDataCollection
    {
        public List<Rdr> Rdr { get; set; } = new();
    }
}