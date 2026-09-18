using FeBuddy.Core.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Models.NASR.CSV.FrqCsvDataModel;

namespace FeBuddy.Core.Parsers.NASR.CSV
{
    public class FrqCsvParser
    {
        public FrqCsvDataCollection ParseFrq(string filePath)
        {
            var result = new FrqCsvDataCollection();

            result.Frq = FebCsvHelper.ProcessLines(
                filePath,
                fields => new Frq
                {
                    EffDate = fields["EFF_DATE"],
                    Facility = fields["FACILITY"],
                    FacName = fields["FAC_NAME"],
                    FacilityType = fields["FACILITY_TYPE"],
                    ArtccOrFssId = fields["ARTCC_OR_FSS_ID"],
                    Cpdlc = fields["CPDLC"],
                    TowerHrs = fields["TOWER_HRS"],
                    ServicedFacility = fields["SERVICED_FACILITY"],
                    ServicedFacName = fields["SERVICED_FAC_NAME"],
                    ServicedSiteType = fields["SERVICED_SITE_TYPE"],
                    LatDecimal = FebCsvHelper.ParseNullableDouble(fields["LAT_DECIMAL"]),
                    LongDecimal = FebCsvHelper.ParseNullableDouble(fields["LONG_DECIMAL"]),
                    ServicedCity = fields["SERVICED_CITY"],
                    ServicedState = fields["SERVICED_STATE"],
                    ServicedCountry = fields["SERVICED_COUNTRY"],
                    TowerOrCommCall = fields["TOWER_OR_COMM_CALL"],
                    PrimaryApproachRadioCall = fields["PRIMARY_APPROACH_RADIO_CALL"],
                    Freq = fields["FREQ"],
                    Sectorization = fields["SECTORIZATION"],
                    FreqUse = fields["FREQ_USE"],
                    Remark = fields["REMARK"],
                });

            return result;
        }

    }

    public class FrqCsvDataCollection
    {
        public List<Frq> Frq { get; set; } = new();
    }
}