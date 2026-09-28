namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// Row models for the NASR <c>ARB</c> CSV files (ARTCC boundaries): one nested class per file.
/// </summary>
public class ArbCsvDataModel
{
	#region Common Fields
	/// <summary>The columns every <c>ARB</c> file shares.</summary>
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
		/// Location Identifier
		/// _Src: All Arb_*.csv files(LOCATION_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>3-4 character alphanumeric identifier.</remarks>
		public string LocationId { get; set; }

		/// <summary>
		/// Center (ARTCC) Name
		/// _Src: All Arb_*.csv files(LOCATION_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? LocationName { get; set; }

	}
	#endregion

	#region Arb_BASE Fields
	/// <summary>One row of <c>ARB_BASE.csv</c>.</summary>
	public class ArbBase : CommonFields
	{
		/// <summary>
		/// Location Computer Identifier
		/// _Src: ARB_BASE.csv(COMPUTER_ID)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string ComputerId { get; set; }

		/// <summary>
		/// ICAO Identifier
		/// _Src: ARB_BASE.csv(ICAO_ID)
		/// _MaxLength: 7
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? IcaoId { get; set; }

		/// <summary>
		/// Location Type (ARTCC or CERAP).
		/// _Src: ARB_BASE.csv(LOCATION_TYPE)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LocationType { get; set; }

		/// <summary>
		/// Location City Name
		/// _Src: ARB_BASE.csv(CITY)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string City { get; set; }

		/// <summary>
		/// State 2 letter code
		/// _Src: ARB_BASE.csv(STATE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Location State Post Office Code standard two letter abbreviation for US States and Territories</remarks>
		public string? State { get; set; }

		/// <summary>
		/// Location Country Post Office Code
		/// _Src: ARB_BASE.csv(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryCode { get; set; }

		/// <summary>
		/// Location Reference Point Latitude Degrees
		/// _Src: ARB_BASE.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public int BaseLatDeg { get; set; }

		/// <summary>
		/// Location Reference Point Latitude Minutes
		/// _Src: ARB_BASE.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public int BaseLatMin { get; set; }

		/// <summary>
		/// Location Reference Point Latitude Seconds
		/// _Src: ARB_BASE.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public double BaseLatSec { get; set; }

		/// <summary>
		/// Location Reference Point Latitude Hemisphere
		/// _Src: ARB_BASE.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public string BaseLatHemis { get; set; }

		/// <summary>
		/// Location Reference Point Latitude in Decimal Format
		/// _Src: ARB_BASE.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public double BaseLatDecimal { get; set; }

		/// <summary>
		/// Location Reference Point Longitude Degrees
		/// _Src: ARB_BASE.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public int BaseLongDeg { get; set; }

		/// <summary>
		/// Location Reference Point Longitude Minutes
		/// _Src: ARB_BASE.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public int BaseLongMin { get; set; }

		/// <summary>
		/// Location Reference Point Longitude Seconds
		/// _Src: ARB_BASE.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public double BaseLongSec { get; set; }

		/// <summary>
		/// Location Reference Point Longitude Hemisphere
		/// _Src: ARB_BASE.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public string BaseLongHemis { get; set; }

		/// <summary>
		/// Location Reference Point Longitude in Decimal Format
		/// _Src: ARB_BASE.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public double BaseLongDecimal { get; set; }

		/// <summary>
		/// Cross Reference Text
		/// _Src: ARB_BASE.csv(CROSS_REF)
		/// _MaxLength: 50
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Free Form Text that further describes a specific Information Item</remarks>
		public string? CrossRef { get; set; }

	}
	#endregion

	#region Arb_SEG Fields
	/// <summary>One row of <c>ARB_SEG.csv</c>.</summary>
	public class ArbSeg : CommonFields
	{
		/// <summary>
		/// Record Identifier
		/// _Src: ARB_SEG.csv(REC_ID)
		/// _MaxLength: 14
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Concatenation of the LOCATION_ID * BNDRY_CODE * 5 Character Point Designator</remarks>
		public string RecId { get; set; }

		/// <summary>
		/// Boundary Altitude Structure
		/// _Src: ARB_SEG.csv(ALTITUDE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>HIGH, LOW or UNLIMITED.</remarks>
		public string Altitude { get; set; }

		/// <summary>
		/// Boundary Type
		/// _Src: ARB_SEG.csv(TYPE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>ARTCC, FIR, CTA, CTA/FIR, UTA</remarks>
		public string Type { get; set; }

		/// <summary>
		/// Point Sequencing number
		/// _Src: ARB_SEG.csv(POINT_SEQ)
		/// _MaxLength: (4,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>In multiples of ten. Points are in order adapted for given Boundary.</remarks>
		public int PointSeq { get; set; }

		/// <summary>
		/// Boundary Point Latitude Degrees
		/// _Src: ARB_SEG.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public int SegLatDeg { get; set; }

		/// <summary>
		/// Boundary Point Latitude Minutes
		/// _Src: ARB_SEG.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public int SegLatMin { get; set; }

		/// <summary>
		/// Boundary Point Latitude Seconds
		/// _Src: ARB_SEG.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public double SegLatSec { get; set; }

		/// <summary>
		/// Boundary Point Latitude Hemisphere
		/// _Src: ARB_SEG.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public string SegLatHemis { get; set; }

		/// <summary>
		/// Boundary Point Latitude in Decimal Format
		/// _Src: ARB_SEG.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public double SegLatDecimal { get; set; }

		/// <summary>
		/// Boundary Point Longitude Degrees
		/// _Src: ARB_SEG.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public int SegLongDeg { get; set; }

		/// <summary>
		/// Boundary Point Longitude Minutes
		/// _Src: ARB_SEG.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public int SegLongMin { get; set; }

		/// <summary>
		/// Boundary Point Longitude Seconds
		/// _Src: ARB_SEG.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public double SegLongSec { get; set; }

		/// <summary>
		/// Boundary Point Longitude Hemisphere
		/// _Src: ARB_SEG.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public string SegLongHemis { get; set; }

		/// <summary>
		/// Boundary Point Longitude in Decimal Format
		/// _Src: ARB_SEG.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ARB_*.csv files</remarks>
		public double SegLongDecimal { get; set; }

		/// <summary>
		/// Description
		/// _Src: ARB_SEG.csv(BNDRY_PT_DESCRIP)
		/// _MaxLength: 300
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Description of Boundary Line Connecting Points on The Boundary</remarks>
		public string? BndryPtDescrip { get; set; }

		/// <summary>
		/// NAS Description
		/// _Src: ARB_SEG.csv(NAS_DESCRIP_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>An 'X' In This Field Indicates This Point Is Used Only in The NAS Description and Not the Legal Description</remarks>
		public string? NasDescripFlag { get; set; }

	}
	#endregion

}