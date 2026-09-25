using static FeBuddy.Core.Infrastructure.Nasr.Models.AtcCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Reads the NASR <c>ATC</c> CSV files (air traffic control facilities), one file per method.
/// </summary>
public class AtcCsvParser
{
	/// <summary>Reads <c>ATC_ATIS.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="AtcCsvDataCollection.AtcAtis"/> filled in.</returns>
	public AtcCsvDataCollection ParseAtcAtis(string filePath)
	{
		var result = new AtcCsvDataCollection
		{
			AtcAtis = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AtcAtis
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					FacilityType = fields["FACILITY_TYPE"],
					StateCode = fields["STATE_CODE"],
					FacilityId = fields["FACILITY_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					AtisNo = NasrCsvReader.ParseInt(fields["ATIS_NO"]),
					Description = fields["DESCRIPTION"],
					AtisHrs = fields["ATIS_HRS"],
					AtisPhoneNo = fields["ATIS_PHONE_NO"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>ATC_BASE.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="AtcCsvDataCollection.AtcBase"/> filled in.</returns>
	public AtcCsvDataCollection ParseAtcBase(string filePath)
	{
		var result = new AtcCsvDataCollection
		{
			AtcBase = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AtcBase
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					FacilityType = fields["FACILITY_TYPE"],
					StateCode = fields["STATE_CODE"],
					FacilityId = fields["FACILITY_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					IcaoId = fields["ICAO_ID"],
					FacilityName = fields["FACILITY_NAME"],
					RegionCode = fields["REGION_CODE"],
					TwrOperatorCode = fields["TWR_OPERATOR_CODE"],
					TwrCall = fields["TWR_CALL"],
					TwrHrs = fields["TWR_HRS"],
					PrimaryApchRadioCall = fields["PRIMARY_APCH_RADIO_CALL"],
					ApchPProvider = fields["APCH_P_PROVIDER"],
					ApchPProvTypeCd = fields["APCH_P_PROV_TYPE_CD"],
					SecondaryApchRadioCall = fields["SECONDARY_APCH_RADIO_CALL"],
					ApchSProvider = fields["APCH_S_PROVIDER"],
					ApchSProvTypeCd = fields["APCH_S_PROV_TYPE_CD"],
					PrimaryDepRadioCall = fields["PRIMARY_DEP_RADIO_CALL"],
					DepPProvider = fields["DEP_P_PROVIDER"],
					DepPProvTypeCd = fields["DEP_P_PROV_TYPE_CD"],
					SecondaryDepRadioCall = fields["SECONDARY_DEP_RADIO_CALL"],
					DepSProvider = fields["DEP_S_PROVIDER"],
					DepSProvTypeCd = fields["DEP_S_PROV_TYPE_CD"],
					CtlFacApchDepCalls = fields["CTL_FAC_APCH_DEP_CALLS"],
					ApchDepOperCode = fields["APCH_DEP_OPER_CODE"],
					CtlPrvdingHrs = fields["CTL_PRVDING_HRS"],
					SecondaryCtlPrvdingHrs = fields["SECONDARY_CTL_PRVDING_HRS"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>ATC_RMK.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="AtcCsvDataCollection.AtcRmk"/> filled in.</returns>
	public AtcCsvDataCollection ParseAtcRmk(string filePath)
	{
		var result = new AtcCsvDataCollection
		{
			AtcRmk = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AtcRmk
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					FacilityType = fields["FACILITY_TYPE"],
					StateCode = fields["STATE_CODE"],
					FacilityId = fields["FACILITY_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					LegacyElementNumber = fields["LEGACY_ELEMENT_NUMBER"],
					TabName = fields["TAB_NAME"],
					RefColName = fields["REF_COL_NAME"],
					RemarkNo = NasrCsvReader.ParseInt(fields["REMARK_NO"]),
					Remark = fields["REMARK"],
				})
		};

		return result;
	}

	/// <summary>Reads <c>ATC_SVC.csv</c>.</summary>
	/// <param name="filePath">The full path of the file.</param>
	/// <returns>A collection with only <see cref="AtcCsvDataCollection.AtcSvc"/> filled in.</returns>
	public AtcCsvDataCollection ParseAtcSvc(string filePath)
	{
		var result = new AtcCsvDataCollection
		{
			AtcSvc = NasrCsvReader.ProcessLines(
				filePath,
				fields => new AtcSvc
				{
					EffDate = fields["EFF_DATE"],
					SiteNo = fields["SITE_NO"],
					SiteTypeCode = fields["SITE_TYPE_CODE"],
					FacilityType = fields["FACILITY_TYPE"],
					StateCode = fields["STATE_CODE"],
					FacilityId = fields["FACILITY_ID"],
					City = fields["CITY"],
					CountryCode = fields["COUNTRY_CODE"],
					CtlSvc = fields["CTL_SVC"],
				})
		};

		return result;
	}

}

/// <summary>
/// Every parsed row of the NASR <c>ATC</c> CSV files, one list per file.
/// </summary>
public class AtcCsvDataCollection
{
	/// <summary>The rows of <c>ATC_ATIS.csv</c>.</summary>
	public List<AtcAtis> AtcAtis { get; set; } = [];
	/// <summary>The rows of <c>ATC_BASE.csv</c>.</summary>
	public List<AtcBase> AtcBase { get; set; } = [];
	/// <summary>The rows of <c>ATC_RMK.csv</c>.</summary>
	public List<AtcRmk> AtcRmk { get; set; } = [];
	/// <summary>The rows of <c>ATC_SVC.csv</c>.</summary>
	public List<AtcSvc> AtcSvc { get; set; } = [];
}