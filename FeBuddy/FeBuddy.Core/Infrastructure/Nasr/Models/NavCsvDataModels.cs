namespace FeBuddy.Core.Infrastructure.Nasr.Models;

public class NavCsvDataModel
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
		/// NAVAID Facility Identifier
		/// _Src: All Nav_*.csv files(NAV_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string NavId { get; set; }

		/// <summary>
		/// NAVAID Facility Type
		/// _Src: All Nav_*.csv files(NAV_TYPE)
		/// _MaxLength: 25
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>CONSOLAN=Low-Frequency Long-Distance NAVAID (Transoceanic) _ DME=Distance Measuring Equipment Only _ FAN MARKER=En Route Marker Beacon (FAN, low power FAN, Z Marker) _ MARINE NDB=Marine Non-Directional Beacon _ MARINE NDB/DME=Marine NDB with DME _ NDB=Non-Directional Beacon _ NDB/DME=NDB with DME _ TACAN=Tactical Air Navigation (Azimuth + Slant Range Distance) _ UHF/NDB=UHF Non-Directional Beacon _ VOR=VHF Omnidirectional Range (Azimuth only) _ VORTAC=VOR + TACAN (Azimuth and DME) _ VOR/DME=VOR with DME _ VOT=VOR Test Facility</remarks>
		public string NavType { get; set; }

		/// <summary>
		/// State Code
		/// _Src: All Nav_*.csv files(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? StateCode { get; set; }

		/// <summary>
		/// City
		/// _Src: All Nav_*.csv files(CITY)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string City { get; set; }

		/// <summary>
		/// Country Code
		/// _Src: All Nav_*.csv files(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryCode { get; set; }

	}
	#endregion

	#region Nav_BASE Fields
	public class NavBase : CommonFields
	{
		/// <summary>
		/// Navigation Aid Status
		/// _Src: NAV_BASE.csv(NAV_STATUS)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string NavStatus { get; set; }

		/// <summary>
		/// Name of NAVAID
		/// _Src: NAV_BASE.csv(NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>NoRemarksYet</remarks>
		public string Name { get; set; }

		/// <summary>
		/// State Name
		/// _Src: NAV_BASE.csv(STATE_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? StateName { get; set; }

		/// <summary>
		/// Region Code
		/// _Src: NAV_BASE.csv(REGION_CODE)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>AAL=Alaska _ ACE=Central _ AEA=Eastern _ AGL=Great Lakes _ ANE=New England _ ANM=Northwest Mountain _ ASO=Southern _ ASW=Southwest _ AWP=Western-Pacific</remarks>
		public string? RegionCode { get; set; }

		/// <summary>
		/// Country Name
		/// _Src: NAV_BASE.csv(COUNTRY_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryName { get; set; }

		/// <summary>
		/// Name of FAN MARKER
		/// _Src: NAV_BASE.csv(FAN_MARKER)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? FanMarker { get; set; }

		/// <summary>
		/// NAVAID OWNER CODE - NAME
		/// _Src: NAV_BASE.csv(OWNER)
		/// _MaxLength: 50
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>A Concatenation of the NAVAID OWNER CODE - NAVAID OWNER NAME</remarks>
		public string? Owner { get; set; }

		/// <summary>
		/// NAVAID OPERATOR CODE - AME
		/// _Src: NAV_BASE.csv(OPERATOR)
		/// _MaxLength: 50
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>A Concatenation of the NAVAID OPERATOR CODE - NAVAID OPERATOR NAME</remarks>
		public string? Operator { get; set; }

		/// <summary>
		/// Common System Usage Flag
		/// _Src: NAV_BASE.csv(NAS_USE_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Common System Usage (Y or N) Defines how the NAVAID is used.</remarks>
		public string NasUseFlag { get; set; }

		/// <summary>
		/// NAVAID PUBLIC USE (Y or N)
		/// _Src: NAV_BASE.csv(PUBLIC_USE_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Defines by whom the NAVAID is used.</remarks>
		public string PublicUseFlag { get; set; }

		/// <summary>
		/// NDB Class Code
		/// _Src: NAV_BASE.csv(NDB_CLASS_CODE)
		/// _MaxLength: 11
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>DME=UHF Standard (TACAN Compatible) Distance Measuring Equipment _ H=NDB (Homing), 50W to greater than 2000W (50 NM) _ HH=NDB (Homing), greater than or equal to 2000W (75 NM) _ LOM=Compass Locator at Outer Marker Site (15 NM) _ MH=NDB (Homing), greater than 50W (25 NM) _ SABH=NDB not authorized for IFR or ATC, provides automatic weather broadcasts _ W=No Voice on Facility Frequency _ Z=VHF Station Location Marker at LF Radio Facility __ Canadian Auxiliary Codes: C=Transcribed Weather Broadcast Station _ B=Scheduled Weather Broadcast _ T=Transmit Only (FSS/ATC, not receive) _ P=PAR Backup Frequency _ L=Power greater than 50W _ M=Power 50W to less than 2000W _ H=Power less than or equal to 2000W _ Z=75 MHz Station Location Marker or Fan Marker __ Multiple codes may be separated by "/" or "-"</remarks>
		public string? NdbClassCode { get; set; }

		/// <summary>
		/// HOURS of Operation of NAVAID
		/// _Src: NAV_BASE.csv(OPER_HOURS)
		/// _MaxLength: 11
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? OperHours { get; set; }

		/// <summary>
		/// High Altitude Boundary ARTCC Identifier
		/// _Src: NAV_BASE.csv(HIGH_ALT_ARTCC_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Identifier of ARTCC with High Altitude Boundary That the NAVAID Falls Within</remarks>
		public string? HighAltArtccId { get; set; }

		/// <summary>
		/// High Altitude Boundary ARTCC Name
		/// _Src: NAV_BASE.csv(HIGH_ARTCC_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Name of ARTCC with High Altitude Boundary That the NAVAID Falls Within</remarks>
		public string? HighArtccName { get; set; }

		/// <summary>
		/// Low Altitude Boundary ARTCC Identifier
		/// _Src: NAV_BASE.csv(LOW_ALT_ARTCC_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Identifier of ARTCC with Low Altitude Boundary That the NAVAID Falls Within</remarks>
		public string? LowAltArtccId { get; set; }

		/// <summary>
		/// Low Altitude Boundary ARTCC Name
		/// _Src: NAV_BASE.csv(LOW_ARTCC_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Name of ARTCC with Low Altitude Boundary That the NAVAID Falls Within</remarks>
		public string? LowArtccName { get; set; }

		/// <summary>
		/// NAVAID Latitude Degrees
		/// _Src: NAV_BASE.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatDeg { get; set; }

		/// <summary>
		/// NAVAID Latitude Minutes
		/// _Src: NAV_BASE.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatMin { get; set; }

		/// <summary>
		/// NAVAID Latitude Seconds
		/// _Src: NAV_BASE.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatSec { get; set; }

		/// <summary>
		/// NAVAID Latitude Hemisphere
		/// _Src: NAV_BASE.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LatHemis { get; set; }

		/// <summary>
		/// NAVAID Latitude in Decimal Format
		/// _Src: NAV_BASE.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatDecimal { get; set; }

		/// <summary>
		/// NAVAID Longitude Degrees
		/// _Src: NAV_BASE.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongDeg { get; set; }

		/// <summary>
		/// NAVAID Longitude Minutes
		/// _Src: NAV_BASE.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongMin { get; set; }

		/// <summary>
		/// NAVAID Longitude Seconds
		/// _Src: NAV_BASE.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongSec { get; set; }

		/// <summary>
		/// NAVAID Longitude Hemisphere
		/// _Src: NAV_BASE.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LongHemis { get; set; }

		/// <summary>
		/// NAVAID Longitude in Decimal Format
		/// _Src: NAV_BASE.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongDecimal { get; set; }

		/// <summary>
		/// Survey Accuracy Code
		/// _Src: NAV_BASE.csv(SURVEY_ACCURACY_CODE)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>0=Unknown _ 1=Degree _ 2=10 Minutes _ 3=1 Minute _ 4=10 Seconds _ 5=1 Second or Better _ 6=NOS _ 7=3rd Order Triangulation</remarks>
		public string? SurveyAccuracyCode { get; set; }

		/// <summary>
		/// TACAN or DME Status
		/// _Src: NAV_BASE.csv(TACAN_DME_STATUS)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Status of TACAN or DME Equipment</remarks>
		public string? TacanDmeStatus { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Latitude Degrees
		/// _Src: NAV_BASE.csv(TACAN_DME_LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Latitude Degrees of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public int? TacanDmeLatDeg { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Latitude Minutes
		/// _Src: NAV_BASE.csv(TACAN_DME_LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Latitude Minutes of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public int? TacanDmeLatMin { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Latitude Seconds
		/// _Src: NAV_BASE.csv(TACAN_DME_LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Latitude Seconds of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public double? TacanDmeLatSec { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Latitude Hemisphere
		/// _Src: NAV_BASE.csv(TACAN_DME_LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Latitude Hemisphere of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public string? TacanDmeLatHemis { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Latitude in Decimal Format
		/// _Src: NAV_BASE.csv(TACAN_DME_LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Latitude Hemisphere of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public double? TacanDmeLatDecimal { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Longitude Degrees
		/// _Src: NAV_BASE.csv(TACAN_DME_LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Longitude Degrees of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public int? TacanDmeLongDeg { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Longitude Minutes
		/// _Src: NAV_BASE.csv(TACAN_DME_LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Longitude Minutes of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public int? TacanDmeLongMin { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Longitude Seconds
		/// _Src: NAV_BASE.csv(TACAN_DME_LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Longitude Seconds of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public double? TacanDmeLongSec { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Longitude Hemisphere
		/// _Src: NAV_BASE.csv(TACAN_DME_LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Longitude Hemisphere of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public string? TacanDmeLongHemis { get; set; }

		/// <summary>
		/// TACAN Portion of VORTAC Longitude in Decimal Format
		/// _Src: NAV_BASE.csv(TACAN_DME_LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Longitude in Decimal Format of TACAN Portion of VORTAC when TACAN is not sited with VOR</remarks>
		public double? TacanDmeLongDecimal { get; set; }

		/// <summary>
		/// Elevation in Tenth of a Foot (MSL)
		/// _Src: NAV_BASE.csv(ELEV)
		/// _MaxLength: (6,1)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		public double? Elev { get; set; }

		/// <summary>
		/// Magnetic Variation in Degrees
		/// _Src: NAV_BASE.csv(MAG_VARN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Indicates magnetic variation in degrees. Not applicable to DME, VOT, and FAN MARKER NAVAID types�any values for these types should be ignored</remarks>
		public int? MagVarn { get; set; }

		/// <summary>
		/// Magnetic Variation Hemisphere
		/// _Src: NAV_BASE.csv(MAG_VARN_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Indicates the direction (East or West) of magnetic variation. Not applicable to DME, VOT, and FAN MARKER NAVAID types�any values for these types should be ignored</remarks>
		public string? MagVarnHemis { get; set; }

		/// <summary>
		/// Magnetic Variation Reference Year
		/// _Src: NAV_BASE.csv(MAG_VARN_YEAR)
		/// _MaxLength: (4,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Represents the year of the magnetic variation reference (epoch). Not applicable to DME, VOT, and FAN MARKER NAVAID types�any values for these types should be ignored</remarks>
		public int? MagVarnYear { get; set; }

		/// <summary>
		/// Simultaneous Voice Feature Flag
		/// _Src: NAV_BASE.csv(SIMUL_VOICE_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? SimulVoiceFlag { get; set; }

		/// <summary>
		/// Power Output (In Watts)
		/// _Src: NAV_BASE.csv(PWR_OUTPUT)
		/// _MaxLength: (4,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		public int? PwrOutput { get; set; }

		/// <summary>
		/// Automatic Voice Identification Feature Flag
		/// _Src: NAV_BASE.csv(AUTO_VOICE_ID_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? AutoVoiceIdFlag { get; set; }

		/// <summary>
		/// Monitoring Category Code
		/// _Src: NAV_BASE.csv(MNT_CAT_CODE)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>1=Internal Monitoring with Status Indicator at Control Point (reverts to Category 3 when control point unmanned) _ 2=Internal Monitoring, Control Point Indicator Inoperative, pilot reports normal ops (temporary, no action required) _ 3=Internal Monitoring Only, no control point indicator _ 4=No Internal Monitor, remote status indicator at control point (NDBs only)</remarks>
		public string? MntCatCode { get; set; }

		/// <summary>
		/// Radio Voice Call (Name) or Trans Signal
		/// _Src: NAV_BASE.csv(VOICE_CALL)
		/// _MaxLength: 60
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? VoiceCall { get; set; }

		/// <summary>
		/// Channel (TACAN) NAVAID Transmits On
		/// _Src: NAV_BASE.csv(CHAN)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Chan { get; set; }

		/// <summary>
		/// Frequency the NAVAID Transmits On (Except TACAN)
		/// _Src: NAV_BASE.csv(FREQ)
		/// _MaxLength: (5,2)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		public double? Freq { get; set; }

		/// <summary>
		/// Transmitted Fan Marker/Marine Radio Beacon Identifier
		/// _Src: NAV_BASE.csv(MKR_IDENT)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? MkrIdent { get; set; }

		/// <summary>
		/// Fan Marker Type
		/// _Src: NAV_BASE.csv(MKR_SHAPE)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>E = ELLIPTICAL</remarks>
		public string? MkrShape { get; set; }

		/// <summary>
		/// Marker Bearing
		/// _Src: NAV_BASE.csv(MKR_BRG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>True Bearing of Major Axis of Fan Marker</remarks>
		public int? MkrBrg { get; set; }

		/// <summary>
		/// VOR Standard Service Volume
		/// _Src: NAV_BASE.csv(ALT_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>T=Terminal (1,000'�12,000' AGL, 25 NM) _ L=Low Altitude (1,000'�18,000' AGL, 40 NM) _ H=High Altitude: 1,000'�14,499' (40 NM), 14,500'�17,999' (100 NM), 18,000'�FL450 (130 NM), above FL450 (100 NM) _ VL=VOR Low: 1,000'�4,999' (40 NM), 5,000'�17,999' (70 NM) _ VH=VOR High: 1,000'�4,999' (40 NM), 5,000'�14,499' (70 NM), 14,500'�17,999' (100 NM), 18,000'�FL450 (130 NM), above FL450 (100 NM)</remarks>
		public string? AltCode { get; set; }

		/// <summary>
		/// DME Standard Service Volume
		/// _Src: NAV_BASE.csv(DME_SSV)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>T=Terminal (1,000'�12,000' AGL, 25 NM) _ L=Low Altitude (1,000'�18,000' AGL, 40 NM) _ H=High Altitude: 1,000'�14,499' (40 NM), 14,500'�17,999' (100 NM), 18,000'�FL450 (130 NM), above FL450 (100 NM) _ DL=DME Low: 12,900'�18,000' (130 NM) _ DH=DME High: 12,900'�FL450 (130 NM), above FL450 (100 NM)</remarks>
		public string? DmeSsv { get; set; }

		/// <summary>
		/// Low Altitude Facility Used in High Structure
		/// _Src: NAV_BASE.csv(LOW_NAV_ON_HIGH_CHART_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? LowNavOnHighChartFlag { get; set; }

		/// <summary>
		/// NAVAID Z Marker Available
		/// _Src: NAV_BASE.csv(Z_MKR_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? ZMkrFlag { get; set; }

		/// <summary>
		/// Associated/Controlling FSS (IDENT)
		/// _Src: NAV_BASE.csv(FSS_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? FssId { get; set; }

		/// <summary>
		/// Associated/Controlling FSS (Name)
		/// _Src: NAV_BASE.csv(FSS_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? FssName { get; set; }

		/// <summary>
		/// Hours of Operation of Controlling FSS
		/// _Src: NAV_BASE.csv(FSS_HOURS)
		/// _MaxLength: 65
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? FssHours { get; set; }

		/// <summary>
		/// NOTAM Accountability Code (IDENT)
		/// _Src: NAV_BASE.csv(NOTAM_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? NotamId { get; set; }

		/// <summary>
		/// Quadrant Identification and Range Leg Bearing (LFR Only)
		/// _Src: NAV_BASE.csv(QUAD_IDENT)
		/// _MaxLength: 20
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? QuadIdent { get; set; }

		/// <summary>
		/// Pitch Flag
		/// _Src: NAV_BASE.csv(PITCH_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>NoRemFairly obsolete system. Ref for more info: https://www.faa.gov/documentLibrary/media/Advisory_Circular/AC_90-99.pdf arksYet</remarks>
		public string? PitchFlag { get; set; }

		/// <summary>
		/// Catch Flag
		/// _Src: NAV_BASE.csv(CATCH_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Fairly obsolete system. Ref for more info: https://www.faa.gov/documentLibrary/media/Advisory_Circular/AC_90-99.pdf </remarks>
		public string? CatchFlag { get; set; }

		/// <summary>
		/// SUA/ATCAA Flag
		/// _Src: NAV_BASE.csv(SUA_ATCAA_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? SuaAtcaaFlag { get; set; }

		/// <summary>
		/// NAVAID Restriction Flag
		/// _Src: NAV_BASE.csv(RESTRICTION_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? RestrictionFlag { get; set; }

		/// <summary>
		/// HIWAS Flag
		/// _Src: NAV_BASE.csv(HIWAS_FLAG)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>NoRemarksYet</remarks>
		public string? HiwasFlag { get; set; }

	}
	#endregion

	#region Nav_CKPT Fields
	public class NavCkpt : CommonFields
	{
		/// <summary>
		/// Altitude Only When Checkpoint is in Air
		/// _Src: NAV_CKPT.csv(ALTITUDE)
		/// _MaxLength: (5,0)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		public int? Altitude { get; set; }

		/// <summary>
		/// Bearing of Checkpoint
		/// _Src: NAV_CKPT.csv(BRG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int Brg { get; set; }

		/// <summary>
		/// Air/Ground Code
		/// _Src: NAV_CKPT.csv(AIR_GND_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>A=AIR _ G=GROUND _ G1=GROUND ONE</remarks>
		public string AirGndCode { get; set; }

		/// <summary>
		/// Narrative Description Associated with the Checkpoint in AIR/Ground
		/// _Src: NAV_CKPT.csv(CHK_DESC)
		/// _MaxLength: 75
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string ChkDesc { get; set; }

		/// <summary>
		/// Airport ID
		/// _Src: NAV_CKPT.csv(ARPT_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? ArptId { get; set; }

		/// <summary>
		/// State Code in Which Associated City is Located
		/// _Src: NAV_CKPT.csv(STATE_CHK_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string StateChkCode { get; set; }

	}
	#endregion

	#region Nav_RMK Fields
	public class NavRmk : CommonFields
	{
		/// <summary>
		/// NASR table associated with Remark
		/// _Src: NAV_RMK.csv(TAB_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string TabName { get; set; }

		/// <summary>
		/// Reference Column Name
		/// _Src: NAV_RMK.csv(REF_COL_NAME)
		/// _MaxLength: 30
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>NASR Column name associated with Remark. Non-specific remarks are identified as GENERAL_REMARK</remarks>
		public string RefColName { get; set; }

		/// <summary>
		/// Reference Column Sequence Number
		/// _Src: NAV_RMK.csv(REF_COL_SEQ_NO)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>Sequence number assigned to Reference Column Remark</remarks>
		public int RefColSeqNo { get; set; }

		/// <summary>
		/// Remark
		/// _Src: NAV_RMK.csv(REMARK)
		/// _MaxLength: 600
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Free Form Text that further describes a specific Information Item</remarks>
		public string Remark { get; set; }

	}
	#endregion

}