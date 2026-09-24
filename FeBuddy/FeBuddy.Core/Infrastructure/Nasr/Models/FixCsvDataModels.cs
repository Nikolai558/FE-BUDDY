namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// Row models for the NASR <c>FIX</c> CSV files (fixes and reporting points): one nested class per file.
/// </summary>
public class FixCsvDataModel
{
	#region Common Fields
	/// <summary>The columns every <c>FIX</c> file shares.</summary>
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
		/// Fixed Geographical Position Identifier
		/// _Src: All Fix_*.csv files(FIX_ID)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string FixId { get; set; }

		/// <summary>
		/// ICAO Region Code
		/// _Src: All Fix_*.csv files(ICAO_REGION_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>ICAO code where the first letter indicates the country and the second letter identifies the region within that country</remarks>
		public string IcaoRegionCode { get; set; }

		/// <summary>
		/// State Code
		/// _Src: All Fix_*.csv files(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Standard two-letter U.S. Postal Service abbreviation for U.S. states and territories</remarks>
		public string? StateCode { get; set; }

		/// <summary>
		/// Country Post Office Code
		/// _Src: All Fix_*.csv files(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryCode { get; set; }

	}
	#endregion

	#region Fix_BASE Fields
	/// <summary>One row of <c>FIX_BASE.csv</c>.</summary>
	public class FixBase : CommonFields
	{
		/// <summary>
		/// FIX Latitude Degrees
		/// _Src: FIX_BASE.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatDeg { get; set; }

		/// <summary>
		/// FIX Latitude Minutes
		/// _Src: FIX_BASE.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatMin { get; set; }

		/// <summary>
		/// FIX Latitude Seconds
		/// _Src: FIX_BASE.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatSec { get; set; }

		/// <summary>
		/// FIX Latitude Hemisphere
		/// _Src: FIX_BASE.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LatHemis { get; set; }

		/// <summary>
		/// FIX Latitude in Decimal Format
		/// _Src: FIX_BASE.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatDecimal { get; set; }

		/// <summary>
		/// FIX Longitude Degrees
		/// _Src: FIX_BASE.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongDeg { get; set; }

		/// <summary>
		/// FIX Longitude Minutes
		/// _Src: FIX_BASE.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongMin { get; set; }

		/// <summary>
		/// FIX Longitude Seconds
		/// _Src: FIX_BASE.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongSec { get; set; }

		/// <summary>
		/// FIX Longitude Hemisphere
		/// _Src: FIX_BASE.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LongHemis { get; set; }

		/// <summary>
		/// FIX Longitude in Decimal Format
		/// _Src: FIX_BASE.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongDecimal { get; set; }

		/// <summary>
		/// Previous Name(s) of the Fix before It was Renamed
		/// _Src: FIX_BASE.csv(FIX_ID_OLD)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? FixIdOld { get; set; }

		/// <summary>
		/// Charting Information Remarks
		/// _Src: FIX_BASE.csv(CHARTING_REMARK)
		/// _MaxLength: 38
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? ChartingRemark { get; set; }

		/// <summary>
		/// FIX Type of Use Code
		/// _Src: FIX_BASE.csv(FIX_USE_CODE)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>CN=Computer Navigation Fix _ MR=Military Reporting Point _ MW=Military Waypoint _ NRS=NRS Waypoint _ RADAR=Radar _ RP=Reporting Point _ VFR=VFR Waypoint _ WP=Waypoint</remarks>
		public string FixUseCode { get; set; }

		/// <summary>
		/// Denotes High ARTCC Area Of Jurisdiction.
		/// _Src: FIX_BASE.csv(ARTCC_ID_HIGH)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? ArtccIdHigh { get; set; }

		/// <summary>
		/// Denotes Low ARTCC Area Of Jurisdiction
		/// _Src: FIX_BASE.csv(ARTCC_ID_LOW)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string ArtccIdLow { get; set; }

		/// <summary>
		/// Pitch (Y = YES or N = NO)
		/// _Src: FIX_BASE.csv(PITCH_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Fairly obsolete system. Ref for more info: https://www.faa.gov/documentLibrary/media/Advisory_Circular/AC_90-99.pdf </remarks>
		public string PitchFlag { get; set; }

		/// <summary>
		/// Catch (Y = YES or N = NO)
		/// _Src: FIX_BASE.csv(CATCH_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Fairly obsolete system. Ref for more info: https://www.faa.gov/documentLibrary/media/Advisory_Circular/AC_90-99.pdf </remarks>
		public string CatchFlag { get; set; }

		/// <summary>
		/// SUA/ATCAA (Y = YES or N = NO)
		/// _Src: FIX_BASE.csv(SUA_ATCAA_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string SuaAtcaaFlag { get; set; }

		/// <summary>
		/// Fix Minimum Reception Altitude (MRA)
		/// _Src: FIX_BASE.csv(MIN_RECEP_ALT)
		/// _MaxLength: (5,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		public int? MinRecepAlt { get; set; }

		/// <summary>
		/// Compulsory FIX
		/// _Src: FIX_BASE.csv(COMPULSORY)
		/// _MaxLength: 8
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Identified as HIGH or LOW or LOW/HIGH. Null in this field identifies Non-Compulsory FIX.</remarks>
		public string? Compulsory { get; set; }

		/// <summary>
		/// Charts Fix is Referenced In
		/// _Src: FIX_BASE.csv(CHARTS)
		/// _MaxLength: 600
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Concatenated list of the information found in the FIX_CHRT file separated by a comma.</remarks>
		public string? Charts { get; set; }

	}
	#endregion

	#region Fix_CHRT Fields
	/// <summary>One row of <c>FIX_CHRT.csv</c>.</summary>
	public class FixChrt : CommonFields
	{
		/// <summary>
		/// Chart on Which Fix Is To Be Depicted
		/// _Src: FIX_CHRT.csv(CHARTING_TYPE_DESC)
		/// _MaxLength: 22
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Redundant data, as it is found in the FIX_BASE.csv, CHARTS field</remarks>
		public string ChartingTypeDesc { get; set; }

	}
	#endregion

	#region Fix_NAV Fields
	/// <summary>
	/// Close NAVAIDS to fix; Used to describe the fix location in relation to these navaids.
	/// </summary>
	public class FixNav : CommonFields
	{
		/// <summary>
		/// NAVAID Identifier
		/// _Src: FIX_NAV.csv(NAV_ID)
		/// _MaxLength: 6
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string NavId { get; set; }

		/// <summary>
		/// Facility/Navaid Type
		/// _Src: FIX_NAV.csv(NAV_TYPE)
		/// _MaxLength: 25
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string NavType { get; set; }

		/// <summary>
		/// Azimuth To Fix From Navaid
		/// _Src: FIX_NAV.csv(BEARING)
		/// _MaxLength: (5,2)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Bearing, Radial, Direction or Course depending on Facility Type</remarks>
		public double? Bearing { get; set; }

		/// <summary>
		/// DME Distance from Navaid/Facility
		/// _Src: FIX_NAV.csv(DISTANCE)
		/// _MaxLength: (7,2)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		public double? Distance { get; set; }

	}
	#endregion

}