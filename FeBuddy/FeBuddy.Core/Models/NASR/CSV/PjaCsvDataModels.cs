namespace FeBuddy.Core.Models.NASR.CSV
{
    public class PjaCsvDataModel
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
            /// Parachute Jump Area ID
            /// _Src: All Pja_*.csv files(PJA_ID)
            /// _MaxLength: 6
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>PJA ID that uniquely identifies a Parachute Jump Area</remarks>
            public string PjaId { get; set; }

        }
        #endregion

        #region Pja_BASE Fields
        public class PjaBase : CommonFields
        {
            /// <summary>
            /// NAVAID Facility Identifier with which PJA is Associated
            /// _Src: PJA_BASE.csv(NAV_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? NavId { get; set; }

            /// <summary>
            /// NoTitleYet
            /// _Src: PJA_BASE.csv(NAV_TYPE)
            /// _MaxLength: 25
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>CONSOLAN=Low-Frequency Long-Distance NAVAID (Transoceanic) _ DME=Distance Measuring Equipment Only _ FAN MARKER=En Route Marker Beacon (FAN, low power FAN, Z Marker) _ MARINE NDB=Marine Non-Directional Beacon _ MARINE NDB/DME=Marine NDB with DME _ NDB=Non-Directional Beacon _ NDB/DME=NDB with DME _ TACAN=Tactical Air Navigation (Azimuth + Slant Range Distance) _ UHF/NDB=UHF Non-Directional Beacon _ VOR=VHF Omnidirectional Range (Azimuth only) _ VORTAC=VOR + TACAN (Azimuth and DME) _ VOR/DME=VOR with DME _ VOT=VOR Test Facility</remarks>
            public string? NavType { get; set; }

            /// <summary>
            /// Azimuth (Degrees) From NAVAID
            /// _Src: PJA_BASE.csv(RADIAL)
            /// _MaxLength: (5,2)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>0-359.99</remarks>
            public double? Radial { get; set; }

            /// <summary>
            /// Distance, In Nautical Miles, From NAVAID
            /// _Src: PJA_BASE.csv(DISTANCE)
            /// _MaxLength: (7,2)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? Distance { get; set; }

            /// <summary>
            /// Name of NAVAID with which PJA is Associated
            /// _Src: PJA_BASE.csv(NAVAID_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? NavaidName { get; set; }

            /// <summary>
            /// State Code
            /// _Src: PJA_BASE.csv(STATE_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? StateCode { get; set; }

            /// <summary>
            /// City
            /// _Src: PJA_BASE.csv(CITY)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? City { get; set; }

            /// <summary>
            /// PJA Latitude (Formatted)
            /// _Src: PJA_BASE.csv(LATITUDE)
            /// _MaxLength: 14
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string Latitude { get; set; }

            /// <summary>
            /// PJA Latitude in Decimal Format
            /// _Src: PJA_BASE.csv(LAT_DECIMAL)
            /// _MaxLength: (10,8)
            /// _DataType: double
            /// _Nullable: No
            /// </summary>
            public double LatDecimal { get; set; }

            /// <summary>
            /// PJA Longitude (Formatted)
            /// _Src: PJA_BASE.csv(LONGITUDE)
            /// _MaxLength: 15
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string Longitude { get; set; }

            /// <summary>
            /// PJA Longitude in Decimal Format
            /// _Src: PJA_BASE.csv(LONG_DECIMAL)
            /// _MaxLength: (11,8)
            /// _DataType: double
            /// _Nullable: No
            /// </summary>
            public double LongDecimal { get; set; }

            /// <summary>
            /// Landing Facility Identifier with which PJA is Associated
            /// _Src: PJA_BASE.csv(ARPT_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ArptId { get; set; }

            /// <summary>
            /// Site Number of Associated Landing Facility
            /// _Src: PJA_BASE.csv(SITE_NO)
            /// _MaxLength: 9
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? SiteNo { get; set; }

            /// <summary>
            /// NoTitleYet
            /// _Src: PJA_BASE.csv(SITE_TYPE_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Facility Codes: AIRPORT (A) _ BALLOONPORT (B) _ SEAPLANE BASE (C) _ GLIDERPORT (G) _ HELIPORT (H) _ ULTRALIGHT (U)</remarks>
            public string? SiteTypeCode { get; set; }

            /// <summary>
            /// PJA Drop Zone Name
            /// _Src: PJA_BASE.csv(DROP_ZONE_NAME)
            /// _MaxLength: 50
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? DropZoneName { get; set; }

            /// <summary>
            /// PJA Maximum Altitude Allowed
            /// _Src: PJA_BASE.csv(MAX_ALTITUDE)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? MaxAltitude { get; set; }

            /// <summary>
            /// PJA Maximum Altitude Allowed Type
            /// _Src: PJA_BASE.csv(MAX_ALTITUDE_TYPE_CODE)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>AGL, MSL, UNR</remarks>
            public string? MaxAltitudeTypeCode { get; set; }

            /// <summary>
            /// PJA Area Radius, in Nautical Miles from Center Point
            /// _Src: PJA_BASE.csv(PJA_RADIUS)
            /// _MaxLength: (4,2)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? PjaRadius { get; set; }

            /// <summary>
            /// Sectional Charting Required (Y/N)
            /// _Src: PJA_BASE.csv(CHART_REQUEST_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ChartRequestFlag { get; set; }

            /// <summary>
            /// PJA to be Published in A/FD (Y/N)
            /// _Src: PJA_BASE.csv(PUBLISH_CRITERIA)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? PublishCriteria { get; set; }

            /// <summary>
            /// Additional Descriptive Text for PJA Area
            /// _Src: PJA_BASE.csv(DESCRIPTION)
            /// _MaxLength: 100
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Description { get; set; }

            /// <summary>
            /// Times of Use Description
            /// _Src: PJA_BASE.csv(TIME_OF_USE)
            /// _MaxLength: 150
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? TimeOfUse { get; set; }

            /// <summary>
            /// FSS Ident with which PJA is Associated
            /// _Src: PJA_BASE.csv(FSS_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? FssId { get; set; }

            /// <summary>
            /// FSS Name with which PJA is Associated
            /// _Src: PJA_BASE.csv(FSS_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? FssName { get; set; }

            /// <summary>
            /// PJA Use Description
            /// _Src: PJA_BASE.csv(PJA_USE)
            /// _MaxLength: 8
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? PjaUse { get; set; }

            /// <summary>
            /// PJA Area Volume
            /// _Src: PJA_BASE.csv(VOLUME)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Volume { get; set; }

            /// <summary>
            /// PJA User Group Name and Description
            /// _Src: PJA_BASE.csv(PJA_USER)
            /// _MaxLength: 150
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? PjaUser { get; set; }

            /// <summary>
            /// Remark
            /// _Src: PJA_BASE.csv(REMARK)
            /// _MaxLength: 600
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Free Form Text that further describes a PJA</remarks>
            public string? Remark { get; set; }

        }
        #endregion

        #region Pja_CON Fields
        public class PjaCon : CommonFields
        {
            /// <summary>
            /// Contact Facility Identifier
            /// _Src: PJA_CON.csv(FAC_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? FacId { get; set; }

            /// <summary>
            /// Contact Facility Name
            /// _Src: PJA_CON.csv(FAC_NAME)
            /// _MaxLength: 50
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string FacName { get; set; }

            /// <summary>
            /// Related Location Identifier
            /// _Src: PJA_CON.csv(LOC_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string LocId { get; set; }

            /// <summary>
            /// Commercial Frequency
            /// _Src: PJA_CON.csv(COMMERCIAL_FREQ)
            /// _MaxLength: (7,3)
            /// _DataType: double
            /// _Nullable: No
            /// </summary>
            public double CommercialFreq { get; set; }

            /// <summary>
            /// Commercial Chart Flag
            /// _Src: PJA_CON.csv(COMMERCIAL_CHART_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string CommercialChartFlag { get; set; }

            /// <summary>
            /// Military Frequency
            /// _Src: PJA_CON.csv(MIL_FREQ)
            /// _MaxLength: (7,3)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? MilFreq { get; set; }

            /// <summary>
            /// Military Chart Flag
            /// _Src: PJA_CON.csv(MIL_CHART_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? MilChartFlag { get; set; }

            /// <summary>
            /// Sector Description Text
            /// _Src: PJA_CON.csv(SECTOR)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Sector { get; set; }

            /// <summary>
            /// Altitude Description Text
            /// _Src: PJA_CON.csv(CONTACT_FREQ_ALTITUDE)
            /// _MaxLength: 20
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ContactFreqAltitude { get; set; }

        }
        #endregion

    }
}