using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.CdrCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers
{
    public class CdrCsvParser
    {
        public CdrCsvDataCollection ParseCdr(string filePath)
        {
            var result = new CdrCsvDataCollection();

            result.Cdr = NasrCsvReader.ProcessLines(
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
                    Naveqp = NasrCsvReader.ParseInt(fields["NavEqp"]),
                    Length = NasrCsvReader.ParseNullableInt(fields["Length"]),
                });

            return result;
        }

    }

    public class CdrCsvDataCollection
    {
        public List<Cdr> Cdr { get; set; } = new();
    }
}