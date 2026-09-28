namespace FeBuddy.Core.Infrastructure.Dtpp.Models;

/// <summary>
/// One cycle's parsed FAA d-TPP Metafile: the cycle's own attributes, plus every
/// <see cref="DtppMetafileXmlDataModel.Airport"/> and <see cref="DtppMetafileXmlDataModel.Record"/>
/// it lists, as <see cref="Parsers.DtppMetafileXmlParser.Parse"/> returns it.
/// </summary>
public class DtppMetafileDataCollection
{
	/// <summary>
	/// The chart production cycle id, format <c>YYCC</c> (two-digit year + cycle 01-13), e.g.
	/// <c>2609</c>. From the root <c>digital_tpp</c> element's <c>cycle</c> attribute.
	/// </summary>
	public string Cycle { get; set; } = string.Empty;

	/// <summary>
	/// The cycle's beginning effective date, exactly as the file states it:
	/// <c>HHMMZ  MM/DD/YY</c> (note the two spaces), e.g. <c>0901Z  09/03/26</c>. From
	/// <c>digital_tpp@from_edate</c>.
	/// </summary>
	public string FromEdate { get; set; } = string.Empty;

	/// <summary>
	/// The cycle's ending effective date, in the same raw format as <see cref="FromEdate"/>. From
	/// <c>digital_tpp@to_edate</c>.
	/// </summary>
	public string ToEdate { get; set; } = string.Empty;

	/// <summary>
	/// <see cref="FromEdate"/>, parsed to a UTC <see cref="DateTime"/>. <see langword="null"/>
	/// when the text doesn't match <c>HHMMZ</c> followed by any amount of whitespace and
	/// <c>MM/DD/YY</c>, or names an invalid date.
	/// </summary>
	public DateTime? FromEffectiveUtc { get; set; }

	/// <summary>
	/// <see cref="ToEdate"/>, parsed the same way as <see cref="FromEffectiveUtc"/>.
	/// </summary>
	public DateTime? ToEffectiveUtc { get; set; }

	/// <summary>Every airport the file lists, in file order.</summary>
	public List<DtppMetafileXmlDataModel.Airport> Airports { get; set; } = [];

	/// <summary>Every chart record the file lists, in file order.</summary>
	public List<DtppMetafileXmlDataModel.Record> Records { get; set; } = [];
}
