namespace FEBuddyLibrary.Models.NASR.CSV
{
    public class HpfCsvDataModel
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
            /// <remarks>The 28 Day NASR Subscription Effective Date in format ‘YYYY/MM/DD’.</remarks>
            public string EffDate { get; set; }

            /// <summary>
            /// Holding Pattern Name/Identifier
            /// _Src: All Hpf_*.csv files(HP_NAME)
            /// _MaxLength: 80
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Format: (NAVAID_NAME FACILITY_TYPE*STATE_CODE) or (FIX_NAME FIX_TYPE*STATE_CODE*ICAO_REGION_CODE)</remarks>
            public string HpName { get; set; }

            /// <summary>
            /// Pattern Number
            /// _Src: All Hpf_*.csv files(HP_NO)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            public int HpNo { get; set; }

            /// <summary>
            /// State Code
            /// _Src: All Hpf_*.csv files(STATE_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Associated State Post Office Code standard two letter abbreviation for US States and Territories</remarks>
            public string? StateCode { get; set; }

            /// <summary>
            /// Post Office County Code
            /// _Src: All Hpf_*.csv files(COUNTRY_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string CountryCode { get; set; }

        }
        #endregion

        #region Hpf_BASE Fields
        public class HpfBase : CommonFields
        {
            /// <summary>
            /// Fix with which Holding is Associated
            /// _Src: HPF_BASE.csv(FIX_ID)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? FixId { get; set; }

            /// <summary>
            /// ICAO Region Code
            /// _Src: HPF_BASE.csv(ICAO_REGION_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? IcaoRegionCode { get; set; }

            /// <summary>
            /// NAVAID ID
            /// _Src: HPF_BASE.csv(NAV_ID)
            /// _MaxLength: 6
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>NoRemarksYet</remarks>
            public string? NavId { get; set; }

            /// <summary>
            /// NAVAID Type
            /// _Src: HPF_BASE.csv(NAV_TYPE)
            /// _MaxLength: 25
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? NavType { get; set; }

            /// <summary>
            /// Holding Direction
            /// _Src: HPF_BASE.csv(HOLD_DIRECTION)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Direction of Holding on the NAVAID or Fix</remarks>
            public string? HoldDirection { get; set; }

            /// <summary>
            /// Hold Degrees/Course
            /// _Src: HPF_BASE.csv(HOLD_DEG_OR_CRS)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Magnetic Bearing, Radial (Degrees) or Course Direction of Holding</remarks>
            public string? HoldDegOrCrs { get; set; }

            /// <summary>
            /// Azimuth
            /// _Src: HPF_BASE.csv(AZIMUTH)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Azimuth (Degrees Shown Above is a Radial, Course, Bearing, or RNAV Track)</remarks>
            public string Azimuth { get; set; }

            /// <summary>
            /// Inbound Course
            /// _Src: HPF_BASE.csv(COURSE_INBOUND_DEG)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? CourseInboundDeg { get; set; }

            /// <summary>
            /// Turning Direction
            /// _Src: HPF_BASE.csv(TURN_DIRECTION)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string TurnDirection { get; set; }

            /// <summary>
            /// Leg Length Outbound DME (NM)
            /// _Src: HPF_BASE.csv(LEG_LENGTH_DIST)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? LegLengthDist { get; set; }

        }
        #endregion

        #region Hpf_CHRT Fields
        public class HpfChrt : CommonFields
        {
            /// <summary>
            /// Chart on Which Holding Pattern is To Be Depicted
            /// _Src: HPF_CHRT.csv(CHARTING_TYPE_DESC)
            /// _MaxLength: 22
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string ChartingTypeDesc { get; set; }

        }
        #endregion

        #region Hpf_RMK Fields
        public class HpfRmk : CommonFields
        {
            /// <summary>
            /// NASR table associated with Remark
            /// _Src: HPF_RMK.csv(TAB_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string TabName { get; set; }

            /// <summary>
            /// Ref NASR Column Name
            /// _Src: HPF_RMK.csv(REF_COL_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>NASR Column name associated with Remark. Non-specific remarks are identified as GENERAL_REMARK.</remarks>
            public string RefColName { get; set; }

            /// <summary>
            /// Sequence number assigned to Reference Column Remark
            /// _Src: HPF_RMK.csv(REF_COL_SEQ_NO)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            public int RefColSeqNo { get; set; }

            /// <summary>
            /// Remarks
            /// _Src: HPF_RMK.csv(REMARK)
            /// _MaxLength: 300
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Free Form Text that further describes a specific Information Item</remarks>
            public string Remark { get; set; }

        }
        #endregion

        #region Hpf_SpdAlt Fields
        public class HpfSpdAlt : CommonFields
        {
            /// <summary>
            /// Speed Range for Holding Altitude of Record
            /// _Src: HPF_SPD_ALT.csv(SPEED_RANGE)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string SpeedRange { get; set; }

            /// <summary>
            /// Holding Altitude for Speed Range of Record
            /// _Src: HPF_SPD_ALT.csv(ALTITUDE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string Altitude { get; set; }

        }
        #endregion

    }
}