namespace FeBuddy.Core.Models.NASR.CSV
{
    public class MilOpsCsvDataModel
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

        }
        #endregion

        #region Mil_OPS Fields
        public class MilOps : CommonFields
        {
            /// <summary>
            /// Landing Facility Site Number
            /// _Src: MIL_OPS.csv(SITE_NO)
            /// _MaxLength: 9
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>A unique identifying number.</remarks>
            public string SiteNo { get; set; }

            /// <summary>
            /// Facility Type Code
            /// _Src: MIL_OPS.csv(SITE_TYPE_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>A=Airport _ B=Balloonport _ C=Seaplane Base _ G=Gliderport _ H=Heliport _ U=Ultralight</remarks>
            public string SiteTypeCode { get; set; }

            /// <summary>
            /// State Code
            /// _Src: MIL_OPS.csv(STATE_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? StateCode { get; set; }

            /// <summary>
            /// Location Identifier
            /// _Src: MIL_OPS.csv(ARPT_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Unique 3-4 character alphanumeric identifier assigned to the Landing Facility</remarks>
            public string ArptId { get; set; }

            /// <summary>
            /// City
            /// _Src: MIL_OPS.csv(CITY)
            /// _MaxLength: 40
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string City { get; set; }

            /// <summary>
            /// Country Code
            /// _Src: MIL_OPS.csv(COUNTRY_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string CountryCode { get; set; }

            /// <summary>
            /// Military Agency Type Code that Operates the Control Facility
            /// _Src: MIL_OPS.csv(MIL_OPS_OPER_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>A=U.S. Air Force _ C=U.S. Coast Guard _ F=Federal Aviation Admin _ N=U.S. Navy _ R=U.S. Army</remarks>
            public string? MilOpsOperCode { get; set; }

            /// <summary>
            /// Radio Call Name for Military Operations at this Control Facility
            /// _Src: MIL_OPS.csv(MIL_OPS_CALL)
            /// _MaxLength: 26
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? MilOpsCall { get; set; }

            /// <summary>
            /// Hours of Military Operations Conducted each Day
            /// _Src: MIL_OPS.csv(MIL_OPS_HRS)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? MilOpsHrs { get; set; }

            /// <summary>
            /// Hours of Operation of the Military Aircraft Command Post (AMCP) Located At the Facility
            /// _Src: MIL_OPS.csv(AMCP_HRS)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? AmcpHrs { get; set; }

            /// <summary>
            /// Hours Of Operation Of The Military Pilot-To-Metro Service (PMSV) Located At The Facility
            /// _Src: MIL_OPS.csv(PMSV_HRS)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? PmsvHrs { get; set; }

            /// <summary>
            /// Remark
            /// _Src: MIL_OPS.csv(REMARK)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Remark associated with Military Operations</remarks>
            public string? Remark { get; set; }

        }
        #endregion

    }
}