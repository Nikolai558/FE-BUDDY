namespace FeBuddy.Core.Infrastructure.Telephony.Models;

/// <summary>
/// Row models for the two FAA telephony pages of Order JO 7340.2, Chapter 3: one row per table row
/// of Section 1 (the ICAO register) and of Section 4 (U.S. special call signs).
/// </summary>
/// <remarks>
/// Every value is the cell's text as published: tags stripped, HTML entities decoded, soft hyphens
/// removed and runs of whitespace collapsed to one space. Nothing is interpreted here - a missing
/// designator stays whatever placeholder the FAA printed (see
/// <see cref="Assignment.ThreeLetterDesignator"/>).
/// </remarks>
public class TelephonyHtmlDataModel
{
	#region Assignment Fields
	/// <summary>
	/// One row of Chapter 3, Section 1, "Aircraft Company/Telephony/Three-Letter Designator
	/// Encode": one company's ICAO assignment. The page is 27 tables, one per leading character of
	/// the company name.
	/// </summary>
	public class Assignment
	{
		/// <summary>
		/// Aircraft Company
		/// _Src: chap3_section_1.html(table/tbody/tr/td, "Company" column)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// The operator's name, often with its U.S. base in brackets, e.g.
		/// "ABX AIR, INC. (WILMINGTON, OH)". Upper case.
		/// </remarks>
		public string Company { get; set; } = string.Empty;

		/// <summary>
		/// Country
		/// _Src: chap3_section_1.html(table/tbody/tr/td, "Country" column)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The state the assignment belongs to, e.g. "UNITED STATES", "IRAN (ISLAMIC REPUBLIC OF)".</remarks>
		public string Country { get; set; } = string.Empty;

		/// <summary>
		/// Telephony Designator
		/// _Src: chap3_section_1.html(table/tbody/tr/td, "Telephony" column)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// The spoken call sign, e.g. "AVIANCA", "RYAN AIR", "COTE D'IVOIRE". Letters, digits,
		/// spaces, hyphens and (once) an apostrophe. Blank for 540 of the 6,343 rows in the 10 Jul
		/// 2026 edition: a company with a designator but no telephony.
		/// </remarks>
		public string Telephony { get; set; } = string.Empty;

		/// <summary>
		/// ICAO Three-Letter Designator (3LD)
		/// _Src: chap3_section_1.html(table/tbody/tr/td, "3-Ltr" column)
		/// _MaxLength: 3
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// Three capital letters, e.g. "AVA". The FAA prints a placeholder instead when a company has
		/// telephony but no designator: "..." (99 rows in the 10 Jul 2026 edition) or "--" (1 row).
		/// </remarks>
		public string ThreeLetterDesignator { get; set; } = string.Empty;
	}
	#endregion

	#region Special Call Sign Fields
	/// <summary>
	/// One row of Chapter 3, Section 4, "U.S. Special Telephony/Call Signs": a call sign the FAA
	/// assigns in the U.S. outside the ICAO register, with its own identifier. One table.
	/// </summary>
	public class SpecialCallSign
	{
		/// <summary>
		/// Telephony/Call Sign
		/// _Src: chap3_section_4.html(table/tbody/tr/td, "Telephony/Call Sign" column)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>The spoken call sign, e.g. "AIR SIX", "BATTLEBORN".</remarks>
		public string Telephony { get; set; } = string.Empty;

		/// <summary>
		/// Identifier
		/// _Src: chap3_section_4.html(table/tbody/tr/td, "Identifier" column)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// What is filed and shown in the data block, e.g. "ARSIX", "BTLBN"; four or five letters,
		/// sometimes the call sign itself ("NASA", "FEMA").
		/// </remarks>
		public string Identifier { get; set; } = string.Empty;

		/// <summary>
		/// Company or Operating Agency
		/// _Src: chap3_section_4.html(table/tbody/tr/td, "Company or Operating Agency" column)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>Mixed case, e.g. "NYC Environmental Protection (New Windsor, NY)".</remarks>
		public string Agency { get; set; } = string.Empty;

		/// <summary>
		/// Expiration Date
		/// _Src: chap3_section_4.html(table/tbody/tr/td, "Expiration Date" column)
		/// _DataType: string
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// The raw text, e.g. "24-Feb-2027" (day-month-year, the month abbreviated), or "N/A" for a
		/// call sign that does not expire.
		/// </remarks>
		public string ExpirationDate { get; set; } = string.Empty;
	}
	#endregion
}
