namespace FeBuddy.Core.Infrastructure.Nasr.Models;

public class FssCsvDataModel
{
	#region Common Fields
	public class CommonFields
	{
		/// <summary>
		/// Effective Date
		/// _Src: All Apt_*.csv files(EFF_DATE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The 28 Day NASR Subscription Effective Date in format �YYYY/MM/DD�.</remarks>
		public string EffDate { get; set; }

		/// <summary>
		/// Flight Service Station Identifier
		/// _Src: All Fss_*.csv files(FSS_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string FssId { get; set; }

		/// <summary>
		/// Flight Service Station Name
		/// _Src: All Fss_*.csv files(NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Associated City Name
		/// _Src: All Fss_*.csv files(CITY)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string City { get; set; }

		/// <summary>
		/// State Code
		/// _Src: All Fss_*.csv files(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Associated State Post Office Code standard two letter abbreviation for US States and Territories.</remarks>
		public string? StateCode { get; set; }

		/// <summary>
		/// Country Post Office Code
		/// _Src: All Fss_*.csv files(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryCode { get; set; }

	}
	#endregion

	#region Fss_BASE Fields
	public class FssBase : CommonFields
	{
		/// <summary>
		/// Last Date on which the Record was updated
		/// _Src: FSS_BASE.csv(UPDATE_DATE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? UpdateDate { get; set; }

		/// <summary>
		/// Facility Type
		/// _Src: FSS_BASE.csv(FSS_FAC_TYPE)
		/// _MaxLength: 8
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Flight Service Station (FSS), FS21 HUB Station (HUB) or FS21 Radio Service Area (RADIO)</remarks>
		public string FssFacType { get; set; }

		/// <summary>
		/// FSS Voice Call
		/// _Src: FSS_BASE.csv(VOICE_CALL)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Examples: "BOISE", "BARROW", "BURLINGTON"</remarks>
		public string? VoiceCall { get; set; }

		/// <summary>
		/// Flight Service Station Latitude Degrees
		/// _Src: FSS_BASE.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatDeg { get; set; }

		/// <summary>
		/// Flight Service Station Latitude Minutes
		/// _Src: FSS_BASE.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatMin { get; set; }

		/// <summary>
		/// Flight Service Station Latitude Seconds
		/// _Src: FSS_BASE.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatSec { get; set; }

		/// <summary>
		/// Flight Service Station Latitude Hemisphere
		/// _Src: FSS_BASE.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LatHemis { get; set; }

		/// <summary>
		/// Flight Service Station Latitude in Decimal Format
		/// _Src: FSS_BASE.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatDecimal { get; set; }

		/// <summary>
		/// Flight Service Station Longitude Degrees
		/// _Src: FSS_BASE.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongDeg { get; set; }

		/// <summary>
		/// Flight Service Station Longitude Minutes
		/// _Src: FSS_BASE.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongMin { get; set; }

		/// <summary>
		/// Flight Service Station Longitude Seconds
		/// _Src: FSS_BASE.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongSec { get; set; }

		/// <summary>
		/// Flight Service Station Longitude Hemisphere
		/// _Src: FSS_BASE.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LongHemis { get; set; }

		/// <summary>
		/// Flight Service Station Longitude in Decimal Format
		/// _Src: FSS_BASE.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongDecimal { get; set; }

		/// <summary>
		/// FSS Hours of Operation
		/// _Src: FSS_BASE.csv(OPR_HOURS)
		/// _MaxLength: 65
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string OprHours { get; set; }

		/// <summary>
		/// Status of Facility
		/// _Src: FSS_BASE.csv(FAC_STATUS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? FacStatus { get; set; }

		/// <summary>
		/// Alternate FSS Identifier
		/// _Src: FSS_BASE.csv(ALTERNATE_FSS)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Indicates the alternate FSS used when the primary facility lacks Circuit B teletype capability for transmitting/receiving flight plan messages</remarks>
		public string? AlternateFss { get; set; }

		/// <summary>
		/// Availability of Weather Radar
		/// _Src: FSS_BASE.csv(WEA_RADAR_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>(N)=None, Null/Empty=Available</remarks>
		public string? WeaRadarFlag { get; set; }

		/// <summary>
		/// Telephone Number used to reach FSS
		/// _Src: FSS_BASE.csv(PHONE_NO)
		/// _MaxLength: 16
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? PhoneNo { get; set; }

		/// <summary>
		/// Toll Free Telephone Number used to reach FSS
		/// _Src: FSS_BASE.csv(TOLL_FREE_NO)
		/// _MaxLength: 16
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? TollFreeNo { get; set; }

	}
	#endregion

	#region Fss_RMK Fields
	public class FssRmk : CommonFields
	{
		/// <summary>
		/// NASR Column Name Associated with Remark
		/// _Src: FSS_RMK.csv(REF_COL_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Indicates the specific NASR column name related to the remark; non-specific remarks are labeled as GENERAL_REMARK</remarks>
		public string RefColName { get; set; }

		/// <summary>
		/// Sequence number assigned to Reference Column Remark
		/// _Src: FSS_RMK.csv(REF_COL_SEQ_NO)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int RefColSeqNo { get; set; }

		/// <summary>
		/// Remark
		/// _Src: FSS_RMK.csv(REMARK)
		/// _MaxLength: 300
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Remark Text (Free Form Text that further describes a specific Information Item.)</remarks>
		public string Remark { get; set; }

	}
	#endregion

}