using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.ClsArspCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers
{
    public class ClsArspCsvParser
    {
        public ClsArspCsvDataCollection ParseClsArsp(string filePath)
        {
            var result = new ClsArspCsvDataCollection();

            result.ClsArsp = NasrCsvReader.ProcessLines(
                filePath,
                fields => new ClsArsp
                {
                    EffDate = fields["EFF_DATE"],
                    SiteNo = fields["SITE_NO"],
                    SiteTypeCode = fields["SITE_TYPE_CODE"],
                    StateCode = fields["STATE_CODE"],
                    ArptId = fields["ARPT_ID"],
                    City = fields["CITY"],
                    CountryCode = fields["COUNTRY_CODE"],
                    ClassBAirspace = fields["CLASS_B_AIRSPACE"],
                    ClassCAirspace = fields["CLASS_C_AIRSPACE"],
                    ClassDAirspace = fields["CLASS_D_AIRSPACE"],
                    ClassEAirspace = fields["CLASS_E_AIRSPACE"],
                    AirspaceHrs = fields["AIRSPACE_HRS"],
                    Remark = fields["REMARK"],
                });

            return result;
        }

    }

    public class ClsArspCsvDataCollection
    {
        public List<ClsArsp> ClsArsp { get; set; } = new();
    }
}