namespace FEBuddyLibrary.Models.NASR.CSV
{
    public class DpCsvDataModel
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
            /// DP Commputer ID
            /// _Src: All Apt_*.csv files(DP_COMPUTER_CODE)
            /// _MaxLength: 12
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>FAA-Assigned Computer Identifier for the DP. EX. ADELL6.ADELL</remarks>
            public string DpComputerCode { get; set; }

            /// <summary>
            /// Name Assigned to the Departure Procedure
            /// _Src: All Dp_*.csv files(DP_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string DpName { get; set; }

            /// <summary>
            /// List of all Responsible ARTCCs based on Airports Served
            /// _Src: All Dp_*.csv files(ARTCC)
            /// _MaxLength: 12
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Artcc { get; set; }

        }
        #endregion

        #region Dp_APT Fields
        public class DpApt : CommonFields
        {

            /// <summary>
            /// Body Name
            /// _Src: DP_APT.csv(BODY_NAME)
            /// _MaxLength: 110
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Name of the body (e.g., airway or segment) associated with the airport or runway end; represents the first and last fix of the segment. Examples: "BROWS", "HBT", "BORRN-DILRE"</remarks>
            public string BodyName { get; set; }

            /// <summary>
            /// NoTitleYet
            /// _Src: DP_APT.csv(BODY_SEQ)
            /// _MaxLength: (1,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other DP_*.csv files. In the rare case that Body Name is not Unique for a given DP, the BODY_SEQ will uniquely identify the Segment.</remarks>
            public int AptBodySeq { get; set; }

            /// <summary>
            /// The associated Airport Identifier
            /// _Src: DP_APT.csv(ARPT_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string ArptId { get; set; }

            /// <summary>
            /// The Runway End Identifier if applicable
            /// _Src: DP_APT.csv(RWY_END_ID)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? RwyEndId { get; set; }

        }
        #endregion

        #region Dp_BASE Fields
        public class DpBase : CommonFields
        {
            /// <summary>
            /// Amendment Number (spelled out) .
            /// _Src: DP_BASE.csv(AMENDMENT_NO)
            /// _MaxLength: 5
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>The DP that will be Active on the Effective Date</remarks>
            public string AmendmentNo { get; set; }

            /// <summary>
            /// The First Effective Date for which the DP Amendment became Active
            /// _Src: DP_BASE.csv(DP_AMEND_EFF_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string DpAmendEffDate { get; set; }

            /// <summary>
            /// RNAV Required
            /// _Src: DP_BASE.csv(RNAV_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Y/N Flag determines whether a DP is RNAV required</remarks>
            public string RnavFlag { get; set; }

            /// <summary>
            /// Graphic DP SID or OBSTACLE
            /// _Src: DP_BASE.csv(GRAPHICAL_DP_TYPE)
            /// _MaxLength: 9
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Identifies whether the Graphical DP is type SID or OBSTACLE</remarks>
            public string GraphicalDpType { get; set; }

            /// <summary>
            /// List of Airports Served by the DP
            /// _Src: DP_BASE.csv(SERVED_ARPT)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string ServedArpt { get; set; }

        }
        #endregion

        #region Dp_RTE Fields
        public class DpRte : CommonFields
        {

            /// <summary>
            /// The Segment is identified as either a Transition or Body.
            /// _Src: DP_RTE.csv(ROUTE_PORTION_TYPE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string RoutePortionType { get; set; }

            /// <summary>
            /// The Transition or Body Name
            /// _Src: DP_RTE.csv(ROUTE_NAME)
            /// _MaxLength: 110
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: "ARLYN TRANSITION", "ADEXE", "ABQ-ADYOS"</remarks>
            public string RouteName { get; set; }

            /// <summary>
            /// Body Sequence Identifier
            /// _Src: DP_RTE.csv(BODY_SEQ)
            /// _MaxLength: (1,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>PropertyName changed due to identical column name in other DP_*.csv files. Used to uniquely identify a segment when the Body Name is not unique within a given Departure Procedure (DP)</remarks>
            public int RteBodySeq { get; set; }

            /// <summary>
            /// FAA-Assigned Computer Identifier for the TRANSITION
            /// _Src: DP_RTE.csv(TRANSITION_COMPUTER_CODE)
            /// _MaxLength: 20
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? TransitionComputerCode { get; set; }

            /// <summary>
            /// Point Sequence Number
            /// _Src: DP_RTE.csv(POINT_SEQ)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>Sequencing number in multiples of ten. Points are in order adapted for given Segment</remarks>
            public int PointSeq { get; set; }

            /// <summary>
            /// The FIX or NAVAID adapted on the Segment
            /// _Src: DP_RTE.csv(POINT)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string Point { get; set; }

            /// <summary>
            /// This is the two letter ICAO Region Code for FIX Point Types only
            /// _Src: DP_RTE.csv(ICAO_REGION_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? IcaoRegionCode { get; set; }

            /// <summary>
            /// Specific FIX or NAVAID Type
            /// _Src: DP_RTE.csv(POINT_TYPE)
            /// _MaxLength: 25
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>CN=Computer Navigation Fix _ MR=Military Reporting Point _ MW=Military Waypoint _ NRS=NRS Waypoint _ RADAR=Radar _ RP=Reporting Point _ VFR=VFR Waypoint _ WP=Waypoint _ CONSOLAN=Low-Frequency Long-Distance NAVAID (Transoceanic) _ DME=Distance Measuring Equipment Only _ FAN MARKER=En Route Marker Beacon (FAN, low power FAN, Z Marker) _ MARINE NDB=Marine Non-Directional Beacon _ MARINE NDB/DME=Marine NDB with DME _ NDB=Non-Directional Beacon _ NDB/DME=NDB with DME _ TACAN=Tactical Air Navigation (Azimuth + Distance) _ UHF/NDB=UHF Non-Directional Beacon _ VOR=VHF Omnidirectional Range (Azimuth only) _ VORTAC=VOR + TACAN (Azimuth and DME) _ VOR/DME=VOR with DME _ VOT=VOR Test Facility</remarks>
            public string PointType { get; set; }

            /// <summary>
            /// Next Fix
            /// _Src: DP_RTE.csv(NEXT_POINT)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>The Point that directly follows the current Point on an individual segment</remarks>
            public string? NextPoint { get; set; }

            /// <summary>
            /// APT/RWY associated with Segment
            /// _Src: DP_RTE.csv(ARPT_RWY_ASSOC)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>The list of APT and/or APT/RWY associated with a given Segment.</remarks>
            public string? ArptRwyAssoc { get; set; }

        }
        #endregion

    }
}