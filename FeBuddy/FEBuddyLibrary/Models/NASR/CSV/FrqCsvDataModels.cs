namespace FEBuddyLibrary.Models.NASR.CSV
{
    public class FrqCsvDataModel
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

        }
        #endregion

        #region Frq Fields
        public class Frq : CommonFields
        {
            /// <summary>
            /// Facility Identifier or Name
            /// _Src: FRQ.csv(FACILITY)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Contains FACILITY ID unless the FACILITY TYPE is AFIS, CTAF, GCO, UNICOM, or RCAG. RCAG uses FACILITY NAME instead. AFIS, CTAF, GCO, and UNICOM are NULL (no FACILITY ID or NAME in NASR)</remarks>
            public string? Facility { get; set; }

            /// <summary>
            /// Official Facility Name
            /// _Src: FRQ.csv(FAC_NAME)
            /// _MaxLength: 50
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Represents the official facility name; NULL for FACILITY TYPEs AFIS, CTAF, GCO, UNICOM, and ASOS/AWOS as these do not contain a facility name in NASR</remarks>
            public string? FacName { get; set; }

            /// <summary>
            /// Facility Type
            /// _Src: FRQ.csv(FACILITY_TYPE)
            /// _MaxLength: 12
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>All records include a facility type; RCO and RCO1 both represent a Remote Communication Outlet and serve the same function. RCO1 may indicate an alternate site sharing the same identifier (e.g., one with a NAVAID, one on airport property)</remarks>
            public string FacilityType { get; set; }

            /// <summary>
            /// ARTCC or FSS Identifier
            /// _Src: FRQ.csv(ARTCC_OR_FSS_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>For FACILITY TYPE RCAG: contains the associated ARTCC ID _ For FACILITY TYPE RCO/RCO1: contains the associated FSS ID _ Included for convenience to locate detailed RCAG or RCO/RCO1 information in NASR</remarks>
            public string? ArtccOrFssId { get; set; }

            /// <summary>
            /// CPDLC Remark
            /// _Src: FRQ.csv(CPDLC)
            /// _MaxLength: 100
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Cpdlc { get; set; }

            /// <summary>
            /// ATCT Facility Hours of Operation
            /// _Src: FRQ.csv(TOWER_HRS)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Only listed for ATCT FACILITY TYPEs where the FACILITY equals the SERVICED FACILITY</remarks>
            public string? TowerHrs { get; set; }

            /// <summary>
            /// Serviced Facility
            /// _Src: FRQ.csv(SERVICED_FACILITY)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Indicates the FACILITY ID or, if FACILITY TYPE is RCAG, the FACILITY NAME that is serviced by the listed frequencies; this field is always populated (non-null)</remarks>
            public string ServicedFacility { get; set; }

            /// <summary>
            /// Serviced Facility Name
            /// _Src: FRQ.csv(SERVICED_FAC_NAME)
            /// _MaxLength: 50
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>The FACILITY NAME that is serviced by the frequencies listed</remarks>
            public string? ServicedFacName { get; set; }

            /// <summary>
            /// Serviced Site Type
            /// _Src: FRQ.csv(SERVICED_SITE_TYPE)
            /// _MaxLength: 25
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Facility Type of SERVICED FACILITY</remarks>
            public string? ServicedSiteType { get; set; }

            /// <summary>
            /// Facility Reference Point Latitude in Decimal Format
            /// _Src: FRQ.csv(LAT_DECIMAL)
            /// _MaxLength: (10,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? LatDecimal { get; set; }

            /// <summary>
            /// Facility Reference Point Longitude in Decimal Format.
            /// _Src: FRQ.csv(LONG_DECIMAL)
            /// _MaxLength: (11,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? LongDecimal { get; set; }

            /// <summary>
            /// Serviced Facility Associated City Name
            /// _Src: FRQ.csv(SERVICED_CITY)
            /// _MaxLength: 40
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ServicedCity { get; set; }

            /// <summary>
            /// State Code of the SERVICED FACILITY
            /// _Src: FRQ.csv(SERVICED_STATE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ServicedState { get; set; }

            /// <summary>
            /// Post Code of Serviced Facility
            /// _Src: FRQ.csv(SERVICED_COUNTRY)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? ServicedCountry { get; set; }

            /// <summary>
            /// Radio call used by pilot to contact ATC or FSS facility
            /// _Src: FRQ.csv(TOWER_OR_COMM_CALL)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? TowerOrCommCall { get; set; }

            /// <summary>
            /// Radio call of facility that furnishes primary approach control
            /// _Src: FRQ.csv(PRIMARY_APPROACH_RADIO_CALL)
            /// _MaxLength: 26
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? PrimaryApproachRadioCall { get; set; }

            /// <summary>
            /// Frequency
            /// _Src: FRQ.csv(FREQ)
            /// _MaxLength: 40
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Frequency used by the serviced facility; for NAVAIDs with DME/TACAN channel, shown in format: FREQ/CHAN</remarks>
            public string? Freq { get; set; }

            /// <summary>
            /// Sectorization
            /// _Src: FRQ.csv(SECTORIZATION)
            /// _MaxLength: 50
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Describes frequency usage boundaries based on serviced facility, airway segmentation, or runway limitations. For ARTCC and RCAG, indicates altitude classification: Low, High, Low/High, or Ultra-High</remarks>
            public string? Sectorization { get; set; }

            /// <summary>
            /// Frequency Use Description
            /// _Src: FRQ.csv(FREQ_USE)
            /// _MaxLength: 600
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? FreqUse { get; set; }

            /// <summary>
            /// Remark
            /// _Src: FRQ.csv(REMARK)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Free Form Text that further describes a specific Information Item</remarks>
            public string? Remark { get; set; }

        }
        #endregion

    }
}