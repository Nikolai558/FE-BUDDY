namespace FeBuddy.Core.Infrastructure.Nasr.Models;

public class StarCsvDataModel
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
		/// STAR Computer Code
		/// _Src: All Star_*.csv files(STAR_COMPUTER_CODE)
		/// _MaxLength: 12
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>FAA-Assigned Computer Identifier for the STAR. EX. GLAND.BLUMS5</remarks>
		public string StarComputerCode { get; set; }

		/// <summary>
		/// ARTCCs Responsible
		/// _Src: All Star_*.csv files(ARTCC)
		/// _MaxLength: 12
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>List of all Responsible ARTCCs based on Airports Served</remarks>
		public string? Artcc { get; set; }

	}
	#endregion

	#region Star_APT Fields
	public class StarApt : CommonFields
	{
		/// <summary>
		/// Body Name
		/// _Src: STAR_APT.csv(BODY_NAME)
		/// _MaxLength: 110
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Name of the airway or route segment associated with the airport or runway end; defined by the first and last fix of the segment</remarks>
		public string BodyName { get; set; }

		/// <summary>
		/// Aditional Unique Body Sequence Identifier
		/// _Src: STAR_APT.csv(BODY_SEQ)
		/// _MaxLength: (1,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other STAR_*.csv files. In the rare case that Body Name is not Unique for a given STAR, the BODY_SEQ will uniquely identify the Segment.</remarks>
		public int AptBodySeq { get; set; }

		/// <summary>
		/// The associated Airport Identifier
		/// _Src: STAR_APT.csv(ARPT_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string ArptId { get; set; }

		/// <summary>
		/// The Runway End Identifier if applicable
		/// _Src: STAR_APT.csv(RWY_END_ID)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? RwyEndId { get; set; }

	}
	#endregion

	#region Star_BASE Fields
	public class StarBase : CommonFields
	{
		/// <summary>
		/// Arrival Name
		/// _Src: STAR_BASE.csv(ARRIVAL_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Name Assigned to the Standard Terminal Arrival</remarks>
		public string ArrivalName { get; set; }

		/// <summary>
		/// Amendment Number
		/// _Src: STAR_BASE.csv(AMENDMENT_NO)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>(spelled out)</remarks>
		public string AmendmentNo { get; set; }

		/// <summary>
		/// Amendment Effective Date
		/// _Src: STAR_BASE.csv(STAR_AMEND_EFF_DATE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The First Effective Date for which the STAR Amendment became Active</remarks>
		public string StarAmendEffDate { get; set; }

		/// <summary>
		/// RNAV Flag
		/// _Src: STAR_BASE.csv(RNAV_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Y/N Flag determines whether a STAR is RNAV required</remarks>
		public string RnavFlag { get; set; }

		/// <summary>
		/// Served Airports
		/// _Src: STAR_BASE.csv(SERVED_ARPT)
		/// _MaxLength: 200
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>List of Airports Served by the STAR</remarks>
		public string ServedArpt { get; set; }

	}
	#endregion

	#region Star_RTE Fields
	public class StarRte : CommonFields
	{
		/// <summary>
		/// Route Portion Type
		/// _Src: STAR_RTE.csv(ROUTE_PORTION_TYPE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The Segment is identified as either a Transition or Body</remarks>
		public string RoutePortionType { get; set; }

		/// <summary>
		/// Route Name
		/// _Src: STAR_RTE.csv(ROUTE_NAME)
		/// _MaxLength: 110
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The Transition or Body Name</remarks>
		public string RouteName { get; set; }

		/// <summary>
		/// Body Sequence Identifier
		/// _Src: STAR_RTE.csv(BODY_SEQ)
		/// _MaxLength: (1,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other STAR_*.csv files. the rare case that Body Name is not Unique for a given STAR, the BODY_SEQ will uniquely identify the Segment.</remarks>
		public int RteBodySeq { get; set; }

		/// <summary>
		/// FAA-Assigned Computer Identifier for the TRANSITION
		/// _Src: STAR_RTE.csv(TRANSITION_COMPUTER_CODE)
		/// _MaxLength: 20
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? TransitionComputerCode { get; set; }

		/// <summary>
		/// Sequencing number
		/// _Src: STAR_RTE.csv(POINT_SEQ)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>Sequencing number in multiples of ten. Points are in order adapted for given Segment</remarks>
		public int PointSeq { get; set; }

		/// <summary>
		/// The FIX or NAVAID adapted on the Segment
		/// _Src: STAR_RTE.csv(POINT)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string Point { get; set; }

		/// <summary>
		/// ICAO Region Code
		/// _Src: STAR_RTE.csv(ICAO_REGION_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>This is the two letter ICAO Region Code for FIX Point Types only</remarks>
		public string? IcaoRegionCode { get; set; }

		/// <summary>
		/// Point Type
		/// _Src: STAR_RTE.csv(POINT_TYPE)
		/// _MaxLength: 25
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>CN=Computer Navigation Fix _ MR=Military Reporting Point _ MW=Military Waypoint _ NRS=NRS Waypoint _ RADAR=Radar _ RP=Reporting Point _ VFR=VFR Waypoint _ WP=Waypoint _ CONSOLAN=Low-Frequency Long-Distance NAVAID (Transoceanic) _ DME=Distance Measuring Equipment Only _ FAN MARKER=En Route Marker Beacon (FAN, low power FAN, Z Marker) _ MARINE NDB=Marine Non-Directional Beacon _ MARINE NDB/DME=Marine NDB with DME _ NDB=Non-Directional Beacon _ NDB/DME=NDB with DME _ TACAN=Tactical Air Navigation (Azimuth + Distance) _ UHF/NDB=UHF Non-Directional Beacon _ VOR=VHF Omnidirectional Range (Azimuth only) _ VORTAC=VOR + TACAN (Azimuth and DME) _ VOR/DME=VOR with DME _ VOT=VOR Test Facility</remarks>
		public string PointType { get; set; }

		/// <summary>
		/// Next Point
		/// _Src: STAR_RTE.csv(NEXT_POINT)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>The Point that directly follows the current Point on an individual segment</remarks>
		public string? NextPoint { get; set; }

		/// <summary>
		/// Associated APT/RWY
		/// _Src: STAR_RTE.csv(ARPT_RWY_ASSOC)
		/// _MaxLength: 200
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>The list of APT and/or APT/RWY associated with a given Segment</remarks>
		public string? ArptRwyAssoc { get; set; }

	}
	#endregion

}