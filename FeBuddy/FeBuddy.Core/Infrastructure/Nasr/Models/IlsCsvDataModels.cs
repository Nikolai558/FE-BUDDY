namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// Row models for the NASR <c>ILS</c> CSV files (instrument landing systems): one nested class per file.
/// </summary>
public class IlsCsvDataModel
{
	#region Common Fields
	/// <summary>The columns every <c>ILS</c> file shares.</summary>
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
		/// Landing Facility Site Number
		/// _Src: All Ils_*.csv files(SITE_NO)
		/// _MaxLength: 9
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>A unique identifying number</remarks>
		public string SiteNo { get; set; }

		/// <summary>
		/// Landing Facility Type Code
		/// _Src: All Ils_*.csv files(SITE_TYPE_CODE)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>A=Airport _ B=Balloonport _ C=Seaplane Base _ G=Gliderport _ H=Heliport _ U=Ultralight</remarks>
		public string SiteTypeCode { get; set; }

		/// <summary>
		/// State Code
		/// _Src: All Ils_*.csv files(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? StateCode { get; set; }

		/// <summary>
		/// Location Identifier
		/// _Src: All Ils_*.csv files(ARPT_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Unique 3-4 character alphanumeric identifier assigned to the Landing Facility.</remarks>
		public string ArptId { get; set; }

		/// <summary>
		/// City
		/// _Src: All Ils_*.csv files(CITY)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string City { get; set; }

		/// <summary>
		/// Country Code
		/// _Src: All Ils_*.csv files(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryCode { get; set; }

		/// <summary>
		/// ILS Runway End Identifier
		/// _Src: All Ils_*.csv files(RWY_END_ID)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string RwyEndId { get; set; }

		/// <summary>
		/// ILS Identification
		/// _Src: All Ils_*.csv files(ILS_LOC_ID)
		/// _MaxLength: 6
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string IlsLocId { get; set; }

		/// <summary>
		/// ILS System Type
		/// _Src: All Ils_*.csv files(SYSTEM_TYPE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>LS=ILS (Instrument Landing System) _ SF=SDF (Simplified Directional Facility) _ LC=LOC (Localizer) _ LA=LDA (Localizer-Type Directional Aid) _ LD=ILS/DME (ILS with Distance Measuring Equipment) _ SD=SDF/DME (SDF with Distance Measuring Equipment) _ LE=LOC/DME (Localizer with Distance Measuring Equipment) _ LG=LOC/GS (Localizer/Glide Slope) _ DD=LDA/DME (LDA with Distance Measuring Equipment)</remarks>
		public string SystemTypeCode { get; set; }

	}
	#endregion

	#region Ils_BASE Fields
	/// <summary>One row of <c>ILS_BASE.csv</c>.</summary>
	public class IlsBase : CommonFields
	{
		/// <summary>
		/// State Name
		/// _Src: ILS_BASE.csv(STATE_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? StateName { get; set; }

		/// <summary>
		/// FAA Region Responsible for NAVAID
		/// _Src: ILS_BASE.csv(REGION_CODE)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>AAL=Alaska _ ACE=Central _ AEA=Eastern _ AGL=Great Lakes _ ANE=New England _ ANM=Northwest Mountain _ ASO=Southern _ ASW=Southwest _ AWP=Western-Pacific</remarks>
		public string RegionCode { get; set; }

		/// <summary>
		/// ILS Runway Length in Whole Feet
		/// _Src: ILS_BASE.csv(RWY_LEN)
		/// _MaxLength: (5,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int RwyLen { get; set; }

		/// <summary>
		/// ILS Runway Width in Whole Feet
		/// _Src: ILS_BASE.csv(RWY_WIDTH)
		/// _MaxLength: (4,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int RwyWidth { get; set; }

		/// <summary>
		/// Category of the ILS
		/// _Src: ILS_BASE.csv(CATEGORY)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Category { get; set; }

		/// <summary>
		/// Operator Name and Code
		/// _Src: ILS_BASE.csv(OWNER)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Concatenation of the ILS OWNER CODE - ILS OWNER NAME. Examples: "F-FEDERAL AVIATION ADMIN" "R-U.S. ARMY"</remarks>
		public string Owner { get; set; }

		/// <summary>
		/// Operator of the ILS
		/// _Src: ILS_BASE.csv(OPERATOR)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Concatenation of the ILS OPERATOR CODE - ILS OPERATOR NAME</remarks>
		public string Operator { get; set; }

		/// <summary>
		/// Approach Bearing
		/// _Src: ILS_BASE.csv(APCH_BEAR)
		/// _MaxLength: (5,2)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>ILS Approach Bearing in Degrees Magnetic</remarks>
		public double ApchBear { get; set; }

		/// <summary>
		/// Magnetic Variation Degrees
		/// _Src: ILS_BASE.csv(MAG_VAR)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int MagVar { get; set; }

		/// <summary>
		/// Magnetic Variation Direction
		/// _Src: ILS_BASE.csv(MAG_VAR_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string MagVarHemis { get; set; }

		/// <summary>
		/// Operational Status of Localizer
		/// _Src: ILS_BASE.csv(COMPONENT_STATUS)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string BaseComponentStatus { get; set; }

		/// <summary>
		/// Effective Date of Localizer Operational Status
		/// _Src: ILS_BASE.csv(COMPONENT_STATUS_DATE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string BaseComponentStatusDate { get; set; }

		/// <summary>
		/// Localizer Antenna Latitude Degrees
		/// _Src: ILS_BASE.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int BaseLatDeg { get; set; }

		/// <summary>
		/// Localizer Antenna Latitude Minutes
		/// _Src: ILS_BASE.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int BaseLatMin { get; set; }

		/// <summary>
		/// Localizer Antenna Latitude Seconds
		/// _Src: ILS_BASE.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double BaseLatSec { get; set; }

		/// <summary>
		/// Localizer Antenna Latitude Hemisphere
		/// _Src: ILS_BASE.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string BaseLatHemis { get; set; }

		/// <summary>
		/// Localizer Antenna Latitude in Decimal Format
		/// _Src: ILS_BASE.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double BaseLatDecimal { get; set; }

		/// <summary>
		/// Localizer Antenna Longitude Degrees
		/// _Src: ILS_BASE.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int BaseLongDeg { get; set; }

		/// <summary>
		/// Localizer Antenna Longitude Minutes
		/// _Src: ILS_BASE.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int BaseLongMin { get; set; }

		/// <summary>
		/// Localizer Antenna Longitude Seconds
		/// _Src: ILS_BASE.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double BaseLongSec { get; set; }

		/// <summary>
		/// Localizer Antenna Longitude Hemisphere
		/// _Src: ILS_BASE.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string BaseLongHemis { get; set; }

		/// <summary>
		/// Localizer Antenna Longitude in Decimal Format
		/// _Src: ILS_BASE.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double BaseLongDecimal { get; set; }

		/// <summary>
		/// Latitude/Longitude Source Code
		/// _Src: ILS_BASE.csv(LAT_LONG_SOURCE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files. A=Air Force _ C=Coast Guard _ D=Canadian AIRAC _ F=FAA _ FS=Tech Ops (AFS-530) _ G=NOS (Historical) _ K=NGS _ M=DoD (NGA) _ N=U.S. Navy _ O=Owner _ P=NOS Photo Survey (Historical) _ Q=Quad Plot (Historical) _ R=Army _ S=SIAP _ T=3rd Party Survey _ Z=Surveyed</remarks>
		public string? BaseLatLongSourceCode { get; set; }

		/// <summary>
		/// Site Elevation of Localizer Antenna in Tenth of a Foot (MSL).
		/// _Src: ILS_BASE.csv(SITE_ELEVATION)
		/// _MaxLength: (6,1)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double? BaseSiteElevation { get; set; }

		/// <summary>
		/// Localizer Frequency (MHZ)
		/// _Src: ILS_BASE.csv(LOC_FREQ)
		/// _MaxLength: (6,2)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LocFreq { get; set; }

		/// <summary>
		/// Localizer Back Course Status
		/// _Src: ILS_BASE.csv(BK_COURSE_STATUS_CODE)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>N=No Restrictions _ R=Restricted _ U=Unusable _ Y=Usable</remarks>
		public string? BkCourseStatusCode { get; set; }

	}
	#endregion

	#region Ils_DME Fields
	/// <summary>One row of <c>ILS_DME.csv</c>.</summary>
	public class IlsDme : CommonFields
	{
		/// <summary>
		/// Operational Status of DME
		/// _Src: ILS_DME.csv(COMPONENT_STATUS)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string DmeComponentStatus { get; set; }

		/// <summary>
		/// Effective Date of DME Operational Status
		/// _Src: ILS_DME.csv(COMPONENT_STATUS_DATE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string DmeComponentStatusDate { get; set; }

		/// <summary>
		/// DME Transponder Antenna Latitude Degrees
		/// _Src: ILS_DME.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int DmeLatDeg { get; set; }

		/// <summary>
		/// DME Transponder Antenna Latitude Minutes
		/// _Src: ILS_DME.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int DmeLatMin { get; set; }

		/// <summary>
		/// DME Transponder Antenna Latitude Seconds
		/// _Src: ILS_DME.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double DmeLatSec { get; set; }

		/// <summary>
		/// DME Transponder Antenna Latitude Hemisphere
		/// _Src: ILS_DME.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string DmeLatHemis { get; set; }

		/// <summary>
		/// DME Transponder Antenna Latitude in Decimal Format
		/// _Src: ILS_DME.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double DmeLatDecimal { get; set; }

		/// <summary>
		/// DME Transponder Antenna Longitude Degrees
		/// _Src: ILS_DME.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int DmeLongDeg { get; set; }

		/// <summary>
		/// DME Transponder Antenna Longitude Minutes
		/// _Src: ILS_DME.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int DmeLongMin { get; set; }

		/// <summary>
		/// DME Transponder Antenna Longitude Seconds
		/// _Src: ILS_DME.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double DmeLongSec { get; set; }

		/// <summary>
		/// DME Transponder Antenna Longitude Hemisphere
		/// _Src: ILS_DME.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string DmeLongHemis { get; set; }

		/// <summary>
		/// DME Transponder Antenna Longitude in Decimal Format
		/// _Src: ILS_DME.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double DmeLongDecimal { get; set; }

		/// <summary>
		/// Code Indication Source of Latitude/Longitude Information
		/// _Src: ILS_DME.csv(LAT_LONG_SOURCE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files. A=Air Force _ C=Coast Guard _ D=Canadian AIRAC _ F=FAA _ FS=Tech Ops (AFS-530) _ G=NOS (Historical) _ K=NGS _ M=DoD (NGA) _ N=U.S. Navy _ O=Owner _ P=NOS Photo Survey (Historical) _ Q=Quad Plot (Historical) _ R=Army _ S=SIAP _ T=3rd Party Survey _ Z=Surveyed</remarks>
		public string? DmeLatLongSourceCode { get; set; }

		/// <summary>
		/// Site Elevation of DME Transponder Antenna in Tenth of a Foot (MSL)
		/// _Src: ILS_DME.csv(SITE_ELEVATION)
		/// _MaxLength: (6,1)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double? DmeSiteElevation { get; set; }

		/// <summary>
		/// NAS Channel on Which Distance Data is Transmitted
		/// _Src: ILS_DME.csv(CHANNEL)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string Channel { get; set; }

	}
	#endregion

	#region Ils_GS Fields
	/// <summary>One row of <c>ILS_GS.csv</c>.</summary>
	public class IlsGs : CommonFields
	{
		/// <summary>
		/// Operational Status of Glide Slope
		/// _Src: ILS_GS.csv(COMPONENT_STATUS)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string GsComponentStatus { get; set; }

		/// <summary>
		/// Effective Date of Glide Slope Operational Status
		/// _Src: ILS_GS.csv(COMPONENT_STATUS_DATE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string GsComponentStatusDate { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Latitude Degrees
		/// _Src: ILS_GS.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int GsLatDeg { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Latitude Minutes
		/// _Src: ILS_GS.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int GsLatMin { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Latitude Seconds
		/// _Src: ILS_GS.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double GsLatSec { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Latitude Hemisphere
		/// _Src: ILS_GS.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string GsLatHemis { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Latitude in Decimal Format
		/// _Src: ILS_GS.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double GsLatDecimal { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Longitude Degrees
		/// _Src: ILS_GS.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int GsLongDeg { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Longitude Minutes
		/// _Src: ILS_GS.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int GsLongMin { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Longitude Seconds
		/// _Src: ILS_GS.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double GsLongSec { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Longitude Hemisphere
		/// _Src: ILS_GS.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string GsLongHemis { get; set; }

		/// <summary>
		/// Glide Slope Transmitter Antenna Longitude in Decimal Format
		/// _Src: ILS_GS.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double GsLongDecimal { get; set; }

		/// <summary>
		/// Code Indication Source of Latitude/Longitude Information
		/// _Src: ILS_GS.csv(LAT_LONG_SOURCE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files. A=Air Force _ C=Coast Guard _ D=Canadian AIRAC _ F=FAA _ FS=Tech Ops (AFS-530) _ G=NOS (Historical) _ K=NGS _ M=DoD (NGA) _ N=U.S. Navy _ O=Owner _ P=NOS Photo Survey (Historical) _ Q=Quad Plot (Historical) _ R=Army _ S=SIAP _ T=3rd Party Survey _ Z=Surveyed</remarks>
		public string? GsLatLongSourceCode { get; set; }

		/// <summary>
		/// Site Elevation of Glide Slope Transmitter Antenna in Tenth of a Foot (MSL)
		/// _Src: ILS_GS.csv(SITE_ELEVATION)
		/// _MaxLength: (6,1)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double? GsSiteElevation { get; set; }

		/// <summary>
		/// Glide Slope Class/Type
		/// _Src: ILS_GS.csv(G_S_TYPE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>GLIDE SLOPE=Standard Glide Slope _ GLIDE SLOPE/DME=Glide Slope with Distance Measuring Equipment</remarks>
		public string GSTypeCode { get; set; }

		/// <summary>
		/// Glide Slope Angle in Degrees and Hundredths of Degree
		/// _Src: ILS_GS.csv(G_S_ANGLE)
		/// _MaxLength: (4,2)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double GSAngle { get; set; }

		/// <summary>
		/// Glide Slope Transmission Frequency
		/// _Src: ILS_GS.csv(G_S_FREQ)
		/// _MaxLength: (6,2)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double GSFreq { get; set; }

	}
	#endregion

	#region Ils_MKR Fields
	/// <summary>One row of <c>ILS_MKR.csv</c>.</summary>
	public class IlsMkr : CommonFields
	{
		/// <summary>
		/// Marker Type
		/// _Src: ILS_MKR.csv(ILS_COMP_TYPE_CODE)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files. IM - Inner Marker, MM - Middle Marker, OM - Outer Marker.</remarks>
		public string MkrIlsCompTypeCode { get; set; }

		/// <summary>
		/// Operational Status of Marker Beacon
		/// _Src: ILS_MKR.csv(COMPONENT_STATUS)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string MkrComponentStatus { get; set; }

		/// <summary>
		/// Effective Date of Marker Beacon Operational Status
		/// _Src: ILS_MKR.csv(COMPONENT_STATUS_DATE)
		/// _MaxLength: 10
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string MkrComponentStatusDate { get; set; }

		/// <summary>
		/// Marker Beacon Latitude Degrees
		/// _Src: ILS_MKR.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int MkrLatDeg { get; set; }

		/// <summary>
		/// Marker Beacon Latitude Minutes
		/// _Src: ILS_MKR.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int MkrLatMin { get; set; }

		/// <summary>
		/// Marker Beacon Latitude Seconds
		/// _Src: ILS_MKR.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double MkrLatSec { get; set; }

		/// <summary>
		/// Marker Beacon Latitude Hemisphere
		/// _Src: ILS_MKR.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string MkrLatHemis { get; set; }

		/// <summary>
		/// Marker Beacon Latitude in Decimal Format
		/// _Src: ILS_MKR.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double MkrLatDecimal { get; set; }

		/// <summary>
		/// Marker Beacon Longitude Degrees
		/// _Src: ILS_MKR.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int MkrLongDeg { get; set; }

		/// <summary>
		/// Marker Beacon Longitude Minutes
		/// _Src: ILS_MKR.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public int MkrLongMin { get; set; }

		/// <summary>
		/// Marker Beacon Longitude Seconds
		/// _Src: ILS_MKR.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double MkrLongSec { get; set; }

		/// <summary>
		/// Marker Beacon Longitude Hemisphere
		/// _Src: ILS_MKR.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public string MkrLongHemis { get; set; }

		/// <summary>
		/// Marker Beacon Longitude in Decimal Format
		/// _Src: ILS_MKR.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double MkrLongDecimal { get; set; }

		/// <summary>
		/// Code Indication Source of Latitude/Longitude Information
		/// _Src: ILS_MKR.csv(LAT_LONG_SOURCE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files. A=Air Force _ C=Coast Guard _ D=Canadian AIRAC _ F=FAA _ FS=Tech Ops (AFS-530) _ G=NOS (Historical) _ K=NGS _ M=DoD (NGA) _ N=U.S. Navy _ O=Owner _ P=NOS Photo Survey (Historical) _ Q=Quad Plot (Historical) _ R=Army _ S=SIAP _ T=3rd Party Survey _ Z=Surveyed</remarks>
		public string? MkrLatLongSourceCode { get; set; }

		/// <summary>
		/// Site Elevation of Marker Beacon in Tenth of a Foot (MSL)
		/// _Src: ILS_MKR.csv(SITE_ELEVATION)
		/// _MaxLength: (6,1)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files</remarks>
		public double? MkrSiteElevation { get; set; }

		/// <summary>
		/// Facility/Type of Marker/Locator
		/// _Src: ILS_MKR.csv(MKR_FAC_TYPE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>M=Marker Beacon Only _ C=Compass Locator _ R=NDB (Nondirectional Radio Beacon) _ MC=Marker/Compass Locator _ MR=Marker/NDB</remarks>
		public string MkrFacTypeCode { get; set; }

		/// <summary>
		/// Location Identifier of Beacon at Marker
		/// _Src: ILS_MKR.csv(MARKER_ID_BEACON)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? MarkerIdBeacon { get; set; }

		/// <summary>
		/// Name of the Marker Locator Beacon
		/// _Src: ILS_MKR.csv(COMPASS_LOCATOR_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? CompassLocatorName { get; set; }

		/// <summary>
		/// Frequency
		/// _Src: ILS_MKR.csv(FREQ)
		/// _MaxLength: (5,2)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>NAVAID Frequency when Marker is collocated else Locator Frequency (in KHZ)</remarks>
		public double? Freq { get; set; }

		/// <summary>
		/// NAVAID ID
		/// _Src: ILS_MKR.csv(NAV_ID)
		/// _MaxLength: 6
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Location identifier of the navigation aid collocated with the marker; blank if the marker is not collocated with a NAVAID</remarks>
		public string? NavId { get; set; }

		/// <summary>
		/// Collocated NAVAID Type
		/// _Src: ILS_MKR.csv(NAV_TYPE)
		/// _MaxLength: 25
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? NavType { get; set; }

		/// <summary>
		/// Low Powered NDB Status of Marker Beacon
		/// _Src: ILS_MKR.csv(LOW_POWERED_NDB_STATUS)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? LowPoweredNdbStatus { get; set; }

	}
	#endregion

	#region Ils_RMK Fields
	/// <summary>One row of <c>ILS_RMK.csv</c>.</summary>
	public class IlsRmk : CommonFields
	{
		/// <summary>
		/// NASR table associated with Remark
		/// _Src: ILS_RMK.csv(TAB_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string TabName { get; set; }

		/// <summary>
		/// ILS Component Type Code
		/// _Src: ILS_RMK.csv(ILS_COMP_TYPE_CODE)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>PropertyName changed due to identical column name in other ILS_*.csv files. Specifies the ILS component type referred to by the remark; derived from TAB_NAME except for the ILS tab, which refers to the overall system</remarks>
		public string? RmkIlsCompTypeCode { get; set; }

		/// <summary>
		/// NASR Column Name Associated with Remark
		/// _Src: ILS_RMK.csv(REF_COL_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Indicates the specific NASR column name related to the remark; non-specific remarks are labeled as GENERAL_REMARK</remarks>
		public string RefColName { get; set; }

		/// <summary>
		/// Sequence number assigned to Reference Column Remark
		/// _Src: ILS_RMK.csv(REF_COL_SEQ_NO)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int RefColSeqNo { get; set; }

		/// <summary>
		/// Remark
		/// _Src: ILS_RMK.csv(REMARK)
		/// _MaxLength: 300
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Free Form Text that further describes a specific Information Item</remarks>
		public string Remark { get; set; }

	}
	#endregion

}