namespace FeBuddy.Core.Infrastructure.Nasr.Models
{
    public class AwosCsvDataModel
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
            /// Weather System Identifier.
            /// _Src: All Awos_*.csv files(ASOS_AWOS_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Unique 3-4 character alphanumeric identifier.</remarks>
            public string AsosAwosId { get; set; }

            /// <summary>
            /// Weather System Type
            /// _Src: All Awos_*.csv files(ASOS_AWOS_TYPE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string AsosAwosType { get; set; }

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

        #region Awos Fields
        public class Awos : CommonFields
        {
            /// <summary>
            /// Commissioning Date
            /// _Src: AWOS.csv(COMMISSIONED_DATE)
            /// _MaxLength: 10
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Decommissioned Weather systems are not included so Dates given are for Commissioning Dates</remarks>
            public string? CommissionedDate { get; set; }

            /// <summary>
            /// Weather associated with NAVAID � Y/N Flag
            /// _Src: AWOS.csv(NAVAID_FLAG)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string NavaidFlag { get; set; }

            /// <summary>
            /// Weather System Latitude Degrees
            /// _Src: AWOS.csv(LAT_DEG)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? LatDeg { get; set; }

            /// <summary>
            /// Weather System Latitude Minutes
            /// _Src: AWOS.csv(LAT_MIN)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? LatMin { get; set; }

            /// <summary>
            /// Weather System Latitude Seconds
            /// _Src: AWOS.csv(LAT_SEC)
            /// _MaxLength: (6,4)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? LatSec { get; set; }

            /// <summary>
            /// Weather System Latitude Hemisphere
            /// _Src: AWOS.csv(LAT_HEMIS)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? LatHemis { get; set; }

            /// <summary>
            /// Weather System Latitude in Decimal Format
            /// _Src: AWOS.csv(LAT_DECIMAL)
            /// _MaxLength: (10,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? LatDecimal { get; set; }

            /// <summary>
            /// Weather System Longitude Degrees
            /// _Src: AWOS.csv(LONG_DEG)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? LongDeg { get; set; }

            /// <summary>
            /// Weather System Longitude Minutes
            /// _Src: AWOS.csv(LONG_MIN)
            /// _MaxLength: (2,0)
            /// _DataType: int
            /// _Nullable: Yes
            /// </summary>
            public int? LongMin { get; set; }

            /// <summary>
            /// Weather System Longitude Seconds
            /// _Src: AWOS.csv(LONG_SEC)
            /// _MaxLength: (6,4)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? LongSec { get; set; }

            /// <summary>
            /// Weather System Longitude Hemisphere
            /// _Src: AWOS.csv(LONG_HEMIS)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? LongHemis { get; set; }

            /// <summary>
            /// Weather System Longitude in Decimal Format
            /// _Src: AWOS.csv(LONG_DECIMAL)
            /// _MaxLength: (11,8)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? LongDecimal { get; set; }

            /// <summary>
            /// Weather System Elevation (Nearest Tenth of a Foot)
            /// _Src: AWOS.csv(ELEV)
            /// _MaxLength: (6,1)
            /// _DataType: double
            /// _Nullable: Yes
            /// </summary>
            public double? Elev { get; set; }

            /// <summary>
            /// Weather System Location Determination Method
            /// _Src: AWOS.csv(SURVEY_METHOD_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>E = ESTIMATED _ S = SURVEYED</remarks>
            public string? SurveyMethodCode { get; set; }

            /// <summary>
            /// Weather System Telephone Number
            /// _Src: AWOS.csv(PHONE_NO)
            /// _MaxLength: 14
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? PhoneNo { get; set; }

            /// <summary>
            /// Weather System Second Telephone Number
            /// _Src: AWOS.csv(SECOND_PHONE_NO)
            /// _MaxLength: 14
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? SecondPhoneNo { get; set; }

            /// <summary>
            /// Landing Facility Site Number when Weather System Located at Airport
            /// _Src: AWOS.csv(SITE_NO)
            /// _MaxLength: 9
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? SiteNo { get; set; }

            /// <summary>
            /// Landing Facility Type Code (when Weather System Located at Airport)
            /// _Src: AWOS.csv(SITE_TYPE_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>A=Airport _ B=Balloonport _ C=Seaplane Base _ G=Gliderport _ H=Heliport _ U=Ultralight</remarks>
            public string? SiteTypeCode { get; set; }

            /// <summary>
            /// Remark associated with Weather System
            /// _Src: AWOS.csv(REMARK)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? Remark { get; set; }

        }
        #endregion

    }
}