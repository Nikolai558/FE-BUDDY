namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// All model properties for the Com_*.csv files. FLIGHT SERVICE STATION COMMUNICATION FACILITIES DATA
/// </summary>
public class ComCsvDataModel
{
	#region Common Fields
	/// <summary>The columns every <c>COM</c> file shares.</summary>
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
		/// Communications Outlet Ident
		/// _Src: All Com_*.csv files(COMM_LOC_ID)
		/// _MaxLength: 6
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>A 3-4 character alphanumeric identifier. COMM_TYPE RCAG do not currently have a 3-4 character identifier stored in NASR</remarks>
		public string? CommLocId { get; set; }

		/// <summary>
		/// Communication Outlet Type
		/// _Src: All Com_*.csv files(COMM_TYPE)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>RCAG=Remote Communications Air/Ground _ RCO=Remote Communication Outlet _ RCO1=Remote Communication Outlet (alternate site sharing same ID, e.g., one with NAVAID and one on airport property)</remarks>
		public string CommType { get; set; }

		/// <summary>
		/// Associated NAVAID Ident
		/// _Src: All Com_*.csv files(NAV_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Applies to RCO/RCO1 types only.</remarks>
		public string? NavId { get; set; }

		/// <summary>
		/// Associated NAVAID Type
		/// _Src: All Com_*.csv files(NAV_TYPE)
		/// _MaxLength: 25
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Applies to RCO/RCO1 Types Only. CONSOLAN=Low-Frequency Long-Distance NAVAID (Transoceanic) _ DME=Distance Measuring Equipment Only _ FAN MARKER=En Route Marker Beacon (FAN, low power FAN, Z Marker) _ MARINE NDB=Marine Non-Directional Beacon _ MARINE NDB/DME=Marine NDB with DME _ NDB=Non-Directional Beacon _ NDB/DME=NDB with DME _ TACAN=Tactical Air Navigation (Azimuth + Distance) _ UHF/NDB=UHF Non-Directional Beacon _ VOR=VHF Omnidirectional Range (Azimuth only) _ VORTAC=VOR + TACAN (Azimuth and DME) _ VOR/DME=VOR with DME _ VOT=VOR Test Facility</remarks>
		public string? NavType { get; set; }

		/// <summary>
		/// Communications Outlet City Name
		/// _Src: All Com_*.csv files(CITY)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>RCAG do not have an Associated City stored in NASR.</remarks>
		public string? City { get; set; }

		/// <summary>
		/// Associated State Code
		/// _Src: All Com_*.csv files(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Standard two letter abbreviation for US States and Territories.</remarks>
		public string? StateCode { get; set; }

		/// <summary>
		/// FAA Region Responsible for Communications Outlet
		/// _Src: All Com_*.csv files(REGION_CODE)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>AAL=Alaska _ ACE=Central _ AEA=Eastern _ AGL=Great Lakes _ ANE=New England _ ANM=Northwest Mountain _ ASO=Southern _ ASW=Southwest _ AWP=Western-Pacific</remarks>
		public string? RegionCode { get; set; }

		/// <summary>
		/// Country Code Communications Outlet is Located.
		/// _Src: All Com_*.csv files(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryCode { get; set; }

	}
	#endregion

	#region Com Fields
	/// <summary>One row of <c>COM.csv</c>.</summary>
	public class Com : CommonFields
	{
		/// <summary>
		/// Communications Outlet Name
		/// _Src: COM.csv(COMM_OUTLET_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The Communications Outlet Name is also used as the Communications Outlet Call.</remarks>
		public string CommOutletName { get; set; }

		/// <summary>
		/// Communications Outlet Latitude Degrees
		/// _Src: COM.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatDeg { get; set; }

		/// <summary>
		/// Communications Outlet Latitude Minutes
		/// _Src: COM.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatMin { get; set; }

		/// <summary>
		/// Communications Outlet Latitude Seconds
		/// _Src: COM.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatSec { get; set; }

		/// <summary>
		/// Communications Outlet Latitude Hemisphere
		/// _Src: COM.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LatHemis { get; set; }

		/// <summary>
		/// Communications Outlet Latitude in Decimal Format
		/// _Src: COM.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatDecimal { get; set; }

		/// <summary>
		/// Communications Outlet Longitude Degrees
		/// _Src: COM.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongDeg { get; set; }

		/// <summary>
		/// Communications Outlet Longitude Minutes
		/// _Src: COM.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongMin { get; set; }

		/// <summary>
		/// Communications Outlet Longitude Seconds
		/// _Src: COM.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongSec { get; set; }

		/// <summary>
		/// Communications Outlet Longitude Hemisphere
		/// _Src: COM.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LongHemis { get; set; }

		/// <summary>
		/// Communications Outlet Longitude in Decimal Format
		/// _Src: COM.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongDecimal { get; set; }

		/// <summary>
		/// Facility ID (Associated with RCO, RCO1, or RCAG)
		/// _Src: COM.csv(FACILITY_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>For RCO and RCO1: Facility ID is the associated Flight Service Station Ident _ For RCAG: Facility ID is the associated ARTCC</remarks>
		public string FacilityId { get; set; }

		/// <summary>
		/// Facility Name (Associated with RCO, RCO1, or RCAG)
		/// _Src: COM.csv(FACILITY_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>For RCO and RCO1: Facility Name is the associated Flight Service Station Name _ For RCAG: Facility Name is the associated ARTCC Name</remarks>
		public string FacilityName { get; set; }

		/// <summary>
		/// Associated Alternate Flight Service Station Ident
		/// _Src: COM.csv(ALT_FSS_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Applies to RCO/RCO1 types only</remarks>
		public string? AltFssId { get; set; }

		/// <summary>
		/// Associated Alternate Flight Service Station Name
		/// _Src: COM.csv(ALT_FSS_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Applies to RCO/RCO1 types only.</remarks>
		public string? AltFssName { get; set; }

		/// <summary>
		/// Operating Hours
		/// _Src: COM.csv(OPR_HRS)
		/// _MaxLength: 65
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Standard Time Zone - Applies to RCO/RCO1 types only.</remarks>
		public string? OprHrs { get; set; }

		/// <summary>
		/// Communication Outlet Status (Applies to RCO/RCO1 Types Only)
		/// _Src: COM.csv(COMM_STATUS_CODE)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>A=Operational IFR _ Q=To Be Commissioned _ Z=Decommissioned</remarks>
		public string? CommStatusCode { get; set; }

		/// <summary>
		/// STATUS Date of Communications Outlet
		/// _Src: COM.csv(COMM_STATUS_DATE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Applies to RCO/RCO1 types only.</remarks>
		public string? CommStatusDate { get; set; }

		/// <summary>
		/// Remark associated with Communications Outlet
		/// _Src: COM.csv(REMARK)
		/// _MaxLength: 1500
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Remark { get; set; }

	}
	#endregion

}