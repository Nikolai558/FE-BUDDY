using FEBuddyLibrary.Models.NASR.CSV;
using System;
using System.Collections.Generic;
using System.IO;
using static FEBuddyLibrary.Models.NASR.CSV.IlsCsvDataModel;

namespace FEBuddyLibrary.Parsers.NASR.CSV
{
    public class IlsCsvParser
    {
        public IlsCsvDataCollection ParseIlsBase(string filePath)
        {
            var result = new IlsCsvDataCollection();

            result.IlsBase = FebCsvHelper.ProcessLines(
                filePath,
                fields => new IlsBase
                {
                    EffDate = fields["EFF_DATE"],
                    SiteNo = fields["SITE_NO"],
                    SiteTypeCode = fields["SITE_TYPE_CODE"],
                    StateCode = fields["STATE_CODE"],
                    ArptId = fields["ARPT_ID"],
                    City = fields["CITY"],
                    CountryCode = fields["COUNTRY_CODE"],
                    RwyEndId = fields["RWY_END_ID"],
                    IlsLocId = fields["ILS_LOC_ID"],
                    SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
                    StateName = fields["STATE_NAME"],
                    RegionCode = fields["REGION_CODE"],
                    RwyLen = FebCsvHelper.ParseInt(fields["RWY_LEN"]),
                    RwyWidth = FebCsvHelper.ParseInt(fields["RWY_WIDTH"]),
                    Category = fields["CATEGORY"],
                    Owner = fields["OWNER"],
                    Operator = fields["OPERATOR"],
                    ApchBear = FebCsvHelper.ParseDouble(fields["APCH_BEAR"]),
                    MagVar = FebCsvHelper.ParseInt(fields["MAG_VAR"]),
                    MagVarHemis = fields["MAG_VAR_HEMIS"],
                    BaseComponentStatus = fields["COMPONENT_STATUS"],
                    BaseComponentStatusDate = fields["COMPONENT_STATUS_DATE"],
                    BaseLatDeg = FebCsvHelper.ParseInt(fields["LAT_DEG"]),
                    BaseLatMin = FebCsvHelper.ParseInt(fields["LAT_MIN"]),
                    BaseLatSec = FebCsvHelper.ParseDouble(fields["LAT_SEC"]),
                    BaseLatHemis = fields["LAT_HEMIS"],
                    BaseLatDecimal = FebCsvHelper.ParseDouble(fields["LAT_DECIMAL"]),
                    BaseLongDeg = FebCsvHelper.ParseInt(fields["LONG_DEG"]),
                    BaseLongMin = FebCsvHelper.ParseInt(fields["LONG_MIN"]),
                    BaseLongSec = FebCsvHelper.ParseDouble(fields["LONG_SEC"]),
                    BaseLongHemis = fields["LONG_HEMIS"],
                    BaseLongDecimal = FebCsvHelper.ParseDouble(fields["LONG_DECIMAL"]),
                    BaseLatLongSourceCode = fields["LAT_LONG_SOURCE_CODE"],
                    BaseSiteElevation = FebCsvHelper.ParseNullableDouble(fields["SITE_ELEVATION"]),
                    LocFreq = FebCsvHelper.ParseDouble(fields["LOC_FREQ"]),
                    BkCourseStatusCode = fields["BK_COURSE_STATUS_CODE"],
                });

            return result;
        }

        public IlsCsvDataCollection ParseIlsDme(string filePath)
        {
            var result = new IlsCsvDataCollection();

            result.IlsDme = FebCsvHelper.ProcessLines(
                filePath,
                fields => new IlsDme
                {
                    EffDate = fields["EFF_DATE"],
                    SiteNo = fields["SITE_NO"],
                    SiteTypeCode = fields["SITE_TYPE_CODE"],
                    StateCode = fields["STATE_CODE"],
                    ArptId = fields["ARPT_ID"],
                    City = fields["CITY"],
                    CountryCode = fields["COUNTRY_CODE"],
                    RwyEndId = fields["RWY_END_ID"],
                    IlsLocId = fields["ILS_LOC_ID"],
                    SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
                    DmeComponentStatus = fields["COMPONENT_STATUS"],
                    DmeComponentStatusDate = fields["COMPONENT_STATUS_DATE"],
                    DmeLatDeg = FebCsvHelper.ParseInt(fields["LAT_DEG"]),
                    DmeLatMin = FebCsvHelper.ParseInt(fields["LAT_MIN"]),
                    DmeLatSec = FebCsvHelper.ParseDouble(fields["LAT_SEC"]),
                    DmeLatHemis = fields["LAT_HEMIS"],
                    DmeLatDecimal = FebCsvHelper.ParseDouble(fields["LAT_DECIMAL"]),
                    DmeLongDeg = FebCsvHelper.ParseInt(fields["LONG_DEG"]),
                    DmeLongMin = FebCsvHelper.ParseInt(fields["LONG_MIN"]),
                    DmeLongSec = FebCsvHelper.ParseDouble(fields["LONG_SEC"]),
                    DmeLongHemis = fields["LONG_HEMIS"],
                    DmeLongDecimal = FebCsvHelper.ParseDouble(fields["LONG_DECIMAL"]),
                    DmeLatLongSourceCode = fields["LAT_LONG_SOURCE_CODE"],
                    DmeSiteElevation = FebCsvHelper.ParseNullableDouble(fields["SITE_ELEVATION"]),
                    Channel = fields["CHANNEL"],
                });

            return result;
        }

        public IlsCsvDataCollection ParseIlsGs(string filePath)
        {
            var result = new IlsCsvDataCollection();

            result.IlsGs = FebCsvHelper.ProcessLines(
                filePath,
                fields => new IlsGs
                {
                    EffDate = fields["EFF_DATE"],
                    SiteNo = fields["SITE_NO"],
                    SiteTypeCode = fields["SITE_TYPE_CODE"],
                    StateCode = fields["STATE_CODE"],
                    ArptId = fields["ARPT_ID"],
                    City = fields["CITY"],
                    CountryCode = fields["COUNTRY_CODE"],
                    RwyEndId = fields["RWY_END_ID"],
                    IlsLocId = fields["ILS_LOC_ID"],
                    SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
                    GsComponentStatus = fields["COMPONENT_STATUS"],
                    GsComponentStatusDate = fields["COMPONENT_STATUS_DATE"],
                    GsLatDeg = FebCsvHelper.ParseInt(fields["LAT_DEG"]),
                    GsLatMin = FebCsvHelper.ParseInt(fields["LAT_MIN"]),
                    GsLatSec = FebCsvHelper.ParseDouble(fields["LAT_SEC"]),
                    GsLatHemis = fields["LAT_HEMIS"],
                    GsLatDecimal = FebCsvHelper.ParseDouble(fields["LAT_DECIMAL"]),
                    GsLongDeg = FebCsvHelper.ParseInt(fields["LONG_DEG"]),
                    GsLongMin = FebCsvHelper.ParseInt(fields["LONG_MIN"]),
                    GsLongSec = FebCsvHelper.ParseDouble(fields["LONG_SEC"]),
                    GsLongHemis = fields["LONG_HEMIS"],
                    GsLongDecimal = FebCsvHelper.ParseDouble(fields["LONG_DECIMAL"]),
                    GsLatLongSourceCode = fields["LAT_LONG_SOURCE_CODE"],
                    GsSiteElevation = FebCsvHelper.ParseNullableDouble(fields["SITE_ELEVATION"]),
                    GSTypeCode = fields["G_S_TYPE_CODE"],
                    GSAngle = FebCsvHelper.ParseDouble(fields["G_S_ANGLE"]),
                    GSFreq = FebCsvHelper.ParseDouble(fields["G_S_FREQ"]),
                });

            return result;
        }

        public IlsCsvDataCollection ParseIlsMkr(string filePath)
        {
            var result = new IlsCsvDataCollection();

            result.IlsMkr = FebCsvHelper.ProcessLines(
                filePath,
                fields => new IlsMkr
                {
                    EffDate = fields["EFF_DATE"],
                    SiteNo = fields["SITE_NO"],
                    SiteTypeCode = fields["SITE_TYPE_CODE"],
                    StateCode = fields["STATE_CODE"],
                    ArptId = fields["ARPT_ID"],
                    City = fields["CITY"],
                    CountryCode = fields["COUNTRY_CODE"],
                    RwyEndId = fields["RWY_END_ID"],
                    IlsLocId = fields["ILS_LOC_ID"],
                    SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
                    MkrIlsCompTypeCode = fields["ILS_COMP_TYPE_CODE"],
                    MkrComponentStatus = fields["COMPONENT_STATUS"],
                    MkrComponentStatusDate = fields["COMPONENT_STATUS_DATE"],
                    MkrLatDeg = FebCsvHelper.ParseInt(fields["LAT_DEG"]),
                    MkrLatMin = FebCsvHelper.ParseInt(fields["LAT_MIN"]),
                    MkrLatSec = FebCsvHelper.ParseDouble(fields["LAT_SEC"]),
                    MkrLatHemis = fields["LAT_HEMIS"],
                    MkrLatDecimal = FebCsvHelper.ParseDouble(fields["LAT_DECIMAL"]),
                    MkrLongDeg = FebCsvHelper.ParseInt(fields["LONG_DEG"]),
                    MkrLongMin = FebCsvHelper.ParseInt(fields["LONG_MIN"]),
                    MkrLongSec = FebCsvHelper.ParseDouble(fields["LONG_SEC"]),
                    MkrLongHemis = fields["LONG_HEMIS"],
                    MkrLongDecimal = FebCsvHelper.ParseDouble(fields["LONG_DECIMAL"]),
                    MkrLatLongSourceCode = fields["LAT_LONG_SOURCE_CODE"],
                    MkrSiteElevation = FebCsvHelper.ParseNullableDouble(fields["SITE_ELEVATION"]),
                    MkrFacTypeCode = fields["MKR_FAC_TYPE_CODE"],
                    MarkerIdBeacon = fields["MARKER_ID_BEACON"],
                    CompassLocatorName = fields["COMPASS_LOCATOR_NAME"],
                    Freq = FebCsvHelper.ParseNullableDouble(fields["FREQ"]),
                    NavId = fields["NAV_ID"],
                    NavType = fields["NAV_TYPE"],
                    LowPoweredNdbStatus = fields["LOW_POWERED_NDB_STATUS"],
                });

            return result;
        }

        public IlsCsvDataCollection ParseIlsRmk(string filePath)
        {
            var result = new IlsCsvDataCollection();

            result.IlsRmk = FebCsvHelper.ProcessLines(
                filePath,
                fields => new IlsRmk
                {
                    EffDate = fields["EFF_DATE"],
                    SiteNo = fields["SITE_NO"],
                    SiteTypeCode = fields["SITE_TYPE_CODE"],
                    StateCode = fields["STATE_CODE"],
                    ArptId = fields["ARPT_ID"],
                    City = fields["CITY"],
                    CountryCode = fields["COUNTRY_CODE"],
                    RwyEndId = fields["RWY_END_ID"],
                    IlsLocId = fields["ILS_LOC_ID"],
                    SystemTypeCode = fields["SYSTEM_TYPE_CODE"],
                    TabName = fields["TAB_NAME"],
                    RmkIlsCompTypeCode = fields["ILS_COMP_TYPE_CODE"],
                    RefColName = fields["REF_COL_NAME"],
                    RefColSeqNo = FebCsvHelper.ParseInt(fields["REF_COL_SEQ_NO"]),
                    Remark = fields["REMARK"],
                });

            return result;
        }

    }

    public class IlsCsvDataCollection
    {
        public List<IlsBase> IlsBase { get; set; } = new();
        public List<IlsDme> IlsDme { get; set; } = new();
        public List<IlsGs> IlsGs { get; set; } = new();
        public List<IlsMkr> IlsMkr { get; set; } = new();
        public List<IlsRmk> IlsRmk { get; set; } = new();
    }
}