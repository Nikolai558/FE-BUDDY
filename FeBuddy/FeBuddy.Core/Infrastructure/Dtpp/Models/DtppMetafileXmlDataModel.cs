namespace FeBuddy.Core.Infrastructure.Dtpp.Models;

/// <summary>
/// Row models for the FAA d-TPP Metafile (<c>d-tpp_Metafile.xml</c>), the Digital Terminal
/// Procedures Publication index: one row per <c>airport_name</c> element and one row per
/// <c>record</c> element.
/// </summary>
public class DtppMetafileXmlDataModel
{
	#region Common Fields
	/// <summary>
	/// The airport a row belongs to, with its city and state. Every <c>airport_name</c> and
	/// <c>record</c> row carries these.
	/// </summary>
	public class CommonFields
	{
		/// <summary>
		/// State Post Office Code
		/// _Src: d-tpp_Metafile.xml(state_code@ID)
		/// _MaxLength: 2
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// Standard two-letter US Postal Service abbreviation. The 2609 data has 54 codes,
		/// including "DC", "PR", "VI" and "XX" - the FAA's catch-all for "PACIFIC TERRITORIES",
		/// whose <see cref="CityName"/> values carry a country suffix (e.g. "TAMUNING,GU",
		/// "KOSRAE,FM").
		/// </remarks>
		public string StateCode { get; set; }

		/// <summary>
		/// State Full Name
		/// _Src: d-tpp_Metafile.xml(state_code@state_fullname)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string StateFullName { get; set; }

		/// <summary>
		/// City Name
		/// _Src: d-tpp_Metafile.xml(city_name@ID)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string CityName { get; set; }

		/// <summary>
		/// Bound TPP Volume
		/// _Src: d-tpp_Metafile.xml(city_name@volume)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// Which of the 26 bound TPP volumes the city's charts print in, e.g. "AK-1", "NC-3",
		/// "PC-1". During a change-notice cycle, charts with <see cref="Record.CnFlg"/> = "Y"
		/// print in the change-notice volume instead, and this attribute doesn't apply to them.
		/// </remarks>
		public string Volume { get; set; }

		/// <summary>
		/// Airport Name
		/// _Src: d-tpp_Metafile.xml(airport_name@ID)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		public string AirportName { get; set; }

		/// <summary>
		/// Military Airport Flag
		/// _Src: d-tpp_Metafile.xml(airport_name@military)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>"N" = non-military (2609 data: 3,045), "M" = military (2609 data: 152).</remarks>
		public string Military { get; set; }

		/// <summary>
		/// FAA Airport Identifier
		/// _Src: d-tpp_Metafile.xml(airport_name@apt_ident)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// 3-4 character alphanumeric identifier; never blank and unique in the 2609 data - the
		/// key other data joins on, e.g. NASR's <c>APT_BASE.ARPT_ID</c>.
		/// </remarks>
		public string AptIdent { get; set; }

		/// <summary>
		/// ICAO Airport Identifier
		/// _Src: d-tpp_Metafile.xml(airport_name@icao_ident)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>Four letters; blank for about 800 airports in the 2609 data.</remarks>
		public string? IcaoIdent { get; set; }

		/// <summary>
		/// Approach and Landing Number
		/// _Src: d-tpp_Metafile.xml(airport_name@alnum)
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>The airport's unique 1-5 digit Approach and Landing number.</remarks>
		public int Alnum { get; set; }
	}
	#endregion

	#region Airport Fields
	/// <summary>
	/// One <c>airport_name</c> element: one row per airport, the same way NASR's
	/// <c>APT_BASE.csv</c> is one row per airport. Carries no fields of its own beyond
	/// <see cref="CommonFields"/> - it's what code iterates to go airport by airport.
	/// </summary>
	public class Airport : CommonFields
	{
	}
	#endregion

	#region Record Fields
	/// <summary>
	/// One <c>record</c> element: one chart (a single PDF). A multi-page procedure has one extra
	/// record per continuation page, named e.g. "GRUUB ONE (RNAV), CONT.1".
	/// </summary>
	public class Record : CommonFields
	{
		/// <summary>
		/// Chart Sequence Number
		/// _Src: d-tpp_Metafile.xml(record/chartseq)
		/// _MaxLength: 5
		/// _DataType: int
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// A 5-digit number giving the chart's type and its ordering within the TPP. 2609 data
		/// (not exhaustive): MIN 10100 takeoff minimums, 10110 diverse vector area, 10200
		/// alternate minimums, 10400 radar minimums; LAH 10600; HOT 10700; STR (STARs) 30000; IAP
		/// 50750-59500 in order of precision (e.g. 50750 ILS/LOC, 53500 RNAV (RNP), 53525 RNAV
		/// (GPS), 55800 VOR, 56000 TACAN, 57500 NDB, 59000-59350 COPTER, 59390-59480 PRM, 59500
		/// charted visual); APD 70000; DAU 89000; ODP 90000; DP 90100, 90200 (copter DPs).
		/// </remarks>
		public int ChartSeq { get; set; }

		/// <summary>
		/// Chart Code
		/// _Src: d-tpp_Metafile.xml(record/chart_code)
		/// _MaxLength: 4
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// Up to four letters identifying the chart type. 2609 data counts: IAP 11,185
		/// (instrument approach procedure), MIN 5,385 (takeoff/alternate/radar minimums), STR
		/// 3,036 (STARs - the FAA's "Metafile XML Definitions" PDF calls this "STAR"), DP 3,030
		/// (departure procedure), APD 921 (airport diagram), ODP 287 (obstacle departure
		/// procedure), HOT 287 (hot spots), LAH 88 (LAHSO), DAU 12 (RNAV DP AAUP).
		/// </remarks>
		public string ChartCode { get; set; }

		/// <summary>
		/// Chart Name
		/// _Src: d-tpp_Metafile.xml(record/chart_name)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// The procedure or chart's printed name, e.g. "ILS OR LOC RWY 23", "RNAV (GPS)-A",
		/// "JALEX THREE (RNAV)", "TAKEOFF MINIMUMS", "AIRPORT DIAGRAM". A continuation page
		/// appends ", CONT.n", e.g. "GRUUB ONE (RNAV), CONT.1".
		/// </remarks>
		public string ChartName { get; set; }

		/// <summary>
		/// User Action
		/// _Src: d-tpp_Metafile.xml(record/useraction)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// "A" = added this cycle, "C" = changed, "D" = deleted, blank = unchanged (2609 data: C
		/// 3,012, A 178, D 147). A deleted record's <see cref="PdfName"/> points at
		/// "DELETED_JOB.PDF" (or "DEL_APT_SERVED.PDF").
		/// </remarks>
		public string? UserAction { get; set; }

		/// <summary>
		/// Chart PDF File Name
		/// _Src: d-tpp_Metafile.xml(record/pdf_name)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>E.g. "01244IYLY23.PDF", "AKTO.PDF".</remarks>
		public string PdfName { get; set; }

		/// <summary>
		/// Change Notice Flag
		/// _Src: d-tpp_Metafile.xml(record/cn_flg)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// "Y" = printed in the change-notice volume on a change-notice cycle, "N" = not (2609
		/// data: N 24,188, Y 43).
		/// </remarks>
		public string CnFlg { get; set; }

		/// <summary>
		/// Change Notice Section
		/// _Src: d-tpp_Metafile.xml(record/cnsection)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// Which change-notice volume section the chart prints in: "B" takeoff minimums, "C"
		/// alternate minimums, "D" radar minimums, "E" STARs; blank = normally numbered pages.
		/// Always blank in the 2609 data.
		/// </remarks>
		public string? CnSection { get; set; }

		/// <summary>
		/// Change Notice Page
		/// _Src: d-tpp_Metafile.xml(record/cnpage)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// Page number within <see cref="CnSection"/>; not applicable for sections B/C/D. Always
		/// blank in the 2609 data.
		/// </remarks>
		public int? CnPage { get; set; }

		/// <summary>
		/// Bound Volume Section
		/// _Src: d-tpp_Metafile.xml(record/bvsection)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// The FAA's "Metafile XML Definitions" PDF documents "C" takeoff, "E" alternates, "N"
		/// radar, "Z" STARs, blank = normally numbered pages - but the 2609 DATA instead uses "L"
		/// takeoff minimums and diverse vector area, "M" alternate minimums, "N" radar minimums,
		/// "O" LAHSO, "P" hot spots, "Z" STARs, blank for everything else. Documented here as the
		/// data behaves, since the PDF and the data disagree.
		/// </remarks>
		public string? BvSection { get; set; }

		/// <summary>
		/// Bound Volume Page
		/// _Src: d-tpp_Metafile.xml(record/bvpage)
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>1-3 digit page number within <see cref="BvSection"/>; blank when not applicable.</remarks>
		public int? BvPage { get; set; }

		/// <summary>
		/// Procedure Unique Identifier
		/// _Src: d-tpp_Metafile.xml(record/procuid)
		/// _MaxLength: 5
		/// _DataType: int
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// The FAA database's 1-5 digit identifier of the procedure; blank for MIN/HOT/LAH etc.
		/// Not unique per record - a procedure's continuation pages share the same
		/// <see cref="ProcUid"/>.
		/// </remarks>
		public int? ProcUid { get; set; }

		/// <summary>
		/// Two-Colored Flag
		/// _Src: d-tpp_Metafile.xml(record/two_colored)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>"Y" = the IAP chart prints with brown (terrain contours) and black plates, "N" = not.</remarks>
		public string TwoColored { get; set; }

		/// <summary>
		/// Civil/Military Production Code
		/// _Src: d-tpp_Metafile.xml(record/civil)
		/// _MaxLength: 1
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// "C" = civil (FAA-produced, civil airport), "D" = joint use (FAA-produced, joint-use
		/// airport), "N" = NGA-produced (printed in the TPP books and in the d-TPP), "H" = NGA
		/// high-altitude (d-TPP only, not printed in the books); blank for MIN/HOT/LAH etc.
		/// </remarks>
		public string? Civil { get; set; }

		/// <summary>
		/// SID/STAR Computer Code
		/// _Src: d-tpp_Metafile.xml(record/faanfd18)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>E.g. "JALEX3.JALEX", "BRODE.GRUUB1"; blank for everything that isn't a SID/STAR.</remarks>
		public string? Faanfd18 { get; set; }

		/// <summary>
		/// Copter Flag
		/// _Src: d-tpp_Metafile.xml(record/copter)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>No longer used by the FAA. 2609 data: "N" on STR/DP/ODP records, blank otherwise.</remarks>
		public string? Copter { get; set; }

		/// <summary>
		/// Amendment Number
		/// _Src: d-tpp_Metafile.xml(record/amdtnum)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>FAA IAPs only, e.g. "1A", "12", "0B".</remarks>
		public string? AmdtNum { get; set; }

		/// <summary>
		/// Amendment Date
		/// _Src: d-tpp_Metafile.xml(record/amdtdate)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// FAA IAPs only, the effective date the procedure was last amended, in "MM/DD/YYYY" text
		/// (e.g. "07/14/2022") - kept as the raw text rather than parsed, the way NASR keeps
		/// <c>EFF_DATE</c> as text. A handful of records have an <see cref="AmdtNum"/> but no
		/// date.
		/// </remarks>
		public string? AmdtDate { get; set; }
	}
	#endregion
}
