using static FeBuddy.Core.Infrastructure.Nasr.Models.DpCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>DP</c> CSV files (departure procedures), one file per method.
/// </summary>
public class DpCsvParser
{
	/// <summary>Reads <c>DP_APT.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="DpCsvDataCollection.DpApt"/> filled in.</returns>
	public DpCsvDataCollection ParseDpApt(string filePath)
	{
		var result = new DpCsvDataCollection
		{
			DpApt = NasrCsvReader.ProcessLines(
				filePath,
				fields => new DpApt
				{
					EffDate = fields["EFF_DATE"],
					DpName = fields["DP_NAME"],
					Artcc = fields["ARTCC"],
					DpComputerCode = fields["DP_COMPUTER_CODE"],
					BodyName = fields["BODY_NAME"],
					AptBodySeq = NasrCsvReader.ParseInt(fields["BODY_SEQ"]),
					ArptId = fields["ARPT_ID"],
					RwyEndId = fields["RWY_END_ID"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>DP_BASE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="DpCsvDataCollection.DpBase"/> filled in.</returns>
	public DpCsvDataCollection ParseDpBase(string filePath)
	{
		var result = new DpCsvDataCollection
		{
			DpBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new DpBase
				{
					EffDate = fields["EFF_DATE"],
					DpName = fields["DP_NAME"],
					AmendmentNo = fields["AMENDMENT_NO"],
					Artcc = fields["ARTCC"],
					DpComputerCode = fields["DP_COMPUTER_CODE"],
					DpAmendEffDate = fields["DP_AMEND_EFF_DATE"],
					RnavFlag = fields["RNAV_FLAG"],
					GraphicalDpType = fields["GRAPHICAL_DP_TYPE"],
					ServedArpt = fields["SERVED_ARPT"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>DP_RTE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="DpCsvDataCollection.DpRte"/> filled in.</returns>
	public DpCsvDataCollection ParseDpRte(string filePath)
	{
		var result = new DpCsvDataCollection
		{
			DpRte = NasrCsvReader.ProcessLines(
				filePath,
				fields => new DpRte
				{
					EffDate = fields["EFF_DATE"],
					DpName = fields["DP_NAME"],
					Artcc = fields["ARTCC"],
					DpComputerCode = fields["DP_COMPUTER_CODE"],
					RoutePortionType = fields["ROUTE_PORTION_TYPE"],
					RouteName = fields["ROUTE_NAME"],
					RteBodySeq = NasrCsvReader.ParseInt(fields["BODY_SEQ"]),
					TransitionComputerCode = fields["TRANSITION_COMPUTER_CODE"],
					PointSeq = NasrCsvReader.ParseInt(fields["POINT_SEQ"]),
					Point = fields["POINT"],
					IcaoRegionCode = fields["ICAO_REGION_CODE"],
					PointType = fields["POINT_TYPE"],
					NextPoint = fields["NEXT_POINT"],
					ArptRwyAssoc = fields["ARPT_RWY_ASSOC"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>DP</c> CSV files, one list per file.
/// </summary>
public class DpCsvDataCollection
{
	/// <summary>The rows of <c>DP_APT.csv</c>.</summary>
	public List<DpApt> DpApt { get; set; } = [];
	/// <summary>The rows of <c>DP_BASE.csv</c>.</summary>
	public List<DpBase> DpBase { get; set; } = [];
	/// <summary>The rows of <c>DP_RTE.csv</c>.</summary>
	public List<DpRte> DpRte { get; set; } = [];
}