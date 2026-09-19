namespace FeBuddy.Core.Models.NASR.CSV
{
    public class ClsArspCsvDataModel
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
            /// Landing Facility Site Number.
            /// _Src: All CLS_ARSP_*.csv files(SITE_NO)
            /// _MaxLength: 9
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>A unique identifying number</remarks>
            public string SiteNo { get; set; }

            /// <summary>
            /// Facility Type Code
            /// _Src: All CLS_ARSP_*.csv files(SITE_TYPE_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>A=Airport _ B=Balloonport _ C=Seaplane Base _ G=Gliderport _ H=Heliport _ U=Ultralight</remarks>
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
            /// Location Identifier.
            /// _Src: All CLS_ARSP_*.csv files(ARPT_ID)
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

        #region ClsArsp Fields
        public class ClsArsp : CommonFields
        {
            /// <summary>
            /// Class B Flag
            /// _Src: CLS_ARSP.csv(CLASS_B_AIRSPACE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Terminal Communication Facility containing Class B Airspace with be designated with �Y� else null</remarks>
            public string? ClassBAirspace { get; set; }

            /// <summary>
            /// Class C Flag
            /// _Src: CLS_ARSP.csv(CLASS_C_AIRSPACE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Terminal Communication Facility containing Class C Airspace with be designated with �Y� else null</remarks>
            public string? ClassCAirspace { get; set; }

            /// <summary>
            /// Class D Flag
            /// _Src: CLS_ARSP.csv(CLASS_D_AIRSPACE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Terminal Communication Facility containing Class D Airspace with be designated with �Y� else null</remarks>
            public string? ClassDAirspace { get; set; }

            /// <summary>
            /// Class E Flag
            /// _Src: CLS_ARSP.csv(CLASS_E_AIRSPACE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Terminal Communication Facility containing Class E Airspace with be designated with �Y� else null</remarks>
            public string? ClassEAirspace { get; set; }

            /// <summary>
            /// Airspace Hours of Terminal Communication Facility
            /// _Src: CLS_ARSP.csv(AIRSPACE_HRS)
            /// _MaxLength: 300
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? AirspaceHrs { get; set; }

            /// <summary>
            /// Remark associated with Class Airspace.
            /// _Src: CLS_ARSP.csv(REMARK)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Remark { get; set; }

        }
        #endregion

    }
}