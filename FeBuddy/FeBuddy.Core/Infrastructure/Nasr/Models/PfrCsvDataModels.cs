namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// Row models for the NASR <c>PFR</c> CSV files (preferred routes): one nested class per file.
/// </summary>
public class PfrCsvDataModel
{
	#region Common Fields
	/// <summary>The columns every <c>PFR</c> file shares.</summary>
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
		/// Origin Facility Identifier
		/// _Src: All Pfr_*.csv files(ORIGIN_ID)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Represents the location identifier of the origin facility; depending on the NAR type and direction, this may be a coastal fix or an inland NAVAID or fix</remarks>
		public string OriginId { get; set; }

		/// <summary>
		/// Destination Facility Location Identifier
		/// _Src: All Pfr_*.csv files(DSTN_ID)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Represents the location identifier of the destination facility; depending on the NAR type and direction, this may be an airport, coastal fix, or inland NAVAID or fix</remarks>
		public string DstnId { get; set; }

		/// <summary>
		/// Preferred Route Description Type Code
		/// _Src: All Pfr_*.csv files(PFR_TYPE_CODE)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>L=Low Altitude _ H=High Altitude _ LPD=Low Altitude Preferred Direction _ HPD=High Altitude Preferred Direction _ SLD=Special Low Altitude Directional _ SHD=Special High Altitude Directional _ TEC=Tower Enroute Control _ NAR=North American Route</remarks>
		public string PfrTypeCode { get; set; }

		/// <summary>
		/// Route Identifier Sequence Number (1-99)
		/// _Src: All Pfr_*.csv files(ROUTE_NO)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int RouteNo { get; set; }

	}
	#endregion

	#region Pfr_BASE Fields
	/// <summary>One row of <c>PFR_BASE.csv</c>.</summary>
	public class PfrBase : CommonFields
	{
		/// <summary>
		/// Origin Facility Associated City Name
		/// _Src: PFR_BASE.csv(ORIGIN_CITY)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? OriginCity { get; set; }

		/// <summary>
		/// Origin Facility Associated State Code
		/// _Src: PFR_BASE.csv(ORIGIN_STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>This is the two letter state ID of the Origin Facility location. NULL if outside the US.</remarks>
		public string? OriginStateCode { get; set; }

		/// <summary>
		/// Country Code of the Origin Facility Located
		/// _Src: PFR_BASE.csv(ORIGIN_COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string OriginCountryCode { get; set; }

		/// <summary>
		/// Destination Facility Associated City Name
		/// _Src: PFR_BASE.csv(DSTN_CITY)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? DstnCity { get; set; }

		/// <summary>
		/// Destination Facility Associated State Code
		/// _Src: PFR_BASE.csv(DSTN_STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>This is the two letter state ID of the Destination Facility location. NULL if outside the US</remarks>
		public string? DstnStateCode { get; set; }

		/// <summary>
		/// Country Code of the Destination Facility Located
		/// _Src: PFR_BASE.csv(DSTN_COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string DstnCountryCode { get; set; }

		/// <summary>
		/// Preferred Route Area Description
		/// _Src: PFR_BASE.csv(SPECIAL_AREA_DESCRIP)
		/// _MaxLength: 75
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? SpecialAreaDescrip { get; set; }

		/// <summary>
		/// Preferred Route Altitude Description
		/// _Src: PFR_BASE.csv(ALT_DESCRIP)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? AltDescrip { get; set; }

		/// <summary>
		/// Aircraft Allowed/Limitations Description
		/// _Src: PFR_BASE.csv(AIRCRAFT)
		/// _MaxLength: 50
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other PFR_*.csv files</remarks>
		public string? BaseAircraft { get; set; }

		/// <summary>
		/// Effective Hours
		/// _Src: PFR_BASE.csv(HOURS)
		/// _MaxLength: 15
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>(GMT) Description * All Preferred IFR Routes are in Effect Continuously Unless Otherwise Noted</remarks>
		public string? Hours { get; set; }

		/// <summary>
		/// Route Direction Limitations Description
		/// _Src: PFR_BASE.csv(ROUTE_DIR_DESCRIP)
		/// _MaxLength: 20
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? RouteDirDescrip { get; set; }

		/// <summary>
		/// Preferred Route Designator if applicable
		/// _Src: PFR_BASE.csv(DESIGNATOR)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Designator { get; set; }

		/// <summary>
		/// North American Route Type 
		/// _Src: PFR_BASE.csv(NAR_TYPE)
		/// _MaxLength: 20
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>COMMON, NON-COMMON</remarks>
		public string? NarType { get; set; }

		/// <summary>
		/// Inland NAV Facility or Fix
		/// _Src: PFR_BASE.csv(INLAND_FAC_FIX)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Represents the inland NAV facility or fix used in North American Routes; functions as the origin on COMMON EASTBOUND and NON-COMMON routes (Eastbound or Westbound), and as the destination on COMMON WESTBOUND</remarks>
		public string? InlandFacFix { get; set; }

		/// <summary>
		/// Coastal Fix
		/// _Src: PFR_BASE.csv(COASTAL_FIX)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Represents the coastal fix used in North American Routes; serves as the origin on COMMON WESTBOUND routes and as the destination on COMMON EASTBOUND routes</remarks>
		public string? CoastalFix { get; set; }

		/// <summary>
		/// Destination
		/// _Src: PFR_BASE.csv(DESTINATION)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>North American Route Destination for NON_COMMON (Eastbound or Westbound).</remarks>
		public string? Destination { get; set; }

		/// <summary>
		/// Preferred Route String
		/// _Src: PFR_BASE.csv(ROUTE_STRING)
		/// _MaxLength: 300
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Represents the formatted route string used in preferred routing. Canadian DPs and STARs are denoted generically as "-DP" and "-STAR"; refer to Canadian Aeronautical Data for the correct amendment number when filing</remarks>
		public string? BaseRouteString { get; set; }

	}
	#endregion

	#region Pfr_RMT_FMT Fields
	/// <summary>One row of <c>PFR_RMT_FMT.csv</c>.</summary>
	public class PfrRmtFmt : CommonFields
	{
		/// <summary>
		/// Origin Facility Identifier
		/// _Src: PFR_RMT_FMT.csv(ORIG)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Identifies the origin facility in a North American Route (NAR); based on NAR type and direction, this may be a coastal fix or an inland NAV facility or fix</remarks>
		public string Orig { get; set; }

		/// <summary>
		/// Preferred Route String
		/// _Src: PFR_RMT_FMT.csv(ROUTE STRING)
		/// _MaxLength: 300
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Represents the formatted route string used in preferred routing. Canadian DPs and STARs are denoted generically as "-DP" and "-STAR"; refer to Canadian Aeronautical Data for the correct amendment number when filing</remarks>
		public string? RmtFmtRouteString { get; set; }

		/// <summary>
		/// Destination Facility Identifier
		/// _Src: PFR_RMT_FMT.csv(DEST)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Destination Facility Location Identifier (Depending on NAR Type and Direction, Destination ID Is either Airport, Coastal Fix or Inland NAV Facility or Fix)</remarks>
		public string Dest { get; set; }

		/// <summary>
		/// Effective Hours
		/// _Src: PFR_RMT_FMT.csv(HOURS1)
		/// _MaxLength: 15
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Describes the effective hours for the route in GMT. All preferred IFR routes are in effect continuously unless otherwise noted</remarks>
		public string? Hours1 { get; set; }

		/// <summary>
		/// Type Code of Preferred Route Description
		/// _Src: PFR_RMT_FMT.csv(TYPE)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>L=Low Altitude _ H=High Altitude _ LPD=Low Altitude Preferred Direction _ HPD=High Altitude Preferred Direction _ SLD=Special Low Altitude Directional _ SHD=Special High Altitude Directional _ TEC=Tower Enroute Control _ NAR=North American Route</remarks>
		public string Type { get; set; }

		/// <summary>
		/// Preferred Route Area Description
		/// _Src: PFR_RMT_FMT.csv(AREA)
		/// _MaxLength: 75
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Area { get; set; }

		/// <summary>
		/// Preferred Route Altitude Description
		/// _Src: PFR_RMT_FMT.csv(ALTITUDE)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Altitude { get; set; }

		/// <summary>
		/// Aircraft Allowed/Limitations Description
		/// _Src: PFR_RMT_FMT.csv(AIRCRAFT)
		/// _MaxLength: 50
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other PFR_*.csv files</remarks>
		public string? RmtFmtAircraft { get; set; }

		/// <summary>
		/// Route Direction Limitations Description
		/// _Src: PFR_RMT_FMT.csv(DIRECTION)
		/// _MaxLength: 20
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Direction { get; set; }

		/// <summary>
		/// Route Identifier Sequence Number
		/// _Src: PFR_RMT_FMT.csv(SEQ)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>1-99</remarks>
		public int Seq { get; set; }

		/// <summary>
		/// Departure ARTCC
		/// _Src: PFR_RMT_FMT.csv(DCNTR)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Dcntr { get; set; }

		/// <summary>
		/// Arrival ARTCC
		/// _Src: PFR_RMT_FMT.csv(ACNTR)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Acntr { get; set; }

	}
	#endregion

	#region Pfr_SEG Fields
	/// <summary>One row of <c>PFR_SEG.csv</c>.</summary>
	public class PfrSeg : CommonFields
	{
		/// <summary>
		/// Segment Sequence Identifier
		/// _Src: PFR_SEG.csv(SEGMENT_SEQ)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>A sequencing number in multiples of five for each SEG_VALUE. Segment Values are in order adapted for each Preferred Route.</remarks>
		public int SegmentSeq { get; set; }

		/// <summary>
		/// Segment ID Value
		/// _Src: PFR_SEG.csv(SEG_VALUE)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The Segment ID Value for each Element of the Route String from PFR_BASE</remarks>
		public string SegValue { get; set; }

		/// <summary>
		/// Segment Type
		/// _Src: PFR_SEG.csv(SEG_TYPE)
		/// _MaxLength: 6
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The Segment Type of the Segment ID Value</remarks>
		public string SegType { get; set; }

		/// <summary>
		/// State Code
		/// _Src: PFR_SEG.csv(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Segment Values outside the US or Types AIRWAY, DP or STAR are NULL.</remarks>
		public string? StateCode { get; set; }

		/// <summary>
		/// Country Code
		/// _Src: PFR_SEG.csv(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Segment Value Types AIRWAY, DP or STAR are NULL</remarks>
		public string? CountryCode { get; set; }

		/// <summary>
		/// Icao Region Code
		/// _Src: PFR_SEG.csv(ICAO_REGION_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>This is the two letter ICAO Region Code for FIX Segment Types only</remarks>
		public string? IcaoRegionCode { get; set; }

		/// <summary>
		/// NAVAID Type
		/// _Src: PFR_SEG.csv(NAV_TYPE)
		/// _MaxLength: 25
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Specific NAVAID Type for Segment Value Types NAVAID, RADIAL or FRD.</remarks>
		public string? NavType { get; set; }

		/// <summary>
		/// Next Segment ID
		/// _Src: PFR_SEG.csv(NEXT_SEG)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>The Segment ID Value of the Element that directly follows the current Segment Value</remarks>
		public string? NextSeg { get; set; }

	}
	#endregion

}