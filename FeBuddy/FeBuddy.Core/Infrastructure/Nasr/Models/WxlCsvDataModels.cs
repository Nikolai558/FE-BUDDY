namespace FeBuddy.Core.Infrastructure.Nasr.Models;

public class WxlCsvDataModel
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
		/// Weather Reporting Location Identifier
		/// _Src: All Wxl_*.csv files(WEA_ID)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string WeaId { get; set; }

		/// <summary>
		/// City
		/// _Src: All Wxl_*.csv files(CITY)
		/// _MaxLength: 40
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string City { get; set; }

		/// <summary>
		/// State Code
		/// _Src: All Wxl_*.csv files(STATE_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? StateCode { get; set; }

		/// <summary>
		/// Country Code
		/// _Src: All Wxl_*.csv files(COUNTRY_CODE)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CountryCode { get; set; }

	}
	#endregion

	#region Wxl_BASE Fields
	public class WxlBase : CommonFields
	{
		/// <summary>
		/// Weather Reporting Location Latitude Degrees
		/// _Src: WXL_BASE.csv(LAT_DEG)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatDeg { get; set; }

		/// <summary>
		/// Weather Reporting Location Latitude Minutes
		/// _Src: WXL_BASE.csv(LAT_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LatMin { get; set; }

		/// <summary>
		/// Weather Reporting Location Latitude Seconds
		/// _Src: WXL_BASE.csv(LAT_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatSec { get; set; }

		/// <summary>
		/// Weather Reporting Location Latitude Hemisphere
		/// _Src: WXL_BASE.csv(LAT_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LatHemis { get; set; }

		/// <summary>
		/// Weather Reporting Location Latitude in Decimal Format
		/// _Src: WXL_BASE.csv(LAT_DECIMAL)
		/// _MaxLength: (10,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LatDecimal { get; set; }

		/// <summary>
		/// Weather Reporting Location Longitude Degrees
		/// _Src: WXL_BASE.csv(LONG_DEG)
		/// _MaxLength: (3,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongDeg { get; set; }

		/// <summary>
		/// Weather Reporting Location Longitude Minutes
		/// _Src: WXL_BASE.csv(LONG_MIN)
		/// _MaxLength: (2,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		public int LongMin { get; set; }

		/// <summary>
		/// Weather Reporting Location Longitude Seconds
		/// _Src: WXL_BASE.csv(LONG_SEC)
		/// _MaxLength: (6,4)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongSec { get; set; }

		/// <summary>
		/// Weather Reporting Location Longitude Hemisphere
		/// _Src: WXL_BASE.csv(LONG_HEMIS)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string LongHemis { get; set; }

		/// <summary>
		/// Weather Reporting Location Longitude in Decimal Format
		/// _Src: WXL_BASE.csv(LONG_DECIMAL)
		/// _MaxLength: (11,8)
		/// _DataType: double
		/// _Nullable: No
		/// </summary>
		public double LongDecimal { get; set; }

		/// <summary>
		/// Weather Reporting Location Elevation
		/// _Src: WXL_BASE.csv(ELEV)
		/// _MaxLength: (5,0)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>Whole Feet MSL</remarks>
		public int Elev { get; set; }

		/// <summary>
		/// Survey Method Code
		/// _Src: WXL_BASE.csv(SURVEY_METHOD_CODE)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>S=Value was surveyed _ E=Value was estimated</remarks>
		public string SurveyMethodCode { get; set; }

	}
	#endregion

	#region Wxl_SVC Fields
	public class WxlSvc : CommonFields
	{
		/// <summary>
		/// Weather Service Type Code
		/// _Src: WXL_SVC.csv(WEA_SVC_TYPE_CODE)
		/// _MaxLength: 5
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>AC=Severe Weather Outlook Narrative _ AWW=Severe Weather Forecast Alert _ CWA=Central Weather Advisory _ FA=Area Forecast _ FD=Winds & Temperature Aloft Forecast _ FT=Aviation Terminal Forecast _ FX=Miscellaneous Forecasts _ METAR=Aviation Routine Weather Report (ICAO) _ MIS=Meteorological Impact Summary _ NOTAM=Notice to Airmen _ SA=Surface Observation Report _ SD=Radar Weather Report _ SPECI=Aviation Special Weather Report (ICAO) _ SYNS=Transcribed Weather Broadcast Synopses _ TAF=Aviation Terminal Forecast (ICAO) _ TWEB=Transcribed Weather Broadcast _ UA=Aircraft Report (PIREP) _ WA=Weather Advisory _ WH=Abbreviated Hurricane Advisory _ WO=Tropical Depressions _ WS=Flight Advisory - SIGMET _ WST=Convective SIGMET _ WW=Severe Weather Broadcasts or Bulletins</remarks>
		public string WeaSvcTypeCode { get; set; }

		/// <summary>
		/// Weather Affected Area
		/// _Src: WXL_SVC.csv(WEA_AFFECT_AREA)
		/// _MaxLength: 200
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Alphabetically ordered series of two-character U.S. state postal abbreviations separated by commas. May also include Great Lakes codes: LE=Lake Erie _ LH=Lake Huron _ LM=Lake Michigan _ LO=Lake Ontario _ LS=Lake Superior</remarks>
		public string? WeaAffectArea { get; set; }

	}
	#endregion

}