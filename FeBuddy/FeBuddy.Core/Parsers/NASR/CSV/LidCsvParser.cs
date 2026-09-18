using FeBuddy.Core.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Models.NASR.CSV.LidCsvDataModel;

namespace FeBuddy.Core.Parsers.NASR.CSV
{
    public class LidCsvParser
    {
        public LidCsvDataCollection ParseLid(string filePath)
        {
            var result = new LidCsvDataCollection();

            result.Lid = FebCsvHelper.ProcessLines(
                filePath,
                fields => new Lid
                {
                    EffDate = fields["EFF_DATE"],
                    CountryCode = fields["COUNTRY_CODE"],
                    LocId = fields["LOC_ID"],
                    RegionCode = fields["REGION_CODE"],
                    State = fields["STATE"],
                    City = fields["CITY"],
                    LidGroup = fields["LID_GROUP"],
                    FacType = fields["FAC_TYPE"],
                    FacName = fields["FAC_NAME"],
                    RespArtccId = fields["RESP_ARTCC_ID"],
                    ArtccComputerId = fields["ARTCC_COMPUTER_ID"],
                    FssId = fields["FSS_ID"],
                });

            return result;
        }

    }

    public class LidCsvDataCollection
    {
        public List<Lid> Lid { get; set; } = new();
    }
}