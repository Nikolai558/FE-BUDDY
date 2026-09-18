using FeBuddy.Core.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Models.NASR.CSV.CdrCsvDataModel;

namespace FeBuddy.Core.Parsers.NASR.CSV
{
    public class CdrCsvParser
    {
        public CdrCsvDataCollection ParseCdr(string filePath)
        {
            var result = new CdrCsvDataCollection();

            result.Cdr = FebCsvHelper.ProcessLines(
                filePath,
                fields => new Cdr
                {
                    Rcode = fields["RCode"],
                    Orig = fields["Orig"],
                    Dest = fields["Dest"],
                    Depfix = fields["DepFix"],
                    RouteString = fields["Route String"],
                    Dcntr = fields["DCNTR"],
                    Acntr = fields["ACNTR"],
                    Tcntrs = fields["TCNTRs"],
                    Coordreq = fields["CoordReq"],
                    Play = fields["Play"],
                    Naveqp = FebCsvHelper.ParseInt(fields["NavEqp"]),
                    Length = FebCsvHelper.ParseNullableInt(fields["Length"]),
                });

            return result;
        }

    }

    public class CdrCsvDataCollection
    {
        public List<Cdr> Cdr { get; set; } = new();
    }
}