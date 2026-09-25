namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// Row models for the NASR <c>RDR</c> CSV files (radar facilities): one nested class per file.
/// </summary>
public class RdrCsvDataModel
{
	#region Common Fields
	/// <summary>The columns every <c>RDR</c> file shares.</summary>
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

	#region Rdr Fields
	/// <summary>One row of <c>RDR.csv</c>.</summary>
	public class Rdr : CommonFields
	{
		/// <summary>
		/// Facility ID
		/// _Src: RDR.csv(FACILITY_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Location Identifier. Unique 3-4 character alphanumeric identifier assigned to the Landing Facility or TRACON.</remarks>
		public string FacilityId { get; set; }

		/// <summary>
		/// Facility Type
		/// _Src: RDR.csv(FACILITY_TYPE)
		/// _MaxLength: 7
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Type of Facility associated with the RADAR data - either AIRPORT or TRACON.</remarks>
		public string FacilityType { get; set; }

		/// <summary>
		/// State Code
		/// _Src: RDR.csv(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? StateCode { get; set; }

		/// <summary>
		/// Country Code
		/// _Src: RDR.csv(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryCode { get; set; }

		/// <summary>
		/// RADAR Type
		/// _Src: RDR.csv(RADAR_TYPE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>ARSR=Air Route Surveillance Radar _ ASR=Airport Surveillance Radar (-# denotes ASR type) _ ASR/PAR=Airport Surveillance Radar plus Precision Approach Radar _ GCA=Ground Control Approach _ PAR=Precision Approach Radar _ SECRA=Secondary Radar</remarks>
		public string RadarType { get; set; }

		/// <summary>
		/// RADAR Sequence Number
		/// _Src: RDR.csv(RADAR_NO)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>Unique Sequence Number assigned to each Radar at a Facility</remarks>
		public int RadarNo { get; set; }

		/// <summary>
		/// RADAR Hours of Operation
		/// _Src: RDR.csv(RADAR_HRS)
		/// _MaxLength: 200
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string RadarHrs { get; set; }

		/// <summary>
		/// Remark
		/// _Src: RDR.csv(REMARK)
		/// _MaxLength: 1500
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Remark associated with RADAR Operations</remarks>
		public string? Remark { get; set; }

	}
	#endregion

}