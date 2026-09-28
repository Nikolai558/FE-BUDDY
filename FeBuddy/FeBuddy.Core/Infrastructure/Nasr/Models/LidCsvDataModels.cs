namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// Row models for the NASR <c>LID</c> CSV files (location identifiers): one nested class per file.
/// </summary>
public class LidCsvDataModel
{
	#region Common Fields
	/// <summary>The columns every <c>LID</c> file shares.</summary>
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

	}
	#endregion

	#region Lid Fields
	/// <summary>One row of <c>LID.csv</c>.</summary>
	public class Lid : CommonFields
	{
		/// <summary>
		/// Country Code
		/// _Src: LID.csv(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryCode { get; set; }

		/// <summary>
		/// Location Identifier
		/// _Src: LID.csv(LOC_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>3-4 character alphanumeric identifier</remarks>
		public string LocId { get; set; }

		/// <summary>
		/// FAA Region Code Associated with the Location Identifier
		/// _Src: LID.csv(REGION_CODE)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>AAL=Alaska _ ACE=Central _ AEA=Eastern _ AGL=Great Lakes _ ANE=New England _ ANM=Northwest Mountain _ ASO=Southern _ ASW=Southwest _ AWP=Western-Pacific</remarks>
		public string? RegionCode { get; set; }

		/// <summary>
		/// State
		/// _Src: LID.csv(STATE)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? State { get; set; }

		/// <summary>
		/// City
		/// _Src: LID.csv(CITY)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string City { get; set; }

		/// <summary>
		/// Lid Group
		/// _Src: LID.csv(LID_GROUP)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Control Facility _ Flight Service Station _ Instrument Landing Facility _ Landing Facility _ Navigation Aid _ Remote Communication Outlet _ Special Use Resource _ Weather Reporting Station _ Weather Sensor</remarks>
		public string LidGroup { get; set; }

		/// <summary>
		/// Facility Type
		/// _Src: LID.csv(FAC_TYPE)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Control Facility: ARTCC, BASE OPS, CERAP, TRACON _ Flight Service Station: FSS, HUB, RADIO _ Instrument Landing System: DD=LDS/DME, LA=LDA, LC=LOC, LD=ILS/DME, LE=LOC/DME, LG=LOC/GS, LS=ILS, SD=SDF/DME, SF=SDF _ Landing Facility: A=Airport, B=Balloonport, C=Seaplane Base, G=Gliderport, H=Heliport, U=Ultralight, V=Vertiport _ Navigation Aid: DME, FAN MARKER, MARINE NDB, NDB, NDB/DME, TACAN, VOR, VOR/DME, VORTAC, VOT _ Remote Communication Outlet: RCO, RCO1 (for shared identifier) _ Special Use Resource: ADMINISTRATIVE SERVICES, GEOGRAPHIC REFERENCE POINT, SPECIAL USAGE (may include (#) for duplicates) _ Weather Service Office _ Weather Reporting Station: WXL _ Weather Sensor: ASOS, AWOS-1, AWOS-2, AWOS-3, AWOS-3P, AWOS-3PT, AWOS-3T, AWOS-4, AWOS-A, AWOS-AV</remarks>
		public string FacType { get; set; }

		/// <summary>
		/// Official Facility Name
		/// _Src: LID.csv(FAC_NAME)
		/// _MaxLength: 75
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Represents the official facility name. For Instrument Landing Systems, it is a concatenation of the associated landing facility name, ID, and runway end ID (e.g., ATLANTIC CITY INTL(ACY) ILS RWY 31)</remarks>
		public string? FacName { get; set; }

		/// <summary>
		/// Responsible FAA Air Route Traffic Control Center (ARTCC) Identifier
		/// _Src: LID.csv(RESP_ARTCC_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? RespArtccId { get; set; }

		/// <summary>
		/// Responsible ARTCC Computer Identifier
		/// _Src: LID.csv(ARTCC_COMPUTER_ID)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? ArtccComputerId { get; set; }

		/// <summary>
		/// Tie-In Flight Service Station (FSS) Identifier
		/// _Src: LID.csv(FSS_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? FssId { get; set; }

	}
	#endregion

}