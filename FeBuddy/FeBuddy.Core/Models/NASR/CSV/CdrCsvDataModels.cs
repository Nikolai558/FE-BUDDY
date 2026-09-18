namespace FeBuddy.Core.Models.NASR.CSV
{
    public class CdrCsvDataModel
    {
        #region Common Fields
        public class CommonFields
        {
            /// <summary>
            /// Route Code
            /// _Src: All Cdr_*.csv files(RCODE)
            /// _MaxLength: 8
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Each CDR is uniquely identified by an eight-character alphanumeric code. The Route Code is a concatenation of the Origin, Destination and an alphanumeric route identifier.</remarks>
            public string Rcode { get; set; }

        }
        #endregion

        #region Cdr Fields
        public class Cdr : CommonFields
        {
            /// <summary>
            /// CDR Point of Origin
            /// _Src: CDR.csv(ORIG)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>3 or 4 character Departure airport designator.</remarks>
            public string Orig { get; set; }

            /// <summary>
            /// CDR Point of Destination
            /// _Src: CDR.csv(DEST)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>3 or 4 character Destination airport designator.</remarks>
            public string Dest { get; set; }

            /// <summary>
            /// The Departure Fix associated with a given CDR.
            /// _Src: CDR.csv(DEPFIX)
            /// _MaxLength: 6
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string Depfix { get; set; }

            /// <summary>
            /// The preplanned route of flight associated with a given CDR.
            /// _Src: CDR.csv(ROUTE STRING)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string RouteString { get; set; }

            /// <summary>
            /// Departure ARTCC associated with a given CDR
            /// _Src: CDR.csv(DCNTR)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string Dcntr { get; set; }

            /// <summary>
            /// Arrival ARTCC associated with a given CDR
            /// _Src: CDR.csv(ACNTR)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string Acntr { get; set; }

            /// <summary>
            /// A list of all Traversed ARTCCs for a given CDR.
            /// _Src: CDR.csv(TCNTRS)
            /// _MaxLength: 100
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Tcntrs { get; set; }

            /// <summary>
            /// Coordination is required
            /// _Src: CDR.csv(COORDREQ)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Y/N indicator</remarks>
            public string Coordreq { get; set; }

            /// <summary>
            /// The Playbook Play name for a given CDR.
            /// _Src: CDR.csv(PLAY)
            /// _MaxLength: 25
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Play { get; set; }

            /// <summary>
            /// Navigation Equipment Designator
            /// _Src: CDR.csv(NAVEQP)
            /// _MaxLength: (1,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            /// <remarks>1=Basic Navigational Routes _ 2=Routes with RNAV DPs and/or STARs _ 3=Routes with Q-route segments and/or pitch and catch points</remarks>
            public int Naveqp { get; set; }

            /// <summary>
            /// Length of CDR in Nautical Miles
            /// _Src: CDR.csv(LENGTH)
            /// _MaxLength: (5,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? Length { get; set; }

        }
        #endregion

    }
}