namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// Row models for the NASR <c>MAA</c> CSV files (miscellaneous activity areas): one nested class per file.
/// </summary>
public class MaaCsvDataModel
{
	#region Common Fields
	/// <summary>The columns every <c>MAA</c> file shares.</summary>
	public class CommonFields
	{
		/// <summary>
		/// Effective Date
		/// _Src: All Apt_*.csv files(EFF_DATE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The 28 Day NASR Subscription Effective Date in format "YYYY/MM/DD".</remarks>
		public string EffDate { get; set; }

		/// <summary>
		/// MISCELLANEOUS ACTIVITY AREA (MAA) Identifier
		/// _Src: All Maa_*.csv files(MAA_ID)
		/// _MaxLength: 6
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>MAA ID that uniquely identifies a Miscellaneous Activity Area</remarks>
		public string MaaId { get; set; }

	}
	#endregion

	#region Maa_BASE Fields
	/// <summary>One row of <c>MAA_BASE.csv</c>.</summary>
	public class MaaBase : CommonFields
	{
		/// <summary>
		/// MAA Type Name
		/// _Src: MAA_BASE.csv(MAA_TYPE_NAME)
		/// _MaxLength: 20
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Aerobatic Practice _ Glider _ Hang Glider _ Space Launch _ Ultralight _ Unmanned Aircraft _ Other</remarks>
		public string MaaTypeName { get; set; }

		/// <summary>
		/// NAVAID Facility Identifier with which MAA is Associated
		/// _Src: MAA_BASE.csv(NAV_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? NavId { get; set; }

		/// <summary>
		/// NAVAID Type
		/// _Src: MAA_BASE.csv(NAV_TYPE)
		/// _MaxLength: 25
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>CONSOLAN=Low-Frequency Long-Distance NAVAID (Transoceanic) _ DME=Distance Measuring Equipment Only _ FAN MARKER=En Route Marker Beacon (FAN, low power FAN, Z Marker) _ MARINE NDB=Marine Non-Directional Beacon _ MARINE NDB/DME=Marine NDB with DME _ NDB=Non-Directional Beacon _ NDB/DME=NDB with DME _ TACAN=Tactical Air Navigation (Azimuth + Distance) _ UHF/NDB=UHF Non-Directional Beacon _ VOR=VHF Omnidirectional Range (Azimuth only) _ VORTAC=VOR + TACAN (Azimuth and DME) _ VOR/DME=VOR with DME _ VOT=VOR Test Facility</remarks>
		public string? NavType { get; set; }

		/// <summary>
		/// Azimuth (Degrees) From NAVAID
		/// _Src: MAA_BASE.csv(NAV_RADIAL)
		/// _MaxLength: (5,2)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>0-359.99</remarks>
		public double? NavRadial { get; set; }

		/// <summary>
		/// Distance From NAVAID
		/// _Src: MAA_BASE.csv(NAV_DISTANCE)
		/// _MaxLength: (7,2)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Nautical Miles</remarks>
		public double? NavDistance { get; set; }

		/// <summary>
		/// State Code
		/// _Src: MAA_BASE.csv(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string StateCode { get; set; }

		/// <summary>
		/// City
		/// _Src: MAA_BASE.csv(CITY)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? City { get; set; }

		/// <summary>
		/// MAA Latitude (Formatted)
		/// _Src: MAA_BASE.csv(LATITUDE)
		/// _MaxLength: 14
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other MAA_*.csv files</remarks>
		public string? BaseLatitude { get; set; }

		/// <summary>
		/// MAA Longitude (Formatted)
		/// _Src: MAA_BASE.csv(LONGITUDE)
		/// _MaxLength: 15
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other MAA_*.csv files</remarks>
		public string? BaseLongitude { get; set; }

		/// <summary>
		/// LIST of Landing Facility Identifiers with which MAA is Associated
		/// _Src: MAA_BASE.csv(ARPT_IDS)
		/// _MaxLength: 50
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? ArptIds { get; set; }

		/// <summary>
		/// Nearest Airport ID
		/// _Src: MAA_BASE.csv(NEAREST_ARPT)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Only Applies to Space Launch Activity Areas</remarks>
		public string? NearestArpt { get; set; }

		/// <summary>
		/// Nearest Airport Distance
		/// _Src: MAA_BASE.csv(NEAREST_ARPT_DIST)
		/// _MaxLength: (7,2)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>In Nautical Miles Only Applies to Space Launch Activity Areas</remarks>
		public double? NearestArptDist { get; set; }

		/// <summary>
		/// Nearest Airport Direction
		/// _Src: MAA_BASE.csv(NEAREST_ARPT_DIR)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Only Applies to Space Launch Activity Areas</remarks>
		public string? NearestArptDir { get; set; }

		/// <summary>
		/// MAA Area Name
		/// _Src: MAA_BASE.csv(MAA_NAME)
		/// _MaxLength: 120
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? MaaName { get; set; }

		/// <summary>
		/// MAA Maximum Altitude Allowed
		/// _Src: MAA_BASE.csv(MAX_ALT)
		/// _MaxLength: 8
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? MaxAlt { get; set; }

		/// <summary>
		/// MAA Minimum Altitude Allowed
		/// _Src: MAA_BASE.csv(MIN_ALT)
		/// _MaxLength: 8
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? MinAlt { get; set; }

		/// <summary>
		/// MAA Area Radius
		/// _Src: MAA_BASE.csv(MAA_RADIUS)
		/// _MaxLength: (4,2)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>In Nautical Miles from Center Point</remarks>
		public double? MaaRadius { get; set; }

		/// <summary>
		/// Additional Descriptive Text for MAA Area
		/// _Src: MAA_BASE.csv(DESCRIPTION)
		/// _MaxLength: 450
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// MAA Use Description
		/// _Src: MAA_BASE.csv(MAA_USE)
		/// _MaxLength: 8
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? MaaUse { get; set; }

		/// <summary>
		/// Check for NOTAMs
		/// _Src: MAA_BASE.csv(CHECK_NOTAMS)
		/// _MaxLength: 50
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Only Applies to Space Launch Activity Areas</remarks>
		public string? CheckNotams { get; set; }

		/// <summary>
		/// Times of Use Description
		/// _Src: MAA_BASE.csv(TIME_OF_USE)
		/// _MaxLength: 300
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? TimeOfUse { get; set; }

		/// <summary>
		/// MAA User Group Name and Description
		/// _Src: MAA_BASE.csv(USER_GROUP_NAME)
		/// _MaxLength: 300
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? UserGroupName { get; set; }

	}
	#endregion

	#region Maa_CON Fields
	/// <summary>One row of <c>MAA_CON.csv</c>.</summary>
	public class MaaCon : CommonFields
	{
		/// <summary>
		/// Frequency Sequence Number
		/// _Src: MAA_CON.csv(FREQ_SEQ)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>Unique Sequence number for Frequency Contact entries</remarks>
		public int FreqSeq { get; set; }

		/// <summary>
		/// Contact Facility Identifier
		/// _Src: MAA_CON.csv(FAC_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? FacId { get; set; }

		/// <summary>
		/// Contact Facility Name
		/// _Src: MAA_CON.csv(FAC_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string FacName { get; set; }

		/// <summary>
		/// Commercial Frequency
		/// _Src: MAA_CON.csv(COMMERCIAL_FREQ)
		/// _MaxLength: (7,3)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double CommercialFreq { get; set; }

		/// <summary>
		/// Commercial Chart Flag
		/// _Src: MAA_CON.csv(COMMERCIAL_CHART_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? CommercialChartFlag { get; set; }

		/// <summary>
		/// Military Frequency
		/// _Src: MAA_CON.csv(MIL_FREQ)
		/// _MaxLength: (7,3)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		public double? MilFreq { get; set; }

		/// <summary>
		/// Military Chart Flag
		/// _Src: MAA_CON.csv(MIL_CHART_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? MilChartFlag { get; set; }

	}
	#endregion

	#region Maa_RMK Fields
	/// <summary>One row of <c>MAA_RMK.csv</c>.</summary>
	public class MaaRmk : CommonFields
	{
		/// <summary>
		/// NASR table associated with Remark
		/// _Src: MAA_RMK.csv(TAB_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string TabName { get; set; }

		/// <summary>
		/// Reference Column Name
		/// _Src: MAA_RMK.csv(REF_COL_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>NASR Column name associated with Remark. Non-specific remarks are identified as GENERAL_REMARK</remarks>
		public string RefColName { get; set; }

		/// <summary>
		/// Reference Column Sequence Number
		/// _Src: MAA_RMK.csv(REF_COL_SEQ_NO)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>Sequence number assigned to Reference Column Remark</remarks>
		public int RefColSeqNo { get; set; }

		/// <summary>
		/// Remark
		/// _Src: MAA_RMK.csv(REMARK)
		/// _MaxLength: 300
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Free Form Text that further describes a specific Information Item</remarks>
		public string Remark { get; set; }

	}
	#endregion

	#region Maa_SHP Fields
	/// <summary>One row of <c>MAA_SHP.csv</c>.</summary>
	public class MaaShp : CommonFields
	{
		/// <summary>
		/// Polygon Sequence Number
		/// _Src: MAA_SHP.csv(POINT_SEQ)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>Unique Sequence number for MAA Polygon Coordinates</remarks>
		public int PointSeq { get; set; }

		/// <summary>
		/// MAA Polygon Coordinate Latitude (Formatted)
		/// _Src: MAA_SHP.csv(LATITUDE)
		/// _MaxLength: 14
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other MAA_*.csv files</remarks>
		public string ShpLatitude { get; set; }

		/// <summary>
		/// MAA Polygon Coordinate Longitude (Formatted)
		/// _Src: MAA_SHP.csv(LONGITUDE)
		/// _MaxLength: 15
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other MAA_*.csv files</remarks>
		public string ShpLongitude { get; set; }

	}
	#endregion

}