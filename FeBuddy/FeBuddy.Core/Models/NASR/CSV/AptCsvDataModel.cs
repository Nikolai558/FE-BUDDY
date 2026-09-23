namespace FeBuddy.Core.Models.NASR.CSV
{
    /// <summary>
    /// All model properties for the Apt_*.csv files.
    /// </summary>
    public class AptCsvDataModel
    {
        #region Common Fields
        /// <summary>
        /// Contains all model properties that are common in multiple Apt_*.csv files.
        /// </summary>
        public class CommonFields
        {
            /// <summary>
            /// Effective Date
            /// _Src: All Apt_*.csv files(EFF_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>The 28 Day NASR Subscription Effective Date in format ‘YYYY/MM/DD’.</remarks>
            public string EffDate { get; set; }
            /// <summary>
            /// Landing Facility Site Number
            /// _Src: All Apt_*.csv files(SITE_NO)
            /// _MaxLength: 9
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>A unique identifying number but can have a leading zero, contains periods in odd places therefore, keep as string. Examples: "01975.12" and "01979."</remarks>
            public string SiteNo { get; set; }
            /// <summary>
            /// Landing Facility Type Code
            /// _Src: All Apt_*.csv files(SITE_TYPE_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Facility Codes: AIRPORT (A) _ BALLOONPORT (B) _ SEAPLANE BASE (C) _ GLIDERPORT (G) _ HELIPORT (H) _ ULTRALIGHT (U)</remarks>
            public string SiteTypeCode { get; set; }
            /// <summary>
            /// Associated State Post Office Code
            /// _Src: All Apt_*.csv files(STATE_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Standard two letter abbreviation for US States and Territories.</remarks>
            public string? StateCode { get; set; }
            /// <summary>
            /// Location Identifier (FAA)
            /// _Src: All Apt_*.csv files(ARPT_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Unique 3-4 character alphanumeric identifier assigned to the Landing Facility.</remarks>
            public string ArptId { get; set; }
            /// <summary>
            /// Airport Associated City Name
            /// _Src: All Apt_*.csv files(CITY)
            /// _MaxLength: 40
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Name of city the airport is in.</remarks>
            public string City { get; set; }
            /// <summary>
            /// Country Code
            /// _Src: All Apt_*.csv files(COUNTRY_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Country Post Office Code Airport Located. Ex: "US"</remarks>
            public string CountryCode { get; set; }
        }
        #endregion
        #region Apt_ARS Fields
        /// <summary>
        /// Contains all model properties from APT_ARS.csv
        /// </summary>
        public class AptArs : CommonFields
        {
            /// <summary>
            /// Runway Identification
            /// _Src: APT_ARS.csv(RWY_ID)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other Apt_*.csv files. Example: "03L/21R"</remarks>
            public string ArsRwyId { get; set; }
            /// <summary>
            /// Runway Identification
            /// _Src: APT_ARS.csv(RWY_END_ID)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other APT_*.csv files. Ex: "03L" </remarks>
            public string ArsRwyEndId { get; set; }
            /// <summary>
            /// Aircraft Arresting Device (Jet Arresting Barrier at Far End)
            /// _Src: APT_ARS.csv(ARREST_DEVICE_CODE)
            /// _MaxLength: 9
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: BAK-6 _ BAK-9 _ BAK-12 _ BAK-12B _ BAK-13 _ BAK-14 _ E5 _ E5-1 _ E27 _ E27B _ E28 _ E28B _ EMAS _ M21 _ MA-1 _ MA-1A _ MA-1A MOD</remarks>
            public string ArrestDeviceCode { get; set; }
        }
        #endregion
        #region Apt_ATT Fields
        /// <summary>
        /// Contains all model properties from APT_ATT.csv
        /// </summary>
        public class AptAtt : CommonFields
        {
            /// <summary>
            /// Attendance Schedule Sequence Number
            /// _Src: APT_ATT.csv(SKED_SEQ_NO)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>A Number which, together with the Site Number, uniquely identifies the Attendance Schedule Component.</remarks>
            public int SkedSeqNo { get; set; }
            /// <summary>
            /// Months Facility Attended
            /// _Src: APT_ATT.csv(MONTH)
            /// _MaxLength: 50
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Describes the months that the facility is attended. May also contain 'UNATNDD' for unattended facilities. Examples: "NOV-APR" "ALL" "UNATNDD" "IREG"</remarks>
            public string? Month { get; set; }
            /// <summary>
            /// Describes the Days of the Week that the Facility is Open
            /// _Src: APT_ATT.csv(DAY)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "ALL" "MON-SAT" "SUN"</remarks>
            public string? Day { get; set; }
            /// <summary>
            /// Describes the Hours of the day that the Facility is Open
            /// _Src: APT_ATT.csv(HOUR)
            /// _MaxLength: 40
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Describes the Hours within the Day that the Facility is Attended. Ex: "0700-1900" "ALL" "0700-DUSK"</remarks>
            public string? Hour { get; set; }
        }
        #endregion
        #region Apt_BASE Fields
        /// <summary>
        /// Contains all model properties from APT_BASE.csv
        /// </summary>
        public class AptBase : CommonFields
        {
            /// <summary>
            /// FAA Region Code
            /// _Src: APT_BASE.csv(REGION_CODE)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>FAA region codes and their corresponding region names: ALASKA (AAL), CENTRAL (ACE), EASTERN (AEA), GREAT LAKES (AGL), NEW ENGLAND (ANE), NORTHWEST MOUNTAIN (ANM), SOUTHERN (ASO), SOUTHWEST (ASW), WESTERN-PACIFIC (AWP).</remarks>
            public string? RegionCode { get; set; }
            /// <summary>
            /// FAA District or Field Office Code
            /// _Src: APT_BASE.csv(ADO_CODE)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "JAN" "MEM" "ATL"</remarks>
            public string? AdoCode { get; set; }
            /// <summary>
            /// Associated State Name
            /// _Src: APT_BASE.csv(STATE_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "JAN" "MEM" "ATL"</remarks>
            public string? StateName { get; set; }
            /// <summary>
            /// Associated County or Parish Name
            /// _Src: APT_BASE.csv(COUNTY_NAME)
            /// _MaxLength: 21
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>For Non-Us Aerodromes This May Be Territory Or Province Name.</remarks>
            public string CountyName { get; set; }
            /// <summary>
            /// Associated County's State (Post Office Code)
            /// _Src: APT_BASE.csv(COUNTY_ASSOC_STATE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Indicates the state (post office code) where the associated county is located, which may differ from the associated city's state code. For non-US aerodrome facilities, these "state" codes are internal to the system and may not align with standard state or country codes. Examples of nonstandard "COUNTY_ASSOC_STATE" and "COUNTY" names include: ANGUILLA (AI), NETHERLANDS ANTILLES (AN), AMERICAN SAMOA (AS), BERMUDA (BM), BAHAMAS (BS), and various Canadian provinces (e.g., B.C., Quebec, Alberta). Additional entries include territories like GUAM (GU), PALAU (PW), and MIDWAY ISLAND (QM).</remarks>
            public string CountyAssocState { get; set; }
            /// <summary>
            /// Official Facility Name
            /// _Src: APT_BASE.csv(ARPT_NAME)
            /// _MaxLength: 50
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: "ADDISON MUNI" "STRICKLAND/SMALLEY FLD"</remarks>
            public string ArptName { get; set; }
            /// <summary>
            /// Airport Ownership Type
            /// _Src: APT_BASE.csv(OWNERSHIP_TYPE_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Indicates the ownership type of the airport. Possible values: PUBLICLY OWNED (PU), PRIVATELY OWNED (PR), AIR FORCE OWNED (MA), NAVY OWNED (MN), ARMY OWNED (MR), COAST GUARD OWNED (CG).</remarks>
            public string OwnershipTypeCode { get; set; }
            /// <summary>
            /// Facility Use
            /// _Src: APT_BASE.csv(FACILITY_USE_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Describes the use of the facility. Possible values: OPEN TO THE PUBLIC (PU), PRIVATE (PR).</remarks>
            public string FacilityUseCode { get; set; }
            /// <summary>
            /// Airport Reference Point Latitude Degrees
            /// _Src: APT_BASE.csv(LAT_DEG)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>-90 through 90. Examples: "-90" "0" "7" "21" "90"</remarks>
            public int LatDeg { get; set; }
            /// <summaAirport Reference Point Latitude Minutesry>
            /// 
            /// _Src: APT_BASE.csv(LAT_MIN)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>A whole number between 0 and 59</remarks>
            public int LatMin { get; set; }
            /// <summary>
            /// Airport Reference Point Latitude Seconds
            /// _Src: APT_BASE.csv(LAT_SEC)
            /// _MaxLength: (6,4)
            /// _DataType: double
            /// _Nullable: No
            /// </summary>
            /// <remarks>0 through 60. Includes milliseconds as decimals. Examples: "0.8" "1.71" "51"</remarks>
            public double LatSec { get; set; }
            /// <summary>
            /// Airport Reference Point Latitude Hemisphere
            /// _Src: APT_BASE.csv(LAT_HEMIS)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other APT_*.csv files N, S, E, or W</remarks>
            public string LatHemis { get; set; }
            /// <summary>
            /// Airport Reference Point Latitude in Decimal Format
            /// _Src: APT_BASE.csv(LAT_DECIMAL)
            /// _MaxLength: (10,8)
            /// _DataType: double
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: "31.60022222" "-31.60022222"</remarks>
            public double BaseLatDecimal { get; set; }
            /// <summary>
            /// Airport Reference Point Longitude Degrees
            /// _Src: APT_BASE.csv(LONG_DEG)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>0 through 180. Examples: "0" "85" "180"</remarks>
            public int LongDeg { get; set; }
            /// <summary>
            /// Airport Reference Point Longitude Minutes
            /// _Src: APT_BASE.csv(LONG_MIN)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>A whole number between 0 and 59</remarks>
            public int LongMin { get; set; }
            /// <summary>
            /// Airport Reference Point Longitude Seconds
            /// _Src: APT_BASE.csv(LONG_SEC)
            /// _MaxLength: (6,4)
            /// _DataType: double
            /// _Nullable: No
            /// </summary>
            /// <remarks>0 through 60. Includes milliseconds as decimals. Examples: "0.8" "1.71" "51"</remarks>
            public double LongSec { get; set; }
            /// <summary>
            /// Airport Reference Point Longitude Hemisphere
            /// _Src: APT_BASE.csv(LONG_HEMIS)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other APT_*.csv files N, S, E, or W</remarks>
            public string LongHemis { get; set; }
            /// <summary>
            /// Airport Reference Point Longitude in Decimal Format
            /// _Src: APT_BASE.csv(LONG_DECIMAL)
            /// _MaxLength: (11,8)
            /// _DataType: double
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: "31.60022222" "-31.60022222"</remarks>
            public double BaseLongDecimal { get; set; }
            /// <summary>
            /// Airport Reference Point Determination Method
            /// _Src: APT_BASE.csv(SURVEY_METHOD_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the method used to determine the airport reference point. Possible values: ESTIMATED (E), SURVEYED (S).</remarks>
            public string? SurveyMethodCode { get; set; }
            /// <summary>
            /// Airport Elevation
            /// _Src: APT_BASE.csv(ELEV)
            /// _MaxLength: (6,1)
            /// _DataType: double
            /// _Nullable: No
            /// </summary>
            /// <remarks>Elevation measured at the highest point on the centerline of the usable landing surface, expressed to the nearest tenth of a foot above mean sea level (MSL). Examples: "-127.8" "0" "130.2"</remarks>
            public double? Elev { get; set; }
            /// <summary>
            /// Airport Elevation Determination Method
            /// _Src: APT_BASE.csv(ELEV_METHOD_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the method used to determine the airport elevation. Possible values: ESTIMATED (E), SURVEYED (S).</remarks>
            public string? ElevMethodCode { get; set; }
            /// <summary>
            /// Magnetic Variation
            /// _Src: APT_BASE.csv(MAG_VARN)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Whole number only, no E/W or negative prefix.</remarks>
            public int? MagVarn { get; set; }
            /// <summary>
            /// Magnetic Variation Direction
            /// _Src: APT_BASE.csv(MAG_HEMIS)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>E or W</remarks>
            public string? MagHemis { get; set; }
            /// <summary>
            /// Magnetic Variation Epoch Year
            /// _Src: APT_BASE.csv(MAG_VARN_YEAR)
            /// _MaxLength: (4,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Example: "1986"</remarks>
            public int? MagVarnYear { get; set; }
            /// <summary>
            /// Traffic Pattern Altitude
            /// _Src: APT_BASE.csv(TPA)
            /// _MaxLength: (4,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Whole Feet AGL. Examples: "530" "1200"</remarks>
            public int? Tpa { get; set; }
            /// <summary>
            /// Aeronautical Sectional Chart on Which Facility Appears
            /// _Src: APT_BASE.csv(CHART_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "ATLANTA" "MEMPHIS" "NEW ORLEANS"</remarks>
            public string? ChartName { get; set; }
            /// <summary>
            /// Distance from Central Business District of the Associated City to the Airport
            /// _Src: APT_BASE.csv(DIST_CITY_TO_AIRPORT)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "0" "7" "23"</remarks>
            public int? DistCityToAirport { get; set; }
            /// <summary>
            /// Direction of Airport from Central Business District of Associated City
            /// _Src: APT_BASE.csv(DIRECTION_CODE)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Nearest 1/8 Compass Point. "N", "S", "E", or "W"</remarks>
            public string? DirectionCode { get; set; }
            /// <summary>
            /// Land Area Covered by Airport
            /// _Src: APT_BASE.csv(ACREAGE)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>By Acres. Whole number.</remarks>
            public int? Acreage { get; set; }
            /// <summary>
            /// Responsible ARTCC Identifier
            /// _Src: APT_BASE.csv(RESP_ARTCC_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Identifies the FAA Air Route Traffic Control Center (ARTCC) who has control over the airport. Examples: "NZZO" "ZTL" "ZOB"</remarks>
            public string RespArtccId { get; set; }
            /// <summary>
            /// Responsible ARTCC (FAA) Computer Identifier
            /// _Src: APT_BASE.csv(COMPUTER_ID)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: "ZUA" "ZAK" "ZCL"</remarks>
            public string ComputerId { get; set; }
            /// <summary>
            /// Responsible ARTCC Name
            /// _Src: APT_BASE.csv(ARTCC_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: "GUAM" "OAKLAND OCEANIC ARTCC" "LOS ANGELES"</remarks>
            public string ArtccName { get; set; }
            /// <summary>
            /// Tie-In FSS Physically Located On Facility
            /// _Src: APT_BASE.csv(FSS_ON_ARPT_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates whether the tie-in Flight Service Station (FSS) is physically located on the airport. Possible values: TIE-IN FSS IS ON THE AIRPORT (Y), TIE-IN FSS IS NOT ON AIRPORT (N).</remarks>
            public string? FssOnArptFlag { get; set; }
            /// <summary>
            /// Tie-In Flight Service Station (FSS) Identifier
            /// _Src: APT_BASE.csv(FSS_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: "ANB" "HUF"</remarks>
            public string FssId { get; set; }
            /// <summary>
            /// Tie-In FSS Name
            /// _Src: APT_BASE.csv(FSS_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: "ANNISTON" "TERRE HAUTE"</remarks>
            public string FssName { get; set; }
            /// <summary>
            /// Local Phone Number from Airport to FSS for Administrative Services
            /// _Src: APT_BASE.csv(PHONE_NO)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other Apt_*.csv files. Examples: "334-222-6598" "(256) 241-7171"</remarks>
            public string? BasePhoneNo { get; set; }
            /// <summary>
            /// Local Phone Number from Airport to FSS for Administrative Services
            /// _Src: APT_BASE.csv(TOLL_FREE_NO)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "1-800-WX-BRIEF" "1-866-864-1737" "LC842-5275"</remarks>
            public string? TollFreeNo { get; set; }
            /// <summary>
            /// Alternate FSS Identifier
            /// _Src: APT_BASE.csv(ALT_FSS_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Provides the identifier of a full-time Flight Service Station that assumes responsibility for the Airport during the off hours of a part-time primary FSS. Examples: "ENA" "FAI"</remarks>
            public string? AltFssId { get; set; }
            /// <summary>
            /// Alternate FSS Name
            /// _Src: APT_BASE.csv(ALT_FSS_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "KENAI" "FAIRBANKS"</remarks>
            public string? AltFssName { get; set; }
            /// <summary>
            /// Toll Free Phone Number from Airport to Alternate FSS for Pilot Briefing Services
            /// _Src: APT_BASE.csv(ALT_TOLL_FREE_NO)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>"1-866-248-6516"</remarks>
            public string? AltTollFreeNo { get; set; }
            /// <summary>
            /// NOTAM and Weather Facility Identifier
            /// _Src: APT_BASE.csv(NOTAM_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Identifies the facility responsible for issuing NOTAMs and weather information for the airport. Examples: "AKK" "ENA"</remarks>
            public string? NotamId { get; set; }
            /// <summary>
            /// Availability of NOTAM 'D' Service at Airport
            /// _Src: APT_BASE.csv(NOTAM_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates whether NOTAM 'D' service is available at the airport. Possible values: YES (Y), NO (N).</remarks>
            public string? NotamFlag { get; set; }
            /// <summary>
            /// Airport Activation Date
            /// _Src: APT_BASE.csv(ACTIVATION_DATE)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Provides the year and month (YYYY/MM) that the facility was added to the NFDC airport database. Available only for facilities opened since 1981. Examples: "1946/05" "2000/05"</remarks>
            public string? ActivationDate { get; set; }
            /// <summary>
            /// Airport Status Code
            /// _Src: APT_BASE.csv(ARPT_STATUS)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Indicates the operational status of the airport. Possible values: CLOSED INDEFINITELY (CI), CLOSED PERMANENTLY (CP), OPERATIONAL (O).</remarks>
            public string ArptStatus { get; set; }
            /// <summary>
            /// Airport ARFF Certification Type Code
            /// _Src: APT_BASE.csv(FAR_139_TYPE_CODE)
            /// _MaxLength: 5
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the Aircraft Rescue and Firefighting (ARFF) certification type for the airport. Format: class code ('I', 'II', 'III', or 'IV') followed by a single character (A, B, C, D, E, or L). Codes A, B, C, D, E represent full certification under CFR PART 139 and identify the ARFF index. Code L represents limited certification under CFR PART 139. A blank value indicates the facility is not certificated. Examples: "I A" "IV A" "I B" "III A"</remarks>
            public string? Far139TypeCode { get; set; }
            /// <summary>
            ///Airport ARFF Certification Carrier Service Code
            /// _Src: APT_BASE.csv(FAR_139_CARRIER_SER_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the type of air carrier service received by the airport under ARFF certification. Possible values: SCHEDULED AIR CARRIER SERVICE (S) or UNSCHEDULED/NON-CERTIFICATED SERVICE (U).</remarks>
            public string? Far139CarrierSerCode { get; set; }
            /// <summary>
            /// Airport ARFF Certification Date
            /// _Src: APT_BASE.csv(ARFF_CERT_TYPE_DATE)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "1973/05" "2005/12"</remarks>
            public string? ArffCertTypeDate { get; set; }
            /// <summary>
            /// NPIAS/Federal Agreements Code
            /// _Src: APT_BASE.csv(NASP_CODE)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the type of federal agreements existing at the airport. Possible values: (N)-NPIAS, (B)-NAVIGATIONAL FACILITIES ON PRIVATELY OWNED AIRPORTS, (G)-GRANT AGREEMENTS, (H)-ACCESSIBILITY COMPLIANCE, (P)-SURPLUS PROPERTY AGREEMENT UNDER PUBLIC LAW 289, (R)-SURPLUS PROPERTY AGREEMENT UNDER REGULATION 16-WAA, (S)-CONVEYANCE UNDER FAA ACT OR AIRWAY DEVELOPMENT ACT, (V)-ADVANCE PLANNING AGREEMENT, (X)-OBLIGATIONS ASSUMED BY TRANSFER, (Y)-CIVIL RIGHTS ACT TITLE VI ASSURANCES, (Z)-CONVEYANCE UNDER FAA ACT OF 1958, (1)-EXPIRED GRANT AGREEMENT REMAINING IN EFFECT, (2)-EXPIRED FAA AUTHORITY REMAINING IN EFFECT, (3)-EXPIRED AP-4 AGREEMENT, (NONE)-NO GRANT AGREEMENT EXISTS, (BLANK)-NO GRANT AGREEMENT EXISTS. Examples: "N", "NGPY", "NGY3".</remarks>
            public string? NaspCode { get; set; }
            /// <summary>
            /// Airport Airspace Analysis Determination
            /// _Src: APT_BASE.csv(ASP_ANLYS_DTRM_CODE)
            /// _MaxLength: 13
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the result of the airspace analysis for the airport. Possible values: (CONDL)-CONDITIONAL, (NOT ANALYZED)-NOT ANALYZED, (NO OBJECTION)-NO OBJECTION, (OBJECTIONABLE)-OBJECTIONABLE.</remarks>
            public string? AspAnlysDtrmCode { get; set; }
            /// <summary>
            /// Customs Flag
            /// _Src: APT_BASE.csv(CUST_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates whether the facility is designated by the U.S. Department of Homeland Security as an International Airport of Entry for Customs. Possible values: (Y)-YES, (N)-NO.</remarks>
            public string? CustFlag { get; set; }
            /// <summary>
            /// Customs Landing Rights Airport Flag
            /// _Src: APT_BASE.csv(LNDG_RIGHTS_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates whether the facility is designated by the U.S. Department of Homeland Security as a Customs Landing Rights Airport. Customs User Fee Airports are designated with an E80, E80A, or E80C remark "US CUSTOMS USER FEE ARPT." Possible values: (Y)-YES, (N)-NO.</remarks>
            public string? LndgRightsFlag { get; set; }
            /// <summary>
            /// Joint Use Agreement Flag
            /// _Src: APT_BASE.csv(JOINT_USE_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates whether the facility has a Military/Civil Joint Use Agreement permitting civil operations at a military airport. Possible values: (Y)-YES, (N)-NO.</remarks>
            public string? JointUseFlag { get; set; }
            /// <summary>
            /// Military Landing Rights Agreement Flag
            /// _Src: APT_BASE.csv(MIL_LNDG_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates whether the airport has an agreement granting landing rights to the military. Possible values: (Y)-YES, (N)-NO.</remarks>
            public string? MilLndgFlag { get; set; }
            /// <summary>
            /// Airport Inspection Method
            /// _Src: APT_BASE.csv(INSPECT_METHOD_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: (F)-FEDERAL, (S)-STATE, (C)-CONTRACTOR, (1)-5010-1 PUBLIC USE MAILOUT PROGRAM, (2)-5010-2 PRIVATE USE MAILOUT PROGRAM.</remarks>
            public string? InspectMethodCode { get; set; }
            /// <summary>
            /// Inspection Agency
            /// _Src: APT_BASE.csv(INSPECTOR_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Possible values: (F)-FAA Airports Field Personnel, (S)-State Aeronautical Personnel, (C)-Private Contract Personnel, (N)-Owner.</remarks>
            public string InspectorCode { get; set; }
            /// <summary>
            /// Last Physical Inspection Date
            /// _Src: APT_BASE.csv(LAST_INSPECTION)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD.</remarks>
            public string? LastInspection { get; set; }
            /// <summary>
            /// Information Request Completion Date
            /// _Src: APT_BASE.csv(LAST_INFO_RESPONSE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD.</remarks>
            public string? LastInfoResponse { get; set; }
            /// <summary>
            /// Available Fuel Types
            /// _Src: APT_BASE.csv(FUEL_TYPES)
            /// _MaxLength: 40
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Includes various aviation gasolines (e.g., 100LL, UL94), jet fuels (e.g., Jet A, Jet A-1, JP8), and automotive gasoline (MOGAS). Additives and specifications are noted, such as FS–II (Fuel System Icing Inhibitor), CI/LI (Corrosion Inhibitor/Lubricity Improver), and SDA (Static Dissipator Additive).</remarks>
            public string? FuelTypes { get; set; }
            /// <summary>
            /// Airframe Repair Service Type
            /// _Src: APT_BASE.csv(AIRFRAME_REPAIR_SER_CODE)
            /// _MaxLength: 5
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: MAJOR, MINOR, NONE</remarks>
            public string? AirframeRepairSerCode { get; set; }
            /// <summary>
            /// Power Plant Repair Service Type
            /// _Src: APT_BASE.csv(PWR_PLANT_REPAIR_SER)
            /// _MaxLength: 5
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: MAJOR, MINOR, NONE.</remarks>
            public string? PwrPlantRepairSer { get; set; }
            /// <summary>
            /// Bottled Oxygen Type
            /// _Src: APT_BASE.csv(BOTTLED_OXY_TYPE)
            /// _MaxLength: 8
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: HIGH, LOW, HIGH/LOW, NONE.</remarks>
            public string? BottledOxyType { get; set; }
            /// <summary>
            /// Type of Bulk Oxygen Available
            /// _Src: APT_BASE.csv(BULK_OXY_TYPE)
            /// _MaxLength: 8
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: HIGH, LOW, HIGH/LOW, NONE.</remarks>
            public string? BulkOxyType { get; set; }
            /// <summary>
            /// Airport Lighting Schedule
            /// _Src: APT_BASE.csv(LGT_SKED)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Values can be time ranges, "SS-SR" (sunset to sunrise), blank (no schedule), or "SEE RMK" (details in facility remarks).</remarks>
            public string? LgtSked { get; set; }
            /// <summary>
            /// Beacon Lighting Schedule
            /// _Src: APT_BASE.csv(BCN_LGT_SKED)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Values may be time ranges, "SS-SR" (sunset to sunrise), blank (no schedule), or "SEE RMK" (details in facility remarks).</remarks>
            public string? BcnLgtSked { get; set; }
            /// <summary>
            /// Air Traffic Control Facility Type
            /// _Src: APT_BASE.csv(TWR_TYPE_CODE)
            /// _MaxLength: 12
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Possible values: ATCT (TWR), NON-ATCT (NO TWR), ATCT-A/C (TWR+Approach CNTRL), ATCT-RAPCON, ATCT-RATCF, ATCT-TRACON, TRACON.</remarks>
            public string TwrTypeCode { get; set; }
            /// <summary>
            /// Segmented Circle Marker System
            /// _Src: APT_BASE.csv(SEG_CIRCLE_MKR_FLAG)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates if on the airport. Possible values: Y (Yes), N (No), NONE, Y-L (Yes, Lighted).</remarks>
            public string? SegCircleMkrFlag { get; set; }
            /// <summary>
            /// Beacon Lens Color
            /// _Src: APT_BASE.csv(BCN_LENS_COLOR)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Colors signify the type of facility: WG (Land Airport), WY (Seaplane Base), WGY (Heliport), SWG (Military Airport), W/Y/G (Unlighted variants), or N (None).</remarks>
            public string? BcnLensColor { get; set; }
            /// <summary>
            /// Landing Fee for Non-Commercial Users
            /// _Src: APT_BASE.csv(LNDG_FEE_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: Y (Yes), N (No).</remarks>
            public string? LndgFeeFlag { get; set; }
            /// <summary>
            /// Medical Use
            /// _Src: APT_BASE.csv(MEDICAL_USE_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates whether the landing facility is used for medical purposes. Value: Y (Yes).</remarks>
            public string? MedicalUseFlag { get; set; }
            /// <summary>
            /// Airport Position Source
            /// _Src: APT_BASE.csv(ARPT_PSN_SOURCE)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Identifies the source of the airport's geographic position data.</remarks>
            public string? ArptPsnSource { get; set; }
            /// <summary>
            /// Airport Position Source Date
            /// _Src: APT_BASE.csv(POSITION_SRC_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Specifies the date the airport's geographic position data was sourced or last updated. Format: YYYY/MM/DD.</remarks>
            public string? PositionSrcDate { get; set; }
            /// <summary>
            /// Airport Elevation Source
            /// _Src: APT_BASE.csv(ARPT_ELEV_SOURCE)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Identifies the source of the airport's Elevation data.</remarks>
            public string? ArptElevSource { get; set; }
            /// <summary>
            /// Airport Elevation Source Date
            /// _Src: APT_BASE.csv(ELEVATION_SRC_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Identifies the source of the airport's Elevation date data.</remarks>
            public string? ElevationSrcDate { get; set; }
            /// <summary>
            /// Contract Fuel Availability
            /// _Src: APT_BASE.csv(CONTR_FUEL_AVBL)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: Y (Yes), N (No).</remarks>
            public string? ContrFuelAvbl { get; set; }
            /// <summary>
            /// Buoy Transient Storage Facilities
            /// _Src: APT_BASE.csv(TRNS_STRG_BUOY_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: Y (Yes), N (No).</remarks>
            public string? TrnsStrgBuoyFlag { get; set; }
            /// <summary>
            /// Hangar Transient Storage Facilities
            /// _Src: APT_BASE.csv(TRNS_STRG_HGR_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: Y (Yes), N (No).</remarks>
            public string? TrnsStrgHgrFlag { get; set; }
            /// <summary>
            /// Tie-Down Transient Storage Facilities
            /// _Src: APT_BASE.csv(TRNS_STRG_TIE_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: Y (Yes), N (No).</remarks>
            public string? TrnsStrgTieFlag { get; set; }
            /// <summary>
            /// Other Airport Services
            /// _Src: APT_BASE.csv(OTHER_SERVICES)
            /// _MaxLength: 110
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values include: AFRT (Air Freight), AGRI (Crop Dusting), AMB (Air Ambulance), AVNCS (Avionics), BCHGR (Beaching Gear), CARGO (Cargo Handling), CHTR (Charter), GLD (Glider Service), INSTR (Pilot Instruction), PAJA (Parachute Jump Activity), RNTL (Aircraft Rental), SALES (Aircraft Sales), SURV (Annual Surveying), TOW (Glider Towing).</remarks>
            public string? OtherServices { get; set; }
            /// <summary>
            /// Wind Indicator Presence
            /// _Src: APT_BASE.csv(WIND_INDCR_FLAG)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: N (No Wind Indicator), Y (Unlighted Wind Indicator), Y-L (Lighted Wind Indicator).</remarks>
            public string? WindIndcrFlag { get; set; }
            /// <summary>
            /// ICAO Identifier
            /// _Src: APT_BASE.csv(ICAO_ID)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? IcaoId { get; set; }
            /// <summary>
            /// Minimum Operational Network (MON)
            /// _Src: APT_BASE.csv(MIN_OP_NETWORK)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Possible values: Y (Yes), N (No).</remarks>
            public string MinOpNetwork { get; set; }
            /// <summary>
            /// US Customs User Fee Airport Flag
            /// _Src: APT_BASE.csv(USER_FEE_FLAG)
            /// _MaxLength: 26
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates if the airport is designated as a User Fee Airport by US Customs by "US CUSTOMS USER FEE ARPT."</remarks>
            public string? UserFeeFlag { get; set; }
            /// <summary>
            /// Cold Temperature Airport
            /// _Src: APT_BASE.csv(CTA)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Altitude Correction Required At or Below Temperature Given in Celsius.</remarks>
            public string? Cta { get; set; }
        }
        #endregion
        #region Apt_CON Fields
        /// <summary>
        /// Contains all model properties from APT_CON.csv
        /// </summary>
        public class AptCon : CommonFields
        {
            /// <summary>
            /// Contact Title
            /// _Src: APT_CON.csv(TITLE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Title of Contact (MANAGER, OWNER, ASST-MGR, etc.)</remarks>
            public string Title { get; set; }
            /// <summary>
            /// Facility Contact Name
            /// _Src: APT_CON.csv(NAME)
            /// _MaxLength: 35
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Facility Contact Name for Title</remarks>
            public string? Name { get; set; }
            /// <summary>
            /// Title Address 
            /// _Src: APT_CON.csv(ADDRESS1)
            /// _MaxLength: 35
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Address1 { get; set; }
            /// <summary>
            /// Title Address 2
            /// _Src: APT_CON.csv(ADDRESS2)
            /// _MaxLength: 35
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Address2 { get; set; }
            /// <summary>
            /// Title City
            /// _Src: APT_CON.csv(TITLE_CITY)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? TitleCity { get; set; }
            /// <summary>
            /// Title State Abbreviation
            /// _Src: APT_CON.csv(STATE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? State { get; set; }
            /// <summary>
            /// Title Zip Code
            /// _Src: APT_CON.csv(ZIP_CODE)
            /// _MaxLength: 5
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ZipCode { get; set; }
            /// <summary>
            /// Title Zip Plus 4
            /// _Src: APT_CON.csv(ZIP_PLUS_FOUR)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ZipPlusFour { get; set; }
            /// <summary>
            /// Title Phone Number
            /// _Src: APT_CON.csv(PHONE_NO)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other APT_*.csv files.</remarks>
            public string? ConPhoneNo { get; set; }
        }
        #endregion
        #region Apt_RMK Fields
        /// <summary>
        /// Contains all model properties from APT_RMK.csv
        /// </summary>
        public class AptRmk : CommonFields
        {
            /// <summary>
            /// Legacy Remark Element Number
            /// _Src: APT_RMK.csv(LEGACY_ELEMENT_NUMBER)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Specifies the legacy element number corresponding to the LEGACY_ELEMENT_NAME in the TXT APT.txt NASR Subscriber File.</remarks>
            public string LegacyElementNumber { get; set; }
            /// <summary>
            /// Table Name
            /// _Src: APT_RMK.csv(TAB_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>NASR Table name associated with Remark.</remarks>
            public string TabName { get; set; }
            /// <summary>
            /// Column Name
            /// _Src: APT_RMK.csv(REF_COL_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>NASR Column name associated with Remark. Non-specific remarks are identified as GENERAL_REMARK.</remarks>
            public string RefColName { get; set; }
            /// <summary>
            /// Remark Text Element Reference
            /// _Src: APT_RMK.csv(ELEMENT)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Identifies the specific data element the remark text applies to. Elements include various airport, service, runway, and device identifiers such as AIRPORT ATTEND SCHED, AIRPORT CONTACT TITLE, FUEL TYPE, RUNWAY ID, and others. Uniqueness of element is not required in all tables.</remarks>
            public string? Element { get; set; }
            /// <summary>
            /// Remark Sequence Number
            /// _Src: APT_RMK.csv(REF_COL_SEQ_NO)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>Specifies the sequence number assigned to the reference column remark for ordering or identification purposes.</remarks>
            public int RefColSeqNo { get; set; }
            /// <summary>
            /// Remark Text
            /// _Src: APT_RMK.csv(REMARK)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Free Form Text that further describes a specific Information Item.</remarks>
            public string Remark { get; set; }
        }
        #endregion
        #region Apt_RWY Fields
        /// <summary>
        /// Contains all model properties from APT_RWY.csv
        /// </summary>
        public class AptRwy : CommonFields
        {
            /// <summary>
            /// Runway Identification
            /// _Src: APT_RWY.csv(RWY_ID)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other APT_*.csv files</remarks>
            public string RwyRwyId { get; set; }
            /// <summary>
            /// Physical Runway Length
            /// _Src: APT_RWY.csv(RWY_LEN)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>Rounded to Nearest Foot</remarks>
            public int RwyLen { get; set; }
            /// <summary>
            /// Physical Runway Width
            /// _Src: APT_RWY.csv(RWY_WIDTH)
            /// _MaxLength: (4,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>Rounded to Nearest Foot</remarks>
            public int RwyWidth { get; set; }
            /// <summary>
            /// Runway Surface Type
            /// _Src: APT_RWY.csv(SURFACE_TYPE_CODE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the type of surface material used on the runway. Common values include CONC (Concrete), ASPH (Asphalt), GRAVEL, TURF, DIRT, SNOW, ICE, and others. Some runways may list a combination of two types or less common materials such as WOOD, METAL, or CALICHE.</remarks>
            public string? SurfaceTypeCode { get; set; }
            /// <summary>
            /// Runway Surface Condition
            /// _Src: APT_RWY.csv(COND)
            /// _MaxLength: 9
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: EXCELLENT, GOOD, FAIR, POOR, FAILED.</remarks>
            public string? Cond { get; set; }
            /// <summary>
            /// Runway Surface Treatment
            /// _Src: APT_RWY.csv(TREATMENT_CODE)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates any special treatment applied to the runway surface to improve friction or drainage. Possible values: GRVD (Grooved), PFC (Porous Friction Course), AFSC (Aggregate Friction Seal Coat), RFSC (Rubberized Friction Seal Coat), WC (Wire Comb/Tine), NONE (No Treatment).</remarks>
            public string? TreatmentCode { get; set; }
			/// <summary>
			/// Pavement Classification
			/// _Src: APT_RWY.csv(PAVEMENT_CLASSIFICATION)
			/// _MaxLength: 3
			/// _DataType: string
			/// _Nullable: Yes
			/// </summary>
			/// <remarks>Rating system that expresses the relative load carrying capacity of a pavement in terms of a standard single wheel load. See FAA Advisory Circular 150/5335-5 for Code Definitions and PCR/PCN Determination Formula. This field will be populated with either PCR values or PCN values (but not both), or null. (PCR) PAVEMENT CLASSIFICATION RATING __ (PCN) PAVEMENT CLASSIFICATION NUMBER</remarks>
			public string? PavementClassification { get; set; }
			/// <summary>
			/// Pavement Classification Rating/Number
			/// _Src: APT_RWY.csv(PCN_PCR_NUMBER)
			/// _MaxLength: (4,0)
			/// _DataType: int
			/// _Nullable: Yes
			/// </summary>
			/// <remarks>PCN/PCR rating/number. Refer to FAA Advisory Circular 150/5335-5 for code definitions and calculation methodology.</remarks>
			public int? PcnPcrNumber { get; set; }
			/// <summary>
			/// Pavement Type
			/// _Src: APT_RWY.csv(PAVEMENT_TYPE_CODE)
			/// _MaxLength: 1
			/// _DataType: string
			/// _Nullable: Yes
			/// </summary>
			/// <remarks>Possible values: R (Rigid), F (Flexible).</remarks>
			public string? PavementTypeCode { get; set; }
            /// <summary>
            /// Subgrade Strength
            /// _Src: APT_RWY.csv(SUBGRADE_STRENGTH_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Letters A-F</remarks>
            public string? SubgradeStrengthCode { get; set; }
            /// <summary>
            /// Tire Pressure Code
            /// _Src: APT_RWY.csv(TIRE_PRES_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Letters W-Z</remarks>
            public string? TirePresCode { get; set; }
            /// <summarDetermination Methody>
            /// 
            /// _Src: APT_RWY.csv(DTRM_METHOD_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: T (Technical Evaluation), U (Based on Aircraft Usage).</remarks>
            public string? DtrmMethodCode { get; set; }
            /// <summary>
            /// Runway Edge Light Intensity
            /// _Src: APT_RWY.csv(RWY_LGT_CODE)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: HIGH, MED, LOW, NSTD (Non-Standard), NONE (No Edge Lighting).</remarks>
            public string? RwyLgtCode { get; set; }
            /// <summary>
            /// Runway Length Source
            /// _Src: APT_RWY.csv(RWY_LEN_SOURCE)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: HIGH, MED, LOW, NSTD (Non-Standard), NONE (No Edge Lighting).</remarks>
            public string? RwyLenSource { get; set; }
            /// <summary>
            /// Runway Length Source Date
            /// _Src: APT_RWY.csv(LENGTH_SOURCE_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format YYYY/MM/DD</remarks>
            public string? LengthSourceDate { get; set; }
            /// <summary>
            /// Single Wheel Gear Bearing Capacity
            /// _Src: APT_RWY.csv(GROSS_WT_SW)
            /// _MaxLength: (5,1)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the maximum weight-bearing capacity of the runway for aircraft with single wheel landing gear. Expressed in thousands of pounds.</remarks>
            public double? GrossWtSw { get; set; }
            /// <summary>
            /// Dual Wheel Gear Bearing Capacity
            /// _Src: APT_RWY.csv(GROSS_WT_DW)
            /// _MaxLength: (5,1)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the maximum weight-bearing capacity of the runway for aircraft with dual wheel landing gear. Expressed in thousands of pounds.</remarks>
            public double? GrossWtDw { get; set; }
            /// <summary>
            /// Dual Tandem Wheel Gear Bearing Capacity
            /// _Src: APT_RWY.csv(GROSS_WT_DTW)
            /// _MaxLength: (5,1)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates the maximum weight-bearing capacity of the runway for aircraft with dual tandem (two dual wheels in tandem) landing gear. Expressed in thousands of pounds.</remarks>
            public double? GrossWtDtw { get; set; }
            /// <summary>
            /// Two Dual Wheels in Tandem Wheel Gear Bearing Capacity
            /// _Src: APT_RWY.csv(GROSS_WT_DDTW)
            /// _MaxLength: (5,1)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Runway Weight-Bearing Capacity for Two Dual Wheels in tandem/two dual wheels in double tandem body gear type Landing Gear</remarks>
            public double? GrossWtDdtw { get; set; }
        }
        #endregion
        #region Apt_RWY_END Fields
        /// <summary>
        /// Contains all model properties from APT_RWY_END.csv
        /// </summary>
        public class AptRwyEnd : CommonFields
        {
            /// <summary>
            /// Runway Identification
            /// _Src: APT_RWY_END.csv(RWY_ID)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other APT_*.csv files.Example: "03L/21R"</remarks>
            public string RwyEndRwyId { get; set; }
            /// <summary>
            /// Runway End Identifier
            /// _Src: APT_RWY_END.csv(RWY_END_ID)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other Apt_*.csv files. The Runway End described by the Arresting System Information. AKA-The specific runway by directory rather than bi-directional as the RwyId is. Examples: "03L" "23"</remarks>
            public string RwyEndRwyEndId { get; set; }
            /// <summary>
            /// Runway End True Alignment
            /// _Src: APT_RWY_END.csv(TRUE_ALIGNMENT)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Specifies the True heading of the runway end, rounded to the nearest degree.</remarks>
            public int? TrueAlignment { get; set; }
            /// <summary>
            /// Instrument Landing System Type
            /// _Src: APT_RWY_END.csv(ILS_TYPE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: ILS (Instrument), MLS (Microwave), SDF (Simplified Directional Facility), LOCALIZER, LDA (Localizer-Type Directional Aid), ISMLS (Interim Standard Microwave), ILS/DME, SDF/DME, LOC/DME, LOC/GS, LDA/DME.</remarks>
            public string? IlsType { get; set; }
            /// <summary>
            /// Right Hand Traffic Pattern Indicator
            /// _Src: APT_RWY_END.csv(RIGHT_HAND_TRAFFIC_PAT_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: Y (Yes), N (No).</remarks>
            public string? RightHandTrafficPatFlag { get; set; }
            /// <summary>
            /// Runway Markings Type
            /// _Src: APT_RWY_END.csv(RWY_MARKING_TYPE_CODE)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: PIR (Precision Instrument), NPI (Nonprecision Instrument), BSC (Basic), NRS (Numbers Only), NSTD (Nonstandard), BUOY (Buoys - Seaplane Base), STOL (Short Takeoff and Landing), NONE.</remarks>
            public string? RwyMarkingTypeCode { get; set; }
            /// <summary>
            /// Runway Condition Markings
            /// _Src: APT_RWY_END.csv(RWY_MARKING_COND)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: G (Good), F (Fair), P (Poor).</remarks>
            public string? RwyMarkingCond { get; set; }
            /// <summary>
            /// Latitude Degrees of Physical Runway End
            /// _Src: APT_RWY_END.csv(RWY_END_LAT_DEG)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? RwyEndLatDeg { get; set; }
            /// <summary>
            /// Latitude Minutes of Physical Runway End
            /// _Src: APT_RWY_END.csv(RWY_END_LAT_MIN)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? RwyEndLatMin { get; set; }
            /// <summary>
            /// Latitude Seconds of Physical Runway End
            /// _Src: APT_RWY_END.csv(RWY_END_LAT_SEC)
            /// _MaxLength: (6,4)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? RwyEndLatSec { get; set; }
            /// <summary>
            /// Latitude Hemisphere of Physical Runway End
            /// _Src: APT_RWY_END.csv(RWY_END_LAT_HEMIS)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: N (Northern Hemisphere), S (Southern Hemisphere).</remarks>
            public string? RwyEndLatHemis { get; set; }
            /// <summary>
            /// Latitude of Physical Runway End in Decimal Format
            /// _Src: APT_RWY_END.csv(LAT_DECIMAL)
            /// _MaxLength: (10,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "31.60022222" "-31.60022222"</remarks>
            public double? RwyEndLatDecimal { get; set; }
            /// <summary>
            /// Longitude Degrees of Physical Runway End
            /// _Src: APT_RWY_END.csv(RWY_END_LONG_DEG)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "31.60022222" "-31.60022222"</remarks>
            public int? RwyEndLongDeg { get; set; }
            /// <summary>
            /// Longitude Minutes of Physical Runway End
            /// _Src: APT_RWY_END.csv(RWY_END_LONG_MIN)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks></remarks>
            public int? RwyEndLongMin { get; set; }
            /// <summary>
            /// Longitude Seconds of Physical Runway End
            /// _Src: APT_RWY_END.csv(RWY_END_LONG_SEC)
            /// _MaxLength: (6,4)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? RwyEndLongSec { get; set; }
            /// <summary>
            /// Longitude Hemisphere of Physical Runway End
            /// _Src: APT_RWY_END.csv(RWY_END_LONG_HEMIS)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: E (Eastern Hemisphere), W (Western Hemisphere).</remarks>
            public string? RwyEndLongHemis { get; set; }
            /// <summary>
            /// Longitude of Physical Runway End in Decimal Format
            /// _Src: APT_RWY_END.csv(LONG_DECIMAL)
            /// _MaxLength: (11,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Examples: "31.60022222" "-31.60022222"</remarks>
            public double? RwyEndLongDecimal { get; set; }
            /// <summary>
            /// Elevation at Physical Runway End
            /// _Src: APT_RWY_END.csv(RWY_END_ELEV)
            /// _MaxLength: (6,1)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Feet in MSL</remarks>
            public double? RwyEndElev { get; set; }
            /// <summary>
            /// Threshold Crossing Height
            /// _Src: APT_RWY_END.csv(THR_CROSSING_HGT)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Height (FT AGL) above ground level where the effective visual glide path crosses the runway threshold.</remarks>
            public int? ThrCrossingHgt { get; set; }
            /// <summary>
            /// Visual Glide Path Angle
            /// _Src: APT_RWY_END.csv(VISUAL_GLIDE_PATH_ANGLE)
            /// _MaxLength: (3,2)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Hundredths of Degrees</remarks>
            public double? VisualGlidePathAngle { get; set; }
            /// <summary>
            /// Latitude Degrees at Displaced Threshold
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_LAT_DEG)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Hundredths of Degrees</remarks>
            public int? DisplacedThrLatDeg { get; set; }
            /// <summary>
            /// Latitude Minutes at Displace Threshold
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_LAT_MIN)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? DisplacedThrLatMin { get; set; }
            /// <summary>
            /// Latitude Seconds at Displace Threshold
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_LAT_SEC)
            /// _MaxLength: (6,4)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? DisplacedThrLatSec { get; set; }
            /// <summary>
            /// Latitude Hemisphere at Displace Threshold
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_LAT_HEMIS)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: N (Northern Hemisphere), S (Southern Hemisphere).</remarks>
            public string? DisplacedThrLatHemis { get; set; }
            /// <summary>
            /// Latitude at Displace Threshold in Decimal Format
            /// _Src: APT_RWY_END.csv(LAT_DISPLACED_THR_DECIMAL)
            /// _MaxLength: (10,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? LatDisplacedThrDecimal { get; set; }
            /// <summary>
            /// Longitude Degrees at Displace Threshold
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_LONG_DEG)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? DisplacedThrLongDeg { get; set; }
            /// <summary>
            /// Longitude Minutes at Displace Threshold
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_LONG_MIN)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? DisplacedThrLongMin { get; set; }
            /// <summary>
            /// Longitude Seconds at Displace Threshold
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_LONG_SEC)
            /// _MaxLength: (6,4)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? DisplacedThrLongSec { get; set; }
            /// <summary>
            /// Longitude Hemisphere at Displace Threshold
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_LONG_HEMIS)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible values: E (Eastern Hemisphere), W (Western Hemisphere).</remarks>
            public string? DisplacedThrLongHemis { get; set; }
            /// <summary>
            /// Longitude at Displace Threshold in Decimal Format
            /// _Src: APT_RWY_END.csv(LONG_DISPLACED_THR_DECIMAL)
            /// _MaxLength: (11,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? LongDisplacedThrDecimal { get; set; }
            /// <summary>
            /// Elevation at Displaced Threshold (Feet MSL)
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_ELEV)
            /// _MaxLength: (6,1)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? DisplacedThrElev { get; set; }
            /// <summary>
            /// Displaced Threshold Length
            /// _Src: APT_RWY_END.csv(DISPLACED_THR_LEN)
            /// _MaxLength: (4,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Distance in feet from the runway end to the displaced threshold.</remarks>
            public int? DisplacedThrLen { get; set; }
            /// <summary>
            /// Elevation at Touchdown Zone (Feet MSL)
            /// _Src: APT_RWY_END.csv(TDZ_ELEV)
            /// _MaxLength: (6,1)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? TdzElev { get; set; }
            /// <summary>
            /// Visual Glide Slope Indicators
            /// _Src: APT_RWY_END.csv(VGSI_CODE)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>SAVASI=Simplified Abbreviated Visual Approach Slope Indicator, VASI=Visual Approach Slope Indicator, PAPI=Precision Approach Path Indicator, TRI=Tri-Color Visual Approach Slope Indicator, PSI=Pulsating/Steady Burning Visual Approach Slope Indicator, PNI=Panel-type Slope Indicator System, S2L=2-box SAVASI Left, S2R=2-box SAVASI Right, V2L=2-box VASI Left, V2R=2-box VASI Right, V4L=4-box VASI Left, V4R=4-box VASI Right, V6L=6-box VASI Left, V6R=6-box VASI Right, V12=12-box VASI Both Sides, V16=16-box VASI Both Sides, P2L=2-light PAPI Left, P2R=2-light PAPI Right, P4L=4-light PAPI Left, P4R=4-light PAPI Right, NSTD=Nonstandard System, PVT=Privately Owned System for Private Use, VAS=Non-specific VASI System, NONE=No Slope Indicator, N=No Slope Indicator, TRIL=Tri-Color VASI Left, TRIR=Tri-Color VASI Right, PSIL=Pulsating/Steady VASI Left, PSIR=Pulsating/Steady VASI Right, PNIL=Panel System Left, PNIR=Panel System Right</remarks>
            public string? VgsiCode { get; set; }
            /// <summary>
            /// Runway Visual Range Equipment (RVR)
            /// _Src: APT_RWY_END.csv(RWY_VISUAL_RANGE_EQUIP_CODE)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>T=Touchdown, M=Midfield, R=Rollout, N=No RVR Available, TM=Touchdown and Midfield, TR=Touchdown and Rollout, MR=Midfield and Rollout, TMR=Touchdown, Midfield, and Rollout</remarks>
            public string? RwyVisualRangeEquipCode { get; set; }
            /// <summary>
            /// Runway Visibility Value Equipment (RVV)
            /// _Src: APT_RWY_END.csv(RWY_VSBY_VALUE_EQUIP_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Indicates whether RVV equipment is present at the runway. Possible values: Y=Yes, N=No.</remarks>
            public string? RwyVsbyValueEquipFlag { get; set; }
            /// <summary>
            /// Approach Light System
            /// _Src: APT_RWY_END.csv(APCH_LGT_SYSTEM_CODE)
            /// _MaxLength: 8
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>AFOVRN=Air Force Overrun 1000-ft Standard; ALSAF=3000-ft High Intensity with Centerline Sequenced Flashers; ALSF1=2400-ft High Intensity with Sequenced Flashers, CAT I; ALSF2=2400-ft High Intensity with Sequenced Flashers, CAT II/III; MALS=1400-ft Medium Intensity; MALSF=1400-ft Medium Intensity with Sequenced Flashers; MALSR=1400-ft Medium Intensity with Runway Alignment Indicator Lights; RAIL=Runway Alignment Indicator Lights; SALS=Short Approach Lighting System; SALSF=Short Approach Lighting with Sequenced Flashing Lights; SSALS=Simplified Short Approach Lighting System; SSALF=Simplified Short with Sequenced Flashers; SSALR=Simplified Short with Runway Alignment Indicator Lights; ODALS=Omnidirectional Approach Lighting System; RLLS=Runway Lead-In Light System; MIL OVRN=Military Overrun; NSTD=Nonstandard System; NONE=No Approach Lighting Available.</remarks>
            public string? ApchLgtSystemCode { get; set; }
            /// <summary>
            /// Runway End Identifier Lights (REIL) Availability
            /// _Src: APT_RWY_END.csv(RWY_END_LGTS_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible Values: Y (Yes), N (No)</remarks>
            public string? RwyEndLgtsFlag { get; set; }
            /// <summary>
            /// Runway Centerline Lights Availability
            /// _Src: APT_RWY_END.csv(CNTRLN_LGTS_AVBL_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible Values: Y (Yes), N (No)</remarks>
            public string? CntrlnLgtsAvblFlag { get; set; }
            /// <summary>
            /// Runway End Touchdown Lights Availability
            /// _Src: APT_RWY_END.csv(TDZ_LGT_AVBL_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Possible Values: Y (Yes), N (No)</remarks>
            public string? TdzLgtAvblFlag { get; set; }
            /// <summary>
            /// Controlling Object Description
            /// _Src: APT_RWY_END.csv(OBSTN_TYPE)
            /// _MaxLength: 11
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ObstnType { get; set; }
            /// <summary>
            /// Controlling Object Marked/Lighted
            /// _Src: APT_RWY_END.csv(OBSTN_MRKD_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>M=Marked; L=Lighted; ML=Marked and Lighted; NONE=None</remarks>
            public string? ObstnMrkdCode { get; set; }
            /// <summary>
            /// FAA CFR Part 77 Runway Category
            /// _Src: APT_RWY_END.csv(FAR_PART_77_CODE)
            /// _MaxLength: 5
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Utility Runway with Visual Approach; B(V)=Non-Utility Runway with Visual Approach; A(NP)=Utility Runway with Nonprecision Approach; C=Non-Utility Runway with Nonprecision Approach (Visibility > 3/4 mile); D=Non-Utility Runway with Nonprecision Approach (Visibility ≥ 3/4 mile); PIR=Precision Instrument Runway</remarks>
            public string? FarPart77Code { get; set; }
            /// <summary>
            /// Controlling Object Clearance Slope
            /// _Src: APT_RWY_END.csv(OBSTN_CLNC_SLOPE)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Ratio N:1 representing clearance available to approaching aircraft; if slope is greater than 50:1, value entered is 50.=</remarks>
            public int? ObstnClncSlope { get; set; }
            /// <summary>
            /// Controlling Object Height Above Runway (Feet AGL)
            /// _Src: APT_RWY_END.csv(OBSTN_HGT)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>The Object Is Above The Physical Runway End.</remarks>
            public int? ObstnHgt { get; set; }
            /// <summary>
            /// Controlling Object Distance from Runway End Distance (In Feet)
            /// _Src: APT_RWY_END.csv(DIST_FROM_THR)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>From the Physical Runway End to the Controlling Object. This is measured using the extended runway centerline to a point abeam the object.</remarks>
            public int? DistFromThr { get; set; }
            /// <summary>
            /// Controlling Object Centerline Offset (Feet)
            /// _Src: APT_RWY_END.csv(CNTRLN_OFFSET)
            /// _MaxLength: (4,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Distance in feet that the controlling object is located away from the extended runway centerline measured horizontally on a line perpendicular to the extended runway centerline.</remarks>
            public int? CntrlnOffset { get; set; }
            /// <summary>
            /// Controlling Object Centerline Offset Direction
            /// _Src: APT_RWY_END.csv(CNTRLN_DIR_CODE)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Direction (LEFT or RIGHT) to the controlling object from the extended runway centerline as seen by an approaching pilot.</remarks>
            public string? CntrlnDirCode { get; set; }
            /// <summary>
            /// Runway End Gradient
            /// _Src: APT_RWY_END.csv(RWY_GRAD)
            /// _MaxLength: (4,3)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Direction (LEFT or RIGHT) to the controlling object from the extended runway centerline as seen by an approaching pilot.</remarks>
            public double? RwyGrad { get; set; }
            /// <summary>
            /// Runway End Gradient Direction (Up Or Down)
            /// _Src: APT_RWY_END.csv(RWY_GRAD_DIRECTION)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? RwyGradDirection { get; set; }
            /// <summary>
            /// Runway End Position Source
            /// _Src: APT_RWY_END.csv(RWY_END_PSN_SOURCE)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? RwyEndPsnSource { get; set; }
            /// <summary>
            /// Runway End Position Source Date
            /// _Src: APT_RWY_END.csv(RWY_END_PSN_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? RwyEndPsnDate { get; set; }
            /// <summary>
            /// Runway End Elevation Source
            /// _Src: APT_RWY_END.csv(RWY_END_ELEV_SOURCE)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public string? RwyEndElevSource { get; set; }
            /// <summary>
            /// Runway End Elevation Source Date
            /// _Src: APT_RWY_END.csv(RWY_END_ELEV_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public string? RwyEndElevDate { get; set; }
            /// <summary>
            /// Displaced Threshold Position Source
            /// _Src: APT_RWY_END.csv(DSPL_THR_PSN_SOURCE)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public string? DsplThrPsnSource { get; set; }
            /// <summary>
            /// Displaced Threshold Position Source Date
            /// _Src: APT_RWY_END.csv(RWY_END_DSPL_THR_PSN_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public string? RwyEndDsplThrPsnDate { get; set; }
            /// <summary>
            /// Displaced Threshold Elevation Source
            /// _Src: APT_RWY_END.csv(DSPL_THR_ELEV_SOURCE)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public string? DsplThrElevSource { get; set; }
            /// <summary>
            /// Displaced Threshold Elevation Source Date
            /// _Src: APT_RWY_END.csv(RWY_END_DSPL_THR_ELEV_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public string? RwyEndDsplThrElevDate { get; set; }
            /// <summary>
            /// Touchdown Elevation Source
            /// _Src: APT_RWY_END.csv(TDZ_ELEV_SOURCE)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public string? TdzElevSource { get; set; }
            /// <summary>
            /// Touchdown Elevation Source Date
            /// _Src: APT_RWY_END.csv(RWY_END_TDZ_ELEV_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public string? RwyEndTdzElevDate { get; set; }
            /// <summary>
            /// Takeoff Run Available (TORA), In Feet
            /// _Src: APT_RWY_END.csv(TKOF_RUN_AVBL)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public int? TkofRunAvbl { get; set; }
            /// <summary>
            /// Takeoff Distance Available (TODA), In Feet
            /// _Src: APT_RWY_END.csv(TKOF_DIST_AVBL)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Format: YYYY/MM/DD</remarks>
            public int? TkofDistAvbl { get; set; }
            /// <summary>
            /// Actual Stop Distance Available (ASDA), In Feet
            /// _Src: APT_RWY_END.csv(ACLT_STOP_DIST_AVBL)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? AcltStopDistAvbl { get; set; }
            /// <summary>
            /// Landing Distance Available (LDA), In Feet
            /// _Src: APT_RWY_END.csv(LNDG_DIST_AVBL)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? LndgDistAvbl { get; set; }
            /// <summary>
            /// Available Landing Distance for Land and Hold Short Operations (LAHSO)
            /// _Src: APT_RWY_END.csv(LAHSO_ALD)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? LahsoAld { get; set; }
            /// <summary>
            /// ID of Intersecting Runway Defining Hold Short Point
            /// _Src: APT_RWY_END.csv(RWY_END_INTERSECT_LAHSO)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? RwyEndIntersectLahso { get; set; }
            /// <summary>
            /// LAHSO Description
            /// _Src: APT_RWY_END.csv(LAHSO_DESC)
            /// _MaxLength: 40
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Description of Entity Defining Hold Short Point If Not an Intersecting Runway</remarks>
            public string? LahsoDesc { get; set; }
            /// <summary>
            /// Latitude of LAHSO Hold Short Point (Formatted)
            /// _Src: APT_RWY_END.csv(LAHSO_LAT)
            /// _MaxLength: 14
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Example: 34-39-00.0210N</remarks>
            public string? LahsoLat { get; set; }
            /// <summary>
            /// Latitude of LAHSO Hold Short Point in Decimal Format
            /// _Src: APT_RWY_END.csv(LAT_LAHSO_DECIMAL)
            /// _MaxLength: (10,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Example: 34-39-00.0210N</remarks>
            public double? LatLahsoDecimal { get; set; }
            /// <summary>
            /// Longitude of LAHSO Hold Short Point (Formatted)
            /// _Src: APT_RWY_END.csv(LAHSO_LONG)
            /// _MaxLength: 15
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Example: 086-44-52.9867W</remarks>
            public string? LahsoLong { get; set; }
            /// <summary>
            /// Longitude of LAHSO Hold Short Point in Decimal Format
            /// _Src: APT_RWY_END.csv(LONG_LAHSO_DECIMAL)
            /// _MaxLength: (11,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Example: 086-44-52.9867W</remarks>
            public double? LongLahsoDecimal { get; set; }
            /// <summary>
            /// LAHSO Hold Short Point Lat/Long Source
            /// _Src: APT_RWY_END.csv(LAHSO_PSN_SOURCE)
            /// _MaxLength: 16
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? LahsoPsnSource { get; set; }
            /// <summary>
            /// Hold Short Point Lat/Long Source Date
            /// _Src: APT_RWY_END.csv(RWY_END_LAHSO_PSN_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>(YYYY/MM/DD)</remarks>
            public string? RwyEndLahsoPsnDate { get; set; }
        }
        #endregion
    }
}