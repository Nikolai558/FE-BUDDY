using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.StarCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

public class StarCsvParser
{
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

public class StarCsvDataCollection
{
	public List<StarApt> StarApt { get; set; } = [];
	public List<StarBase> StarBase { get; set; } = [];
	public List<StarRte> StarRte { get; set; } = [];
}