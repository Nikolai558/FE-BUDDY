using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.MilOpsCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers
{
    public class MilOpsCsvParser
    {
        public MilOpsCsvDataCollection ParseMilOps(string filePath)
        {
            var result = new MilOpsCsvDataCollection();

            result.MilOps = NasrCsvReader.ProcessLines(
                filePath,
                fields => new MilOps
                {
                    EffDate = fields["EFF_DATE"],
                    SiteNo = fields["SITE_NO"],
                    SiteTypeCode = fields["SITE_TYPE_CODE"],
                    StateCode = fields["STATE_CODE"],
                    ArptId = fields["ARPT_ID"],
                    City = fields["CITY"],
                    CountryCode = fields["COUNTRY_CODE"],
                    MilOpsOperCode = fields["MIL_OPS_OPER_CODE"],
                    MilOpsCall = fields["MIL_OPS_CALL"],
                    MilOpsHrs = fields["MIL_OPS_HRS"],
                    AmcpHrs = fields["AMCP_HRS"],
                    PmsvHrs = fields["PMSV_HRS"],
                    Remark = fields["REMARK"],
                });

            return result;
        }

    }

    public class MilOpsCsvDataCollection
    {
        public List<MilOps> MilOps { get; set; } = new();
    }
}