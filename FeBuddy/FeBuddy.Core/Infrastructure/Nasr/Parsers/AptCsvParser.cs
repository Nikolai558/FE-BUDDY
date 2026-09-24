using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.AptCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

public class AptCsvParser
{
	public AptCsvDataCollection ParseAptArs(string filePath)
	{
		var result = new AptCsvDataCollection
		{
			AptArs = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AptArs
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					ArsRwyId = fields["RWY_ID"],
					ArsRwyEndId = fields["RWY_END_ID"],
					ArrestDeviceCode = fields["ARREST_DEVICE_CODE"],
				})
		};

		return result;
	}

	public AptCsvDataCollection ParseAptAtt(string filePath)
	{
		var result = new AptCsvDataCollection
		{
			AptAtt = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AptAtt
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					SkedSeqNo = NasrCsvReader.ParseInt(fields["SKED_SEQ_NO"]),
					Month = fields["MONTH"],
					Day = fields["DAY"],
					Hour = fields["HOUR"],
				})
		};

		return result;
	}

	public AptCsvDataCollection ParseAptBase(string filePath)
	{
		var result = new AptCsvDataCollection
		{
			AptBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AptBase
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					RegionCode = fields["REGION_CODE"],
					AdoCode = fields["ADO_CODE"],
					StateName = fields["STATE_NAME"],
					CountyName = fields["COUNTY_NAME"],
					CountyAssocState = fields["COUNTY_ASSOC_STATE"],
					ArptName = fields["ARPT_NAME"],
					OwnershipTypeCode = fields["OWNERSHIP_TYPE_CODE"],
					FacilityUseCode = fields["FACILITY_USE_CODE"],
					LatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
					LatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
					LatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
					LatHemis = fields["LAT_HEMIS"],
					BaseLatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
					LongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
					LongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
					LongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
					LongHemis = fields["LONG_HEMIS"],
					BaseLongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
					SurveyMethodCode = fields["SURVEY_METHOD_CODE"],
					Elev = NasrCsvReader.ParseNullableDouble(fields["ELEV"]),
					ElevMethodCode = fields["ELEV_METHOD_CODE"],
					MagVarn = NasrCsvReader.ParseNullableInt(fields["MAG_VARN"]),
					MagHemis = fields["MAG_HEMIS"],
					MagVarnYear = NasrCsvReader.ParseNullableInt(fields["MAG_VARN_YEAR"]),
					Tpa = NasrCsvReader.ParseNullableInt(fields["TPA"]),
					ChartName = fields["CHART_NAME"],
					DistCityToAirport = NasrCsvReader.ParseNullableInt(fields["DIST_CITY_TO_AIRPORT"]),
					DirectionCode = fields["DIRECTION_CODE"],
					Acreage = NasrCsvReader.ParseNullableInt(fields["ACREAGE"]),
					RespArtccId = fields["RESP_ARTCC_ID"],
					ComputerId = fields["COMPUTER_ID"],
					ArtccName = fields["ARTCC_NAME"],
					FssOnArptFlag = fields["FSS_ON_ARPT_FLAG"],
					FssId = fields["FSS_ID"],
					FssName = fields["FSS_NAME"],
					BasePhoneNo = fields["PHONE_NO"],
					TollFreeNo = fields["TOLL_FREE_NO"],
					AltFssId = fields["ALT_FSS_ID"],
					AltFssName = fields["ALT_FSS_NAME"],
					AltTollFreeNo = fields["ALT_TOLL_FREE_NO"],
					NotamId = fields["NOTAM_ID"],
					NotamFlag = fields["NOTAM_FLAG"],
					ActivationDate = fields["ACTIVATION_DATE"],
					ArptStatus = fields["ARPT_STATUS"],
					Far139TypeCode = fields["FAR_139_TYPE_CODE"],
					Far139CarrierSerCode = fields["FAR_139_CARRIER_SER_CODE"],
					ArffCertTypeDate = fields["ARFF_CERT_TYPE_DATE"],
					NaspCode = fields["NASP_CODE"],
					AspAnlysDtrmCode = fields["ASP_ANLYS_DTRM_CODE"],
					CustFlag = fields["CUST_FLAG"],
					LndgRightsFlag = fields["LNDG_RIGHTS_FLAG"],
					JointUseFlag = fields["JOINT_USE_FLAG"],
					MilLndgFlag = fields["MIL_LNDG_FLAG"],
					InspectMethodCode = fields["INSPECT_METHOD_CODE"],
					InspectorCode = fields["INSPECTOR_CODE"],
					LastInspection = fields["LAST_INSPECTION"],
					LastInfoResponse = fields["LAST_INFO_RESPONSE"],
					FuelTypes = fields["FUEL_TYPES"],
					AirframeRepairSerCode = fields["AIRFRAME_REPAIR_SER_CODE"],
					PwrPlantRepairSer = fields["PWR_PLANT_REPAIR_SER"],
					BottledOxyType = fields["BOTTLED_OXY_TYPE"],
					BulkOxyType = fields["BULK_OXY_TYPE"],
					LgtSked = fields["LGT_SKED"],
					BcnLgtSked = fields["BCN_LGT_SKED"],
					TwrTypeCode = fields["TWR_TYPE_CODE"],
					SegCircleMkrFlag = fields["SEG_CIRCLE_MKR_FLAG"],
					BcnLensColor = fields["BCN_LENS_COLOR"],
					LndgFeeFlag = fields["LNDG_FEE_FLAG"],
					MedicalUseFlag = fields["MEDICAL_USE_FLAG"],
					ArptPsnSource = fields["ARPT_PSN_SOURCE"],
					PositionSrcDate = fields["POSITION_SRC_DATE"],
					ArptElevSource = fields["ARPT_ELEV_SOURCE"],
					ElevationSrcDate = fields["ELEVATION_SRC_DATE"],
					ContrFuelAvbl = fields["CONTR_FUEL_AVBL"],
					TrnsStrgBuoyFlag = fields["TRNS_STRG_BUOY_FLAG"],
					TrnsStrgHgrFlag = fields["TRNS_STRG_HGR_FLAG"],
					TrnsStrgTieFlag = fields["TRNS_STRG_TIE_FLAG"],
					OtherServices = fields["OTHER_SERVICES"],
					WindIndcrFlag = fields["WIND_INDCR_FLAG"],
					IcaoId = fields["ICAO_ID"],
					MinOpNetwork = fields["MIN_OP_NETWORK"],
					UserFeeFlag = fields["USER_FEE_FLAG"],
					Cta = fields["CTA"],
				})
		};

		return result;
	}

	public AptCsvDataCollection ParseAptCon(string filePath)
	{
		var result = new AptCsvDataCollection
		{
			AptCon = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AptCon
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					Title = fields["TITLE"],
					Name = fields["NAME"],
					Address1 = fields["ADDRESS1"],
					Address2 = fields["ADDRESS2"],
					TitleCity = fields["TITLE_CITY"],
					State = fields["STATE"],
					ZipCode = fields["ZIP_CODE"],
					ZipPlusFour = fields["ZIP_PLUS_FOUR"],
					ConPhoneNo = fields["PHONE_NO"],
				})
		};

		return result;
	}

	public AptCsvDataCollection ParseAptRmk(string filePath)
	{
		var result = new AptCsvDataCollection
		{
			AptRmk = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AptRmk
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					LegacyElementNumber = fields["LEGACY_ELEMENT_NUMBER"],
					TabName = fields["TAB_NAME"],
					RefColName = fields["REF_COL_NAME"],
					Element = fields["ELEMENT"],
					RefColSeqNo = NasrCsvReader.ParseInt(fields["REF_COL_SEQ_NO"]),
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

	public AptCsvDataCollection ParseAptRwy(string filePath)
	{
		var result = new AptCsvDataCollection
		{
			AptRwy = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AptRwy
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					RwyRwyId = fields["RWY_ID"],
					RwyLen = NasrCsvReader.ParseInt(fields["RWY_LEN"]),
					RwyWidth = NasrCsvReader.ParseInt(fields["RWY_WIDTH"]),
					SurfaceTypeCode = fields["SURFACE_TYPE_CODE"],
					Cond = fields["COND"],
					TreatmentCode = fields["TREATMENT_CODE"],
					// ACN-PCN pavement classification fields: not present on every NASR
					// cycle's APT_RWY.csv, so read defensively rather than with the
					// dictionary indexer (see NasrCsvReader.GetField).
					PavementClassification = NasrCsvReader.GetField(fields, "PAVEMENT_CLASSIFICATION"),
					PcnPcrNumber = NasrCsvReader.ParseNullableInt(NasrCsvReader.GetField(fields, "PCN_PCR_NUMBER")),
					PavementTypeCode = NasrCsvReader.GetField(fields, "PAVEMENT_TYPE_CODE"),
					SubgradeStrengthCode = NasrCsvReader.GetField(fields, "SUBGRADE_STRENGTH_CODE"),
					TirePresCode = NasrCsvReader.GetField(fields, "TIRE_PRES_CODE"),
					DtrmMethodCode = NasrCsvReader.GetField(fields, "DTRM_METHOD_CODE"),
					RwyLgtCode = fields["RWY_LGT_CODE"],
					RwyLenSource = fields["RWY_LEN_SOURCE"],
					LengthSourceDate = fields["LENGTH_SOURCE_DATE"],
					GrossWtSw = NasrCsvReader.ParseNullableDouble(fields["GROSS_WT_SW"]),
					GrossWtDw = NasrCsvReader.ParseNullableDouble(fields["GROSS_WT_DW"]),
					GrossWtDtw = NasrCsvReader.ParseNullableDouble(fields["GROSS_WT_DTW"]),
					GrossWtDdtw = NasrCsvReader.ParseNullableDouble(fields["GROSS_WT_DDTW"]),
				})
		};

		return result;
	}

	public AptCsvDataCollection ParseAptRwyEnd(string filePath)
	{
		var result = new AptCsvDataCollection
		{
			AptRwyEnd = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AptRwyEnd
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					StateCode = fields["STATE_CODE"],
					ArptId = fields["ARPT_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					RwyEndRwyId = fields["RWY_ID"],
					RwyEndRwyEndId = fields["RWY_END_ID"],
					TrueAlignment = NasrCsvReader.ParseNullableInt(fields["TRUE_ALIGNMENT"]),
					IlsType = fields["ILS_TYPE"],
					RightHandTrafficPatFlag = fields["RIGHT_HAND_TRAFFIC_PAT_FLAG"],
					RwyMarkingTypeCode = fields["RWY_MARKING_TYPE_CODE"],
					RwyMarkingCond = fields["RWY_MARKING_COND"],
					RwyEndLatDeg = NasrCsvReader.ParseNullableInt(fields["RWY_END_LAT_DEG"]),
					RwyEndLatMin = NasrCsvReader.ParseNullableInt(fields["RWY_END_LAT_MIN"]),
					RwyEndLatSec = NasrCsvReader.ParseNullableDouble(fields["RWY_END_LAT_SEC"]),
					RwyEndLatHemis = fields["RWY_END_LAT_HEMIS"],
					RwyEndLatDecimal = NasrCsvReader.ParseNullableDouble(fields["LAT_DECIMAL"]),
					RwyEndLongDeg = NasrCsvReader.ParseNullableInt(fields["RWY_END_LONG_DEG"]),
					RwyEndLongMin = NasrCsvReader.ParseNullableInt(fields["RWY_END_LONG_MIN"]),
					RwyEndLongSec = NasrCsvReader.ParseNullableDouble(fields["RWY_END_LONG_SEC"]),
					RwyEndLongHemis = fields["RWY_END_LONG_HEMIS"],
					RwyEndLongDecimal = NasrCsvReader.ParseNullableDouble(fields["LONG_DECIMAL"]),
					RwyEndElev = NasrCsvReader.ParseNullableDouble(fields["RWY_END_ELEV"]),
					ThrCrossingHgt = NasrCsvReader.ParseNullableInt(fields["THR_CROSSING_HGT"]),
					VisualGlidePathAngle = NasrCsvReader.ParseNullableDouble(fields["VISUAL_GLIDE_PATH_ANGLE"]),
					DisplacedThrLatDeg = NasrCsvReader.ParseNullableInt(fields["DISPLACED_THR_LAT_DEG"]),
					DisplacedThrLatMin = NasrCsvReader.ParseNullableInt(fields["DISPLACED_THR_LAT_MIN"]),
					DisplacedThrLatSec = NasrCsvReader.ParseNullableDouble(fields["DISPLACED_THR_LAT_SEC"]),
					DisplacedThrLatHemis = fields["DISPLACED_THR_LAT_HEMIS"],
					LatDisplacedThrDecimal = NasrCsvReader.ParseNullableDouble(fields["LAT_DISPLACED_THR_DECIMAL"]),
					DisplacedThrLongDeg = NasrCsvReader.ParseNullableInt(fields["DISPLACED_THR_LONG_DEG"]),
					DisplacedThrLongMin = NasrCsvReader.ParseNullableInt(fields["DISPLACED_THR_LONG_MIN"]),
					DisplacedThrLongSec = NasrCsvReader.ParseNullableDouble(fields["DISPLACED_THR_LONG_SEC"]),
					DisplacedThrLongHemis = fields["DISPLACED_THR_LONG_HEMIS"],
					LongDisplacedThrDecimal = NasrCsvReader.ParseNullableDouble(fields["LONG_DISPLACED_THR_DECIMAL"]),
					DisplacedThrElev = NasrCsvReader.ParseNullableDouble(fields["DISPLACED_THR_ELEV"]),
					DisplacedThrLen = NasrCsvReader.ParseNullableInt(fields["DISPLACED_THR_LEN"]),
					TdzElev = NasrCsvReader.ParseNullableDouble(fields["TDZ_ELEV"]),
					VgsiCode = fields["VGSI_CODE"],
					RwyVisualRangeEquipCode = fields["RWY_VISUAL_RANGE_EQUIP_CODE"],
					RwyVsbyValueEquipFlag = fields["RWY_VSBY_VALUE_EQUIP_FLAG"],
					ApchLgtSystemCode = fields["APCH_LGT_SYSTEM_CODE"],
					RwyEndLgtsFlag = fields["RWY_END_LGTS_FLAG"],
					CntrlnLgtsAvblFlag = fields["CNTRLN_LGTS_AVBL_FLAG"],
					TdzLgtAvblFlag = fields["TDZ_LGT_AVBL_FLAG"],
					ObstnType = fields["OBSTN_TYPE"],
					ObstnMrkdCode = fields["OBSTN_MRKD_CODE"],
					FarPart77Code = fields["FAR_PART_77_CODE"],
					ObstnClncSlope = NasrCsvReader.ParseNullableInt(fields["OBSTN_CLNC_SLOPE"]),
					ObstnHgt = NasrCsvReader.ParseNullableInt(fields["OBSTN_HGT"]),
					DistFromThr = NasrCsvReader.ParseNullableInt(fields["DIST_FROM_THR"]),
					CntrlnOffset = NasrCsvReader.ParseNullableInt(fields["CNTRLN_OFFSET"]),
					CntrlnDirCode = fields["CNTRLN_DIR_CODE"],
					RwyGrad = NasrCsvReader.ParseNullableDouble(fields["RWY_GRAD"]),
					RwyGradDirection = fields["RWY_GRAD_DIRECTION"],
					RwyEndPsnSource = fields["RWY_END_PSN_SOURCE"],
					RwyEndPsnDate = fields["RWY_END_PSN_DATE"],
					RwyEndElevSource = fields["RWY_END_ELEV_SOURCE"],
					RwyEndElevDate = fields["RWY_END_ELEV_DATE"],
					DsplThrPsnSource = fields["DSPL_THR_PSN_SOURCE"],
					RwyEndDsplThrPsnDate = fields["RWY_END_DSPL_THR_PSN_DATE"],
					DsplThrElevSource = fields["DSPL_THR_ELEV_SOURCE"],
					RwyEndDsplThrElevDate = fields["RWY_END_DSPL_THR_ELEV_DATE"],
					TdzElevSource = fields["TDZ_ELEV_SOURCE"],
					RwyEndTdzElevDate = fields["RWY_END_TDZ_ELEV_DATE"],
					TkofRunAvbl = NasrCsvReader.ParseNullableInt(fields["TKOF_RUN_AVBL"]),
					TkofDistAvbl = NasrCsvReader.ParseNullableInt(fields["TKOF_DIST_AVBL"]),
					AcltStopDistAvbl = NasrCsvReader.ParseNullableInt(fields["ACLT_STOP_DIST_AVBL"]),
					LndgDistAvbl = NasrCsvReader.ParseNullableInt(fields["LNDG_DIST_AVBL"]),
					LahsoAld = NasrCsvReader.ParseNullableInt(fields["LAHSO_ALD"]),
					RwyEndIntersectLahso = fields["RWY_END_INTERSECT_LAHSO"],
					LahsoDesc = fields["LAHSO_DESC"],
					LahsoLat = fields["LAHSO_LAT"],
					LatLahsoDecimal = NasrCsvReader.ParseNullableDouble(fields["LAT_LAHSO_DECIMAL"]),
					LahsoLong = fields["LAHSO_LONG"],
					LongLahsoDecimal = NasrCsvReader.ParseNullableDouble(fields["LONG_LAHSO_DECIMAL"]),
					LahsoPsnSource = fields["LAHSO_PSN_SOURCE"],
					RwyEndLahsoPsnDate = fields["RWY_END_LAHSO_PSN_DATE"],
				})
		};

		return result;
	}

}

public class AptCsvDataCollection
{
	public List<AptArs> AptArs { get; set; } = [];
	public List<AptAtt> AptAtt { get; set; } = [];
	public List<AptBase> AptBase { get; set; } = [];
	public List<AptCon> AptCon { get; set; } = [];
	public List<AptRmk> AptRmk { get; set; } = [];
	public List<AptRwy> AptRwy { get; set; } = [];
	public List<AptRwyEnd> AptRwyEnd { get; set; } = [];
}