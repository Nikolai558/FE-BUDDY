namespace FeBuddy.Core.Models.NASR.CSV
{
    public class AwyCsvDataModel
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
            /// Airways published under CFR
            /// _Src: All Awy_*.csv files(REGULATORY)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Identifies Airways published under 14 CFR (Code of Federal Regulation) Part-71 and Part-95 � Y/N</remarks>
            public string Regulatory { get; set; }

            /// <summary>
            /// Airway Type Identifying General Location of the Airway
            /// _Src: All Awy_*.csv files(AWY_LOCATION)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>A=Alaska Airway _ H=Hawaii Airway _ C=U.S. Contiguous Airway</remarks>
            public string AwyLocation { get; set; }

            /// <summary>
            /// Airway Identifier.
            /// _Src: All Awy_*.csv files(AWY_ID)
            /// _MaxLength: 12
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string AwyId { get; set; }

        }
        #endregion

        #region Awy_BASE Fields
        public class AwyBase : CommonFields
        {
            /// <summary>
            /// Airway Designation
            /// _Src: AWY_BASE.csv(AWY_DESIGNATION)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>A=Amber Colored Airway _ AT=Atlantic Airway _ B=Blue Colored Airway _ BF=Bahama Airway _ G=Green Colored Airway _ J=Jet Airway _ PA=Pacific Airway _ PR=Puerto Rico Airway _ R=Red Colored Airway _ RN=GPS RNAV Airway _ V=VOR Airway</remarks>
            public string AwyDesignation { get; set; }

            /// <summary>
            /// The Last Date for which the AIRWAY Data amended
            /// _Src: AWY_BASE.csv(UPDATE_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string UpdateDate { get; set; }

            /// <summary>
            /// Remark Text
            /// _Src: AWY_BASE.csv(REMARK)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other AWY_*.csv files. Free Form Text that further describes a specific Information Item.</remarks>
            public string? BaseRemark { get; set; }

            /// <summary>
            /// Airway Fix and Navaids
            /// _Src: AWY_BASE.csv(AIRWAY_STRING)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>List of FIX and NAVAID that make up the AIRWAY in order adapted</remarks>
            public string AirwayString { get; set; }

        }
        #endregion

        #region Awy_SEG_ALT Fields
        public class AwySegAlt : CommonFields
        {
            /// <summary>
            /// Point Sequencing number
            /// _Src: AWY_SEG_ALT.csv(POINT_SEQ)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>Sequencing number in multiples of ten. Points are in order adapted for given Airway.</remarks>
            public int PointSeq { get; set; }

            /// <summary>
            /// NAVAID Facility Identifier, FIX Name, or Border Crossing
            /// _Src: AWY_SEG_ALT.csv(FROM_POINT)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Represents the starting point of the segment; may be a NAVAID ID, FIX name, or uniquely numbered Border Crossing segment (non-sequential system-generated value)</remarks>
            public string FromPoint { get; set; }

            /// <summary>
            /// NAVAID Facility or FIX Type (FROM_PT_TYPE)
            /// _Src: AWY_SEG_ALT.csv(FROM_PT_TYPE)
            /// _MaxLength: 25
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>CN=Computer Navigation Fix _ MR=Military Reporting Point _ MW=Military Waypoint _ NRS=NRS Waypoint _ RADAR=Radar _ RP=Reporting Point _ VFR=VFR Waypoint _ WP=Waypoint _ CONSOLAN=Low-Frequency Long-Distance NAVAID (Transoceanic) _ DME=Distance Measuring Equipment Only _ FAN MARKER=En Route Marker Beacon (FAN, low power FAN, Z Marker) _ MARINE NDB=Marine Non-Directional Beacon _ MARINE NDB/DME=Marine NDB with DME _ NDB=Non-Directional Beacon _ NDB/DME=NDB with DME _ TACAN=Tactical Air Navigation (Azimuth + Distance) _ UHF/NDB=UHF Non-Directional Beacon _ VOR=VHF Omnidirectional Range (Azimuth only) _ VORTAC=VOR + TACAN (Azimuth and DME) _ VOR/DME=VOR with DME _ VOT=VOR Test Facility</remarks>
            public string? FromPtType { get; set; }

            /// <summary>
            /// NAVAID Facility Name
            /// _Src: AWY_SEG_ALT.csv(NAV_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? NavName { get; set; }

            /// <summary>
            /// The NAVIAD Facility City
            /// _Src: AWY_SEG_ALT.csv(NAV_CITY)
            /// _MaxLength: 40
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Part of the key for all NAV_*.csv files</remarks>
            public string? NavCity { get; set; }

            /// <summary>
            /// Identifier of Low ARTCC Altitude Boundary
            /// _Src: AWY_SEG_ALT.csv(ARTCC)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>ARTCC the FROM_POINT FIX/NAVAID Falls Within.</remarks>
            public string? Artcc { get; set; }

            /// <summary>
            /// ICAO Region Code
            /// _Src: AWY_SEG_ALT.csv(ICAO_REGION_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>This is the two letter ICAO Region Code for FIX Point Types only.</remarks>
            public string? IcaoRegionCode { get; set; }

            /// <summary>
            /// State Abbreviation
            /// _Src: AWY_SEG_ALT.csv(STATE_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Associated State Post Office Code standard two letter abbreviation for US States and Territories</remarks>
            public string? StateCode { get; set; }

            /// <summary>
            /// Country Post Office Code
            /// _Src: AWY_SEG_ALT.csv(COUNTRY_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? CountryCode { get; set; }

            /// <summary>
            /// The "To" Point
            /// _Src: AWY_SEG_ALT.csv(TO_POINT)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>The To Point that directly follows the current From Point on an individual segment.</remarks>
            public string? ToPoint { get; set; }

            /// <summary>
            /// Segment Magnetic Course
            /// _Src: AWY_SEG_ALT.csv(MAG_COURSE)
            /// _MaxLength: (5,2)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? MagCourse { get; set; }

            /// <summary>
            /// Segment Magnetic Course - Opposite Direction
            /// _Src: AWY_SEG_ALT.csv(OPP_MAG_COURSE)
            /// _MaxLength: (5,2)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? OppMagCourse { get; set; }

            /// <summary>
            /// Distance to Next Point in Segment in Nautical Miles.
            /// _Src: AWY_SEG_ALT.csv(MAG_COURSE_DIST)
            /// _MaxLength: (5,2)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? MagCourseDist { get; set; }

            /// <summary>
            /// NAVAID Changeover Point Facility Identifier
            /// _Src: AWY_SEG_ALT.csv(CHGOVR_PT)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ChgovrPt { get; set; }

            /// <summary>
            /// NAVAID Changeover Point Facility Name
            /// _Src: AWY_SEG_ALT.csv(CHGOVR_PT_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ChgovrPtName { get; set; }

            /// <summary>
            /// Changeover Point Distance (Nautical Miles)
            /// _Src: AWY_SEG_ALT.csv(CHGOVR_PT_DIST)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Distance in nautical miles from the referenced NAVAID facility to the changeover point, applicable when the point is more than one mile from the halfway mark between two NAVAID facilities</remarks>
            public int? ChgovrPtDist { get; set; }

            /// <summary>
            /// Gap Flag Indicator
            /// _Src: AWY_SEG_ALT.csv(AWY_SEG_GAP_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Airway Gap Flag Indicator for when Airway Discontinued � Y/N</remarks>
            public string AwySegGapFlag { get; set; }

            /// <summary>
            /// Gap in Signal Coverage Indicator
            /// _Src: AWY_SEG_ALT.csv(SIGNAL_GAP_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Gap in Signal Coverage Indicator for when Mea established With a Gap in Navigation Signal Coverage - Y/N.</remarks>
            public string SignalGapFlag { get; set; }

            /// <summary>
            /// A Turn Point Not At A NAVAID
            /// _Src: AWY_SEG_ALT.csv(DOGLEG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Y/N. GPS RNAV Routes [Q, T, TK] will have Dogleg=Y at First Point, End Point, And All Turn Points in between.</remarks>
            public string Dogleg { get; set; }

            /// <summary>
            /// The "To" MEA_PT
            /// _Src: AWY_SEG_ALT.csv(NEXT_MEA_PT)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>The To MEA_PT that directly follows the From MEA_PT for an individual Altitude record</remarks>
            public string NextMeaPt { get; set; }

            /// <summary>
            /// Point To Point Minimum Enroute Altitude (MEA)
            /// _Src: AWY_SEG_ALT.csv(MIN_ENROUTE_ALT)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? MinEnrouteAlt { get; set; }

            /// <summary>
            /// Point To Point Minimum Enroute Direction (MEA)
            /// _Src: AWY_SEG_ALT.csv(MIN_ENROUTE_ALT_DIR)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? MinEnrouteAltDir { get; set; }

            /// <summary>
            /// Point To Point Minimum Enroute Altitude (MEA-Opposite Direction)
            /// _Src: AWY_SEG_ALT.csv(MIN_ENROUTE_ALT_OPPOSITE)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? MinEnrouteAltOpposite { get; set; }

            /// <summary>
            /// Point To Point Minimum Enroute Direction (MEA-Opposite Direction)
            /// _Src: AWY_SEG_ALT.csv(MIN_ENROUTE_ALT_OPPOSITE_DIR)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? MinEnrouteAltOppositeDir { get; set; }

            /// <summary>
            /// Point To Point GNSS Minimum Enroute Altitude (Global Navigation Satellite System MEA)
            /// _Src: AWY_SEG_ALT.csv(GPS_MIN_ENROUTE_ALT)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? GpsMinEnrouteAlt { get; set; }

            /// <summary>
            /// Point To Point GNSS Minimum Enroute Direction (Global Navigation Satellite System MEA)
            /// _Src: AWY_SEG_ALT.csv(GPS_MIN_ENROUTE_ALT_DIR)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? GpsMinEnrouteAltDir { get; set; }

            /// <summary>
            /// Point To Point GNSS MEA - Opposite Direction
            /// _Src: AWY_SEG_ALT.csv(GPS_MIN_ENROUTE_ALT_OPPOSITE)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Point To Point GNSS Minimum Enroute Altitude (Global Navigation Satellite System MEA-Opposite Direction)</remarks>
            public int? GpsMinEnrouteAltOpposite { get; set; }

            /// <summary>
            /// Point To Point GNSS Minimum Enroute Direction - Opposite Direction
            /// _Src: AWY_SEG_ALT.csv(GPS_MEA_OPPOSITE_DIR)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Point To Point GNSS Minimum Enroute Direction (Global Navigation Satellite System MEA-Opposite Direction)</remarks>
            public string? GpsMeaOppositeDir { get; set; }

            /// <summary>
            /// Point To Point DME/DME/IRU Minimum Enroute Altitude (MEA)
            /// _Src: AWY_SEG_ALT.csv(DD_IRU_MEA)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? DdIruMea { get; set; }

            /// <summary>
            /// Point To Point DME/DME/IRU Minimum Enroute Direction (MEA)
            /// _Src: AWY_SEG_ALT.csv(DD_IRU_MEA_DIR)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? DdIruMeaDir { get; set; }

            /// <summary>
            /// Point To Point DME/DME/IRU Minimum Enroute Altitude (MEA- Opposite Direction)
            /// _Src: AWY_SEG_ALT.csv(DD_I_MEA_OPPOSITE)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? DdIMeaOpposite { get; set; }

            /// <summary>
            /// Point To Point DME/DME/IRU Minimum Enroute Direction (MEA- Opposite Direction)
            /// _Src: AWY_SEG_ALT.csv(DD_I_MEA_OPPOSITE_DIR)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? DdIMeaOppositeDir { get; set; }

            /// <summary>
            /// Point To Point Minimum Obstruction Clearance Altitude (MOCA)
            /// _Src: AWY_SEG_ALT.csv(MIN_OBSTN_CLNC_ALT)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? MinObstnClncAlt { get; set; }

            /// <summary>
            /// Minimum Crossing Altitude (MCA)
            /// _Src: AWY_SEG_ALT.csv(MIN_CROSS_ALT)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? MinCrossAlt { get; set; }

            /// <summary>
            /// Minimum Crossing Direction (MCA)
            /// _Src: AWY_SEG_ALT.csv(MIN_CROSS_ALT_DIR)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? MinCrossAltDir { get; set; }

            /// <summary>
            /// Minimum Crossing Altitude (MCA) Point
            /// _Src: AWY_SEG_ALT.csv(MIN_CROSS_ALT_NAV_PT)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? MinCrossAltNavPt { get; set; }

            /// <summary>
            /// Minimum Crossing Altitude (MCA- Opposite Direction)
            /// _Src: AWY_SEG_ALT.csv(MIN_CROSS_ALT_OPPOSITE)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? MinCrossAltOpposite { get; set; }

            /// <summary>
            /// Minimum Crossing Direction (MCA- Opposite Direction)
            /// _Src: AWY_SEG_ALT.csv(MIN_CROSS_ALT_OPPOSITE_DIR)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? MinCrossAltOppositeDir { get; set; }

            /// <summary>
            /// FIX Minimum Reception Altitude (MRA)
            /// _Src: AWY_SEG_ALT.csv(MIN_RECEP_ALT)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? MinRecepAlt { get; set; }

            /// <summary>
            /// Point To Point Maximum Authorized Altitude (MAA)
            /// _Src: AWY_SEG_ALT.csv(MAX_AUTH_ALT)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? MaxAuthAlt { get; set; }

            /// <summary>
            /// Identifies whether a given MEA Segment is Unusable � �U�.
            /// _Src: AWY_SEG_ALT.csv(MEA_GAP)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? MeaGap { get; set; }

            /// <summary>
            /// Required Navigation Performance (RNP) value.
            /// _Src: AWY_SEG_ALT.csv(REQD_NAV_PERFORMANCE)
            /// _MaxLength: (4,2)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? ReqdNavPerformance { get; set; }

            /// <summary>
            /// Remark Text
            /// _Src: AWY_SEG_ALT.csv(REMARK)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other AWY_*.csv files. Free Form Text that further describes a specific Information Item.</remarks>
            public string? SegAltRemark { get; set; }

        }
        #endregion

    }
}