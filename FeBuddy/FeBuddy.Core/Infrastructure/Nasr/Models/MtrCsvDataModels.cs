namespace FeBuddy.Core.Infrastructure.Nasr.Models;

public class MtrCsvDataModel
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
		/// MTR Type Code
		/// _Src: All Mtr_*.csv files(ROUTE_TYPE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>IR=IFR _ VR=VFR</remarks>
		public string RouteTypeCode { get; set; }

		/// <summary>
		/// Route Identifier
		/// _Src: All Mtr_*.csv files(ROUTE_ID)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Example "012". Note: Combine this along with the ROUTE_TYPE_CODE for a unique MTR identifier Ex: "IR012".</remarks>
		public string RouteId { get; set; }

		/// <summary>
		/// Traversed ARTCCs
		/// _Src: All Mtr_*.csv files(ARTCC)
		/// _MaxLength: 80
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>List of ARTCC Idents that MTR traverses</remarks>
		public string? Artcc { get; set; }

	}
	#endregion

	#region Mtr_AGY Fields
	public class MtrAgy : CommonFields
	{
		/// <summary>
		/// MTR Agency Type Code
		/// _Src: MTR_AGY.csv(AGENCY_TYPE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>O=Originating _ S1=Scheduling-1 _ S2=Scheduling-2 _ S3=Scheduling-3 _ S4=Scheduling-4</remarks>
		public string AgencyType { get; set; }

		/// <summary>
		/// Agency Organization Name
		/// _Src: MTR_AGY.csv(AGENCY_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string AgencyName { get; set; }

		/// <summary>
		/// Agency Station
		/// _Src: MTR_AGY.csv(STATION)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Station { get; set; }

		/// <summary>
		/// Agency Address
		/// _Src: MTR_AGY.csv(ADDRESS)
		/// _MaxLength: 35
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Address { get; set; }

		/// <summary>
		/// Agency City
		/// _Src: MTR_AGY.csv(CITY)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? City { get; set; }

		/// <summary>
		/// Agency State Code
		/// _Src: MTR_AGY.csv(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? StateCode { get; set; }

		/// <summary>
		/// Agency Zip Code
		/// _Src: MTR_AGY.csv(ZIP_CODE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? ZipCode { get; set; }

		/// <summary>
		/// Agency Commercial Phone Number
		/// _Src: MTR_AGY.csv(COMMERCIAL_NO)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? CommercialNo { get; set; }

		/// <summary>
		/// Agency DSN Phone Number
		/// _Src: MTR_AGY.csv(DSN_NO)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? DsnNo { get; set; }

		/// <summary>
		/// Agency Hours
		/// _Src: MTR_AGY.csv(HOURS)
		/// _MaxLength: 175
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Hours { get; set; }

	}
	#endregion

	#region Mtr_BASE Fields
	public class MtrBase : CommonFields
	{
		/// <summary>
		/// FSS Stations Along Route
		/// _Src: MTR_BASE.csv(FSS)
		/// _MaxLength: 160
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>All FSSs Within 150nm of The Route</remarks>
		public string? Fss { get; set; }

		/// <summary>
		/// Times of Use Text Information
		/// _Src: MTR_BASE.csv(TIME_OF_USE)
		/// _MaxLength: 175
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? TimeOfUse { get; set; }

	}
	#endregion

	#region Mtr_PT Fields
	public class MtrPt : CommonFields
	{
		/// <summary>
		/// Point Sequencing Number
		/// _Src: MTR_PT.csv(ROUTE_PT_SEQ)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>In multiples of ten. Points are in order adapted for given MTR. Note: For key, use ROUTE_PT_ID instead of ROUTE_PT_SEQ.</remarks>
		public int RoutePtSeq { get; set; }

		/// <summary>
		/// Route Point Identifier
		/// _Src: MTR_PT.csv(ROUTE_PT_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Note: For key, use ROUTE_PT_ID instead of ROUTE_PT_SEQ.</remarks>
		public string RoutePtId { get; set; }

		/// <summary>
		/// The Next Sequential Route Point Identifier
		/// _Src: MTR_PT.csv(NEXT_ROUTE_PT_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? NextRoutePtId { get; set; }

		/// <summary>
		/// Segment Text
		/// _Src: MTR_PT.csv(SEGMENT_TEXT)
		/// _MaxLength: 228
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Concatenation of Segment Text preceded by the Segment Text Sequence Number</remarks>
		public string? SegmentText { get; set; }

		/// <summary>
		/// MTR Route Point Latitude Degrees
		/// _Src: MTR_PT.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatDeg { get; set; }

		/// <summary>
		/// MTR Route Point Latitude Minutes
		/// _Src: MTR_PT.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatMin { get; set; }

		/// <summary>
		/// MTR Route Point Latitude Seconds
		/// _Src: MTR_PT.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatSec { get; set; }

		/// <summary>
		/// MTR Route Point Latitude Hemisphere
		/// _Src: MTR_PT.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LatHemis { get; set; }

		/// <summary>
		/// MTR Route Point Latitude in Decimal Format
		/// _Src: MTR_PT.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatDecimal { get; set; }

		/// <summary>
		/// MTR Route Point Longitude Degrees
		/// _Src: MTR_PT.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongDeg { get; set; }

		/// <summary>
		/// MTR Route Point Longitude Minutes
		/// _Src: MTR_PT.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongMin { get; set; }

		/// <summary>
		/// MTR Route Point Longitude Seconds
		/// _Src: MTR_PT.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongSec { get; set; }

		/// <summary>
		/// MTR Route Point Longitude Hemisphere
		/// _Src: MTR_PT.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LongHemis { get; set; }

		/// <summary>
		/// MTR Route Point Longitude in Decimal Format
		/// _Src: MTR_PT.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongDecimal { get; set; }

		/// <summary>
		/// Navaid Identifier
		/// _Src: MTR_PT.csv(NAV_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? NavId { get; set; }

		/// <summary>
		/// Bearing of NAVAID from Point
		/// _Src: MTR_PT.csv(NAVAID_BEARING)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		public int? NavaidBearing { get; set; }

		/// <summary>
		/// Distance of NAVAID from Point
		/// _Src: MTR_PT.csv(NAVAID_DIST)
		/// _MaxLength: (4,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		public int? NavaidDist { get; set; }

	}
	#endregion

	#region Mtr_SOP Fields
	public class MtrSop : CommonFields
	{
		/// <summary>
		/// SOP Text Computer assigned Sequence Number
		/// _Src: MTR_SOP.csv(SOP_SEQ_NO)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int SopSeqNo { get; set; }

		/// <summary>
		/// Standard Operating Procedure Text
		/// _Src: MTR_SOP.csv(SOP_TEXT)
		/// _MaxLength: 100
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string SopText { get; set; }

	}
	#endregion

	#region Mtr_TERR Fields
	public class MtrTerr : CommonFields
	{
		/// <summary>
		/// TERRAIN Text Computer assigned Sequence Number
		/// _Src: MTR_TERR.csv(TERRAIN_SEQ_NO)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int TerrainSeqNo { get; set; }

		/// <summary>
		/// Terrain Following Operations Text
		/// _Src: MTR_TERR.csv(TERRAIN_TEXT)
		/// _MaxLength: 100
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string TerrainText { get; set; }

	}
	#endregion

	#region Mtr_WDTH Fields
	public class MtrWdth : CommonFields
	{
		/// <summary>
		/// WIDTH Text Computer assigned Sequence Number
		/// _Src: MTR_WDTH.csv(WIDTH_SEQ_NO)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int WidthSeqNo { get; set; }

		/// <summary>
		/// Route Width Description Text
		/// _Src: MTR_WDTH.csv(WIDTH_TEXT)
		/// _MaxLength: 100
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string WidthText { get; set; }

	}
	#endregion

}