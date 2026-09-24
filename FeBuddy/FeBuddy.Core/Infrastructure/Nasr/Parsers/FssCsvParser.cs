using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.FssCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

public class FssCsvParser
{
	public FssCsvDataCollection ParseFssBase(string filePath)
	{
		var result = new FssCsvDataCollection
		{
			FssBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new FssBase
				{
					EffDate = fields["EFF_DATE"],
					FssId = fields["FSS_ID"],
					Name = fields["NAME"],
					UpdateDate = fields["UPDATE_DATE"],
					FssFacType = fields["FSS_FAC_TYPE"],
					VoiceCall = fields["VOICE_CALL"],
					City = fields["CITY"],
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					LatDeg = NasrCsvReader.ParseInt(fields["LAT_DEG"]),
					LatMin = NasrCsvReader.ParseInt(fields["LAT_MIN"]),
					LatSec = NasrCsvReader.ParseDouble(fields["LAT_SEC"]),
					LatHemis = fields["LAT_HEMIS"],
					LatDecimal = NasrCsvReader.ParseDouble(fields["LAT_DECIMAL"]),
					LongDeg = NasrCsvReader.ParseInt(fields["LONG_DEG"]),
					LongMin = NasrCsvReader.ParseInt(fields["LONG_MIN"]),
					LongSec = NasrCsvReader.ParseDouble(fields["LONG_SEC"]),
					LongHemis = fields["LONG_HEMIS"],
					LongDecimal = NasrCsvReader.ParseDouble(fields["LONG_DECIMAL"]),
					OprHours = fields["OPR_HOURS"],
					FacStatus = fields["FAC_STATUS"],
					AlternateFss = fields["ALTERNATE_FSS"],
					WeaRadarFlag = fields["WEA_RADAR_FLAG"],
					PhoneNo = fields["PHONE_NO"],
					TollFreeNo = fields["TOLL_FREE_NO"],
				})
		};

		return result;
	}

	public FssCsvDataCollection ParseFssRmk(string filePath)
	{
		var result = new FssCsvDataCollection
		{
			FssRmk = NasrCsvReader.ProcessLines(
				filePath,
				fields => new FssRmk
				{
					EffDate = fields["EFF_DATE"],
					FssId = fields["FSS_ID"],
					Name = fields["NAME"],
					City = fields["CITY"],
					StateCode = fields["STATE_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					RefColName = fields["REF_COL_NAME"],
					RefColSeqNo = NasrCsvReader.ParseInt(fields["REF_COL_SEQ_NO"]),
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

}

public class FssCsvDataCollection
{
	public List<FssBase> FssBase { get; set; } = [];
	public List<FssRmk> FssRmk { get; set; } = [];
}