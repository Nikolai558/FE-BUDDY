using static FeBuddy.Core.Infrastructure.Nasr.Models.StarCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>STAR</c> CSV files (standard terminal arrivals), one file per method.
/// </summary>
public class StarCsvParser
{
	/// <summary>Reads <c>STAR_APT.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="StarCsvDataCollection.StarApt"/> filled in.</returns>
	public StarCsvDataCollection ParseStarApt(string filePath)
	{
		var result = new StarCsvDataCollection
		{
			StarApt = NasrCsvReader.ProcessLines(
				filePath,
				fields => new StarApt
				{
					EffDate = fields["EFF_DATE"],
					StarComputerCode = fields["STAR_COMPUTER_CODE"],
					Artcc = fields["ARTCC"],
					BodyName = fields["BODY_NAME"],
					AptBodySeq = NasrCsvReader.ParseInt(fields["BODY_SEQ"]),
					ArptId = fields["ARPT_ID"],
					RwyEndId = fields["RWY_END_ID"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>STAR_BASE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="StarCsvDataCollection.StarBase"/> filled in.</returns>
	public StarCsvDataCollection ParseStarBase(string filePath)
	{
		var result = new StarCsvDataCollection
		{
			StarBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new StarBase
				{
					EffDate = fields["EFF_DATE"],
					ArrivalName = fields["ARRIVAL_NAME"],
					AmendmentNo = fields["AMENDMENT_NO"],
					Artcc = fields["ARTCC"],
					StarAmendEffDate = fields["STAR_AMEND_EFF_DATE"],
					RnavFlag = fields["RNAV_FLAG"],
					StarComputerCode = fields["STAR_COMPUTER_CODE"],
					ServedArpt = fields["SERVED_ARPT"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>STAR_RTE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="StarCsvDataCollection.StarRte"/> filled in.</returns>
	public StarCsvDataCollection ParseStarRte(string filePath)
	{
		var result = new StarCsvDataCollection
		{
			StarRte = NasrCsvReader.ProcessLines(
				filePath,
				fields => new StarRte
				{
					EffDate = fields["EFF_DATE"],
					StarComputerCode = fields["STAR_COMPUTER_CODE"],
					Artcc = fields["ARTCC"],
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
/// Every parsed row of the NASR <c>STAR</c> CSV files, one list per file.
/// </summary>
public class StarCsvDataCollection
{
	/// <summary>The rows of <c>STAR_APT.csv</c>.</summary>
	public List<StarApt> StarApt { get; set; } = [];
	/// <summary>The rows of <c>STAR_BASE.csv</c>.</summary>
	public List<StarBase> StarBase { get; set; } = [];
	/// <summary>The rows of <c>STAR_RTE.csv</c>.</summary>
	public List<StarRte> StarRte { get; set; } = [];
}