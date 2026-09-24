using System;
using System.Collections.Generic;
using System.IO;
using static FeBuddy.Core.Infrastructure.Nasr.Models.ComCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

public class ComCsvParser
{
	public ComCsvDataCollection ParseCom(string filePath)
	{
		var result = new ComCsvDataCollection
		{
			Com = NasrCsvReader.ProcessLines(
				filePath,
				fields => new Com
				{
					EffDate = fields["EFF_DATE"],
					CommLocId = fields["COMM_LOC_ID"],
					CommType = fields["COMM_TYPE"],
					NavId = fields["NAV_ID"],
					NavType = fields["NAV_TYPE"],
					City = fields["CITY"],
					StateCode = fields["STATE_CODE"],
					RegionCode = fields["REGION_CODE"],
					CountryCode = fields["COUNTRY_CODE"],
					CommOutletName = fields["COMM_OUTLET_NAME"],
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
					FacilityId = fields["FACILITY_ID"],
					FacilityName = fields["FACILITY_NAME"],
					AltFssId = fields["ALT_FSS_ID"],
					AltFssName = fields["ALT_FSS_NAME"],
					OprHrs = fields["OPR_HRS"],
					CommStatusCode = fields["COMM_STATUS_CODE"],
					CommStatusDate = fields["COMM_STATUS_DATE"],
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

}

public class ComCsvDataCollection
{
	public List<Com> Com { get; set; } = [];
}