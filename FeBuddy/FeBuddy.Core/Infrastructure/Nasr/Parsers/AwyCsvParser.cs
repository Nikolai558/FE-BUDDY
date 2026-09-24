using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.AwyCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

public class AwyCsvParser
{
	public AwyCsvDataCollection ParseAwyBase(string filePath)
	{
		var result = new AwyCsvDataCollection
		{
			AwyBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AwyBase
				{
					EffDate = fields["EFF_DATE"],
					Regulatory = fields["REGULATORY"],
					AwyDesignation = fields["AWY_DESIGNATION"],
					AwyLocation = fields["AWY_LOCATION"],
					AwyId = fields["AWY_ID"],
					UpdateDate = fields["UPDATE_DATE"],
					BaseRemark = fields["REMARK"],
					AirwayString = fields["AIRWAY_STRING"],
				})
		};

		return result;
	}

	public AwyCsvDataCollection ParseAwySegAlt(string filePath)
	{
		var result = new AwyCsvDataCollection
		{
			AwySegAlt = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AwySegAlt
				{
					EffDate = fields["EFF_DATE"],
					Regulatory = fields["REGULATORY"],
					AwyLocation = fields["AWY_LOCATION"],
					AwyId = fields["AWY_ID"],
					PointSeq = NasrCsvReader.ParseInt(fields["POINT_SEQ"]),
					FromPoint = fields["FROM_POINT"],
					FromPtType = fields["FROM_PT_TYPE"],
					NavName = fields["NAV_NAME"],
					NavCity = fields["NAV_CITY"],
					Artcc = fields["ARTCC"],
					IcaoRegionCode = fields["ICAO_REGION_CODE"],
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					ToPoint = fields["TO_POINT"],
					MagCourse = NasrCsvReader.ParseNullableDouble(fields["MAG_COURSE"]),
					OppMagCourse = NasrCsvReader.ParseNullableDouble(fields["OPP_MAG_COURSE"]),
					MagCourseDist = NasrCsvReader.ParseNullableDouble(fields["MAG_COURSE_DIST"]),
					ChgovrPt = fields["CHGOVR_PT"],
					ChgovrPtName = fields["CHGOVR_PT_NAME"],
					ChgovrPtDist = NasrCsvReader.ParseNullableInt(fields["CHGOVR_PT_DIST"]),
					AwySegGapFlag = fields["AWY_SEG_GAP_FLAG"],
					SignalGapFlag = fields["SIGNAL_GAP_FLAG"],
					Dogleg = fields["DOGLEG"],
					NextMeaPt = fields["NEXT_MEA_PT"],
					MinEnrouteAlt = NasrCsvReader.ParseNullableInt(fields["MIN_ENROUTE_ALT"]),
					MinEnrouteAltDir = fields["MIN_ENROUTE_ALT_DIR"],
					MinEnrouteAltOpposite = NasrCsvReader.ParseNullableInt(fields["MIN_ENROUTE_ALT_OPPOSITE"]),
					MinEnrouteAltOppositeDir = fields["MIN_ENROUTE_ALT_OPPOSITE_DIR"],
					GpsMinEnrouteAlt = NasrCsvReader.ParseNullableInt(fields["GPS_MIN_ENROUTE_ALT"]),
					GpsMinEnrouteAltDir = fields["GPS_MIN_ENROUTE_ALT_DIR"],
					GpsMinEnrouteAltOpposite = NasrCsvReader.ParseNullableInt(fields["GPS_MIN_ENROUTE_ALT_OPPOSITE"]),
					GpsMeaOppositeDir = fields["GPS_MEA_OPPOSITE_DIR"],
					DdIruMea = NasrCsvReader.ParseNullableInt(fields["DD_IRU_MEA"]),
					DdIruMeaDir = fields["DD_IRU_MEA_DIR"],
					DdIMeaOpposite = NasrCsvReader.ParseNullableInt(fields["DD_I_MEA_OPPOSITE"]),
					DdIMeaOppositeDir = fields["DD_I_MEA_OPPOSITE_DIR"],
					MinObstnClncAlt = NasrCsvReader.ParseNullableInt(fields["MIN_OBSTN_CLNC_ALT"]),
					MinCrossAlt = NasrCsvReader.ParseNullableInt(fields["MIN_CROSS_ALT"]),
					MinCrossAltDir = fields["MIN_CROSS_ALT_DIR"],
					MinCrossAltNavPt = fields["MIN_CROSS_ALT_NAV_PT"],
					MinCrossAltOpposite = NasrCsvReader.ParseNullableInt(fields["MIN_CROSS_ALT_OPPOSITE"]),
					MinCrossAltOppositeDir = fields["MIN_CROSS_ALT_OPPOSITE_DIR"],
					MinRecepAlt = NasrCsvReader.ParseNullableInt(fields["MIN_RECEP_ALT"]),
					MaxAuthAlt = NasrCsvReader.ParseNullableInt(fields["MAX_AUTH_ALT"]),
					MeaGap = fields["MEA_GAP"],
					ReqdNavPerformance = NasrCsvReader.ParseNullableDouble(fields["REQD_NAV_PERFORMANCE"]),
					SegAltRemark = fields["REMARK"],
				})
		};

		return result;
	}

}

public class AwyCsvDataCollection
{
	public List<AwyBase> AwyBase { get; set; } = [];
	public List<AwySegAlt> AwySegAlt { get; set; } = [];
}