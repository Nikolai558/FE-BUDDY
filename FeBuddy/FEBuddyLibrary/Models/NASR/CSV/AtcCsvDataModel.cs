namespace FEBuddyLibrary.Models.NASR.CSV
{
    public class AtcCsvDataModel
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
            /// Landing Facility Site Number
            /// _Src: All Atc_*.csv files(SITE_NO)
            /// _MaxLength: 9
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>A unique identifying number but can have a leading zero, contains periods in odd places therefore, keep as string. Examples: "01975.12" and "01979."</remarks>
            public string SiteNo { get; set; }

            /// <summary>
            /// Landing Facility Type Code
            /// _Src: All Atc_*.csv files(SITE_TYPE_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Facility Codes: AIRPORT (A) _ BALLOONPORT (B) _ SEAPLANE BASE (C) _ GLIDERPORT (G) _ HELIPORT (H) _ ULTRALIGHT (U)</remarks>
            public string SiteTypeCode { get; set; }

            /// <summary>
            /// Facility Type
            /// _Src: All Atc_*.csv files(FACILITY_TYPE)
            /// _MaxLength: 12
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>ATCT=Air Traffic Control Tower _ ATCT-A/C=ATCT plus Approach Control _ ATCT-RAPCON=ATCT plus Radar Approach Control (USAF ATCT / FAA Approach) _ ATCT-RATCF=ATCT plus Radar Approach Control (Navy ATCT / FAA Approach) _ ATCT-TRACON=ATCT plus Terminal Radar Approach Control _ NON-ATCT=Non-Air Traffic Control Tower _ TRACON=Consolidated TRACON _ ARTCC=Air Route Traffic Control Center _ CERAP=Center Radar Approach Control Facility</remarks>
            public string FacilityType { get; set; }

            /// <summary>
            /// Associated State Post Office Code
            /// _Src: All Atc_*.csv files(STATE_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Standard two letter abbreviation for US States and Territories.</remarks>
            public string? StateCode { get; set; }

            /// <summary>
            /// Location Identifier.
            /// _Src: All Atc_*.csv files(FACILITY_ID)
            /// _MaxLength: 4
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Unique 3-4 character alphanumeric identifier assigned to the Landing Facility or TRACON.</remarks>
            public string FacilityId { get; set; }

            /// <summary>
            /// Airport Associated City Name
            /// _Src: All Atc_*.csv files(CITY)
            /// _MaxLength: 40
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Name of city the airport is in.</remarks>
            public string City { get; set; }

            /// <summary>
            /// Country Code
            /// _Src: All Atc_*.csv files(COUNTRY_CODE)
            /// _MaxLength: 2
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Country Post Office Code Airport Located. Ex: "US"</remarks>
            public string CountryCode { get; set; }

        }
        #endregion

        #region Atc_ATIS Fields
        public class AtcAtis : CommonFields
        {
            /// <summary>
            /// ATIS Serial Number
            /// _Src: ATC_ATIS.csv(ATIS_NO)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            public int AtisNo { get; set; }

            /// <summary>
            /// Optional Description of Purpose
            /// _Src: ATC_ATIS.csv(DESCRIPTION)
            /// _MaxLength: 100
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Fulfilled by ATIS. Ex: "D-ATIS (ARR)"</remarks>
            public string? Description { get; set; }

            /// <summary>
            /// ATIS Hours of Operation in Local Time
            /// _Src: ATC_ATIS.csv(ATIS_HRS)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Ex: "24" "1400-0400Z++ WKDAYS; 1600-0000Z++ WKENDS, CLSD FED HOLS."</remarks>
            public string AtisHrs { get; set; }

            /// <summary>
            /// ATIS Phone Number
            /// _Src: ATC_ATIS.csv(ATIS_PHONE_NO)
            /// _MaxLength: 18
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? AtisPhoneNo { get; set; }

        }
        #endregion

        #region Atc_BASE Fields
        public class AtcBase : CommonFields
        {
            /// <summary>
            /// ICAO Identifier
            /// _Src: ATC_BASE.csv(ICAO_ID)
            /// _MaxLength: 7
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? IcaoId { get; set; }

            /// <summary>
            /// Official Facility Name
            /// _Src: ATC_BASE.csv(FACILITY_NAME)
            /// _MaxLength: 50
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string FacilityName { get; set; }

            /// <summary>
            /// FAA Region Code
            /// _Src: ATC_BASE.csv(REGION_CODE)
            /// _MaxLength: 3
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>FAA region codes and their corresponding region names: ALASKA (AAL), CENTRAL (ACE), EASTERN (AEA), GREAT LAKES (AGL), NEW ENGLAND (ANE), NORTHWEST MOUNTAIN (ANM), SOUTHERN (ASO), SOUTHWEST (ASW), WESTERN-PACIFIC (AWP).</remarks>
            public string? RegionCode { get; set; }

            /// <summary>
            /// Operator Code of the Agency that Operates the Tower
            /// _Src: ATC_BASE.csv(TWR_OPERATOR_CODE)
            /// _MaxLength: 6
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>A=U.S. Air Force _ C=U.S. Coast Guard _ CITY=CITY _ COUNTY=COUNTY _ D=Canadian Ministry of Transport _ F=Federal Aviation Admin _ FCT=FAA Contract Tower _ G=Federal Gov't - Not U.S.A _ N=U.S. Navy _ NFCT=Non Fed Control Tower _ O=Other _ P=Private _ R=U.S. Army _ W=U.S. Weather Service _ X=Royal Canadian Air Force _ Z=Unknown</remarks>
            public string? TwrOperatorCode { get; set; }

            /// <summary>
            /// Radio Call used by Pilot to Contact Tower
            /// _Src: ATC_BASE.csv(TWR_CALL)
            /// _MaxLength: 26
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? TwrCall { get; set; }

            /// <summary>
            /// Hours of Tower Operation in Local Time
            /// _Src: ATC_BASE.csv(TWR_HRS)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Ex: "0800-1600 MON-FRI"</remarks>
            public string? TwrHrs { get; set; }

            /// <summary>
            /// Radio Call of Primary Approach Control
            /// _Src: ATC_BASE.csv(PRIMARY_APCH_RADIO_CALL)
            /// _MaxLength: 26
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Primary Facility That Furnishes APPCTRL SVC</remarks>
            public string? PrimaryApchRadioCall { get; set; }

            /// <summary>
            /// Primary Approach Control Facility ID
            /// _Src: ATC_BASE.csv(APCH_P_PROVIDER)
            /// _MaxLength: 700
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Facility ID (or Provider Description when Provider Type equals ‘S’) of the Agency That Operates the Primary Approach Control Facility/Functions</remarks>
            public string? ApchPProvider { get; set; }

            /// <summary>
            /// Provider Agency Type Code for Agency that Operates the Primary Approach Control Facility/Functions
            /// _Src: ATC_BASE.csv(APCH_P_PROV_TYPE_CD)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>A=Airport (with ATCT) _ C=ARTCC _ S=Special _ T=TRACON</remarks>
            public string? ApchPProvTypeCd { get; set; }

            /// <summary>
            /// Secondary Approach Control Facility ID
            /// _Src: ATC_BASE.csv(SECONDARY_APCH_RADIO_CALL)
            /// _MaxLength: 26
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Secondary Facility That Furnishes APPCTRL SVC</remarks>
            public string? SecondaryApchRadioCall { get; set; }

            /// <summary>
            /// Primary Approach Control Facility ID
            /// _Src: ATC_BASE.csv(APCH_S_PROVIDER)
            /// _MaxLength: 700
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Facility ID (or Provider Description when Provider Type equals ‘S’) of the Agency That Operates the Secondary Approach Control Facility/Functions</remarks>
            public string? ApchSProvider { get; set; }

            /// <summary>
            /// Secondary Approach Control Facility ID
            /// _Src: ATC_BASE.csv(APCH_S_PROV_TYPE_CD)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>A=Airport (with ATCT) _ C=ARTCC _ S=Special _ T=TRACON</remarks>
            public string? ApchSProvTypeCd { get; set; }

            /// <summary>
            /// Radio Call of Facility That Furnishes Primary Departure Control
            /// _Src: ATC_BASE.csv(PRIMARY_DEP_RADIO_CALL)
            /// _MaxLength: 26
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? PrimaryDepRadioCall { get; set; }

            /// <summary>
            /// Primary Departure Control Facility ID/Description
            /// _Src: ATC_BASE.csv(DEP_P_PROVIDER)
            /// _MaxLength: 700
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Facility ID (or Provider Description when Provider Type equals ‘S’) of the Agency That Operates the Primary Departure Control Facility/Functions</remarks>
            public string? DepPProvider { get; set; }

            /// <summary>
            /// Primary Departure Control Facility ID
            /// _Src: ATC_BASE.csv(DEP_P_PROV_TYPE_CD)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>A=Airport (with ATCT) _ C=ARTCC _ S=Special _ T=TRACON</remarks>
            public string? DepPProvTypeCd { get; set; }

            /// <summary>
            /// Radio Call of Facility That Furnishes Secondary Departure Control
            /// _Src: ATC_BASE.csv(SECONDARY_DEP_RADIO_CALL)
            /// _MaxLength: 26
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? SecondaryDepRadioCall { get; set; }

            /// <summary>
            /// Primary Departure Control Facility ID/Description
            /// _Src: ATC_BASE.csv(DEP_S_PROVIDER)
            /// _MaxLength: 700
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>Facility ID (or Provider Description when Provider Type equals ‘S’) of the Agency That Operates the Secondary Departure Control Facility/Functions</remarks>
            public string? DepSProvider { get; set; }

            /// <summary>
            /// Primary Departure Control Facility ID
            /// _Src: ATC_BASE.csv(DEP_S_PROV_TYPE_CD)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>A=Airport (with ATCT) _ C=ARTCC _ S=Special _ T=TRACON</remarks>
            public string? DepSProvTypeCd { get; set; }

            /// <summary>
            /// Approach Departure Call associated with a Control Facility
            /// _Src: ATC_BASE.csv(CTL_FAC_APCH_DEP_CALLS)
            /// _MaxLength: 54
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? CtlFacApchDepCalls { get; set; }

            /// <summary>
            /// Agency Type Code that Operates the Control Facility
            /// _Src: ATC_BASE.csv(APCH_DEP_OPER_CODE)
            /// _MaxLength: 1
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            /// <remarks>A=U.S. Air Force _ F=Federal Aviation Admin _ N=U.S. Navy _ R=U.S. Army</remarks>
            public string? ApchDepOperCode { get; set; }

            /// <summary>
            /// Hours of Operation of the Primary Control Facility
            /// _Src: ATC_BASE.csv(CTL_PRVDING_HRS)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? CtlPrvdingHrs { get; set; }

            /// <summary>
            /// Hours of Operation of the Secondary Control Facility
            /// _Src: ATC_BASE.csv(SECONDARY_CTL_PRVDING_HRS)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: Yes
            /// </summary>
            public string? SecondaryCtlPrvdingHrs { get; set; }

        }
        #endregion

        #region Atc_RMK Fields
        public class AtcRmk : CommonFields
        {
            /// <summary>
            /// Legacy Remark Element
            /// _Src: ATC_RMK.csv(LEGACY_ELEMENT_NUMBER)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>More of an ID than a Number; May contain other characters such as "TA21".</remarks>
            public string LegacyElementNumber { get; set; }

            /// <summary>
            /// NASR Table name associated with Remark
            /// _Src: ATC_RMK.csv(TAB_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            public string TabName { get; set; }

            /// <summary>
            /// NASR Column name associated with Remark
            /// _Src: ATC_RMK.csv(REF_COL_NAME)
            /// _MaxLength: 30
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>ARPT_CTL_REMARKs are identified as ATC_REMARK. All other Non-specific remarks are identified as GENERAL_REMARK</remarks>
            public string RefColName { get; set; }

            /// <summary>
            /// Sequence number assigned to Reference Column Remark
            /// _Src: ATC_RMK.csv(REMARK_NO)
            /// _MaxLength: (3,0)
            /// _DataType: int
            /// _Nullable: No
            /// </summary>
            public int RemarkNo { get; set; }

            /// <summary>
            /// Remark Text
            /// _Src: ATC_RMK.csv(REMARK)
            /// _MaxLength: 1500
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Free Form Text that further describes a specific Information Item.</remarks>
            public string Remark { get; set; }

        }
        #endregion

        #region Atc_SVC Fields
        public class AtcSvc : CommonFields
        {
            /// <summary>
            /// Services Provided to Satellite Airport
            /// _Src: ATC_SVC.csv(CTL_SVC)
            /// _MaxLength: 200
            /// _DataType: string
            /// _Nullable: No
            /// </summary>
            /// <remarks>Examples: "ARTS-IIIE" "CLASS B" "CONFLICT ALERT"</remarks>
            public string CtlSvc { get; set; }

        }
        #endregion

    }
}