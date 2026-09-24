using static FeBuddy.Core.Infrastructure.Nasr.Models.CdrCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>CDR</c> CSV files (coded departure routes), one file per method.
/// </summary>
public class CdrCsvParser
{
	/// <summary>Reads <c>CDR.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="CdrCsvDataCollection.Cdr"/> filled in.</returns>
	public CdrCsvDataCollection ParseCdr(string filePath)
	{
		var result = new CdrCsvDataCollection
		{
			Cdr = NasrCsvReader.ProcessLines(
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
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>CDR</c> CSV files, one list per file.
/// </summary>
public class CdrCsvDataCollection
{
	/// <summary>The rows of <c>CDR.csv</c>.</summary>
	public List<Cdr> Cdr { get; set; } = [];
}