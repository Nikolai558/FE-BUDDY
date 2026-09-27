using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using FeBuddy.Core.Infrastructure.Dtpp.Models;

namespace FeBuddy.Core.Infrastructure.Dtpp.Parsers;

/// <summary>
/// Reads the FAA d-TPP Metafile (<c>d-tpp_Metafile.xml</c>), the Digital Terminal Procedures
/// Publication index.
/// </summary>
/// <remarks>
/// The file is a deeply nested, non-repeating structure - one <c>state_code</c> per US state, one
/// <c>city_name</c> per city, one <c>airport_name</c> per airport, one <c>record</c> per chart -
/// and the real file runs to about 16 MB across 3,197 airports and 24,231 records. Rather than
/// loading it whole (as <see cref="WxStations.Parsers.WxStationXmlParser"/> does for the much
/// smaller Wx Stations file), this streams it with <see cref="XmlReader"/>, reading the
/// root/state/city attributes as it descends and materializing one <c>airport_name</c> element
/// (and its <c>record</c> children) at a time with <see cref="XNode.ReadFrom(XmlReader)"/>. An
/// element it does not know at any level is skipped, so an element the FAA adds later does not
/// stop the rest of the file being read.
/// </remarks>
public static partial class DtppMetafileXmlParser
{
	private const string RootElement = "digital_tpp";
	private const string StateElement = "state_code";
	private const string CityElement = "city_name";
	private const string AirportElement = "airport_name";

	private static XmlReaderSettings Settings => new()
	{
		IgnoreComments = true,
		IgnoreWhitespace = true,
		DtdProcessing = DtdProcessing.Prohibit,
	};

	/// <summary>Reads <c>d-tpp_Metafile.xml</c>.</summary>
	/// <param name="xmlPath">The full path of the file.</param>
	/// <returns>Every airport and chart record the file lists, plus the cycle's own attributes.</returns>
	/// <exception cref="InvalidDataException">
	/// Thrown when the file's root is not <c>&lt;digital_tpp&gt;</c>, or an airport's
	/// <c>alnum</c> attribute or a record's <c>chartseq</c> element is blank or not a number.
	/// </exception>
	public static DtppMetafileDataCollection Parse(string xmlPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(xmlPath);

		using XmlReader reader = XmlReader.Create(xmlPath, Settings);
		return ReadDocument(reader, xmlPath);
	}

	/// <summary>Async wrapper over <see cref="Parse"/>, for callers already on an async pipeline (e.g. <c>AiracCycleDataCache</c>).</summary>
	/// <param name="xmlPath">The full path of the file.</param>
	/// <param name="cancellationToken">Checked before parsing starts.</param>
	/// <returns>The parsed data.</returns>
	/// <exception cref="InvalidDataException">See <see cref="Parse"/>.</exception>
	public static Task<DtppMetafileDataCollection> ParseAsync(string xmlPath, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.Run(() => Parse(xmlPath), cancellationToken);
	}

	private static DtppMetafileDataCollection ReadDocument(XmlReader reader, string xmlPath)
	{
		// MoveToContent lands on the root element (or throws), so its name is all there is to check.
		reader.MoveToContent();

		if (reader.LocalName != RootElement)
		{
			throw new InvalidDataException($"D-TPP metafile '{xmlPath}' does not have a <{RootElement}> root element.");
		}

		string fromEdate = TrimOrEmpty(reader.GetAttribute("from_edate"));
		string toEdate = TrimOrEmpty(reader.GetAttribute("to_edate"));

		var result = new DtppMetafileDataCollection
		{
			Cycle = TrimOrEmpty(reader.GetAttribute("cycle")),
			FromEdate = fromEdate,
			ToEdate = toEdate,
			FromEffectiveUtc = ParseEffectiveDate(fromEdate),
			ToEffectiveUtc = ParseEffectiveDate(toEdate),
		};

		bool isEmptyRoot = reader.IsEmptyElement;
		reader.ReadStartElement();

		if (!isEmptyRoot)
		{
			while (reader.NodeType == XmlNodeType.Element)
			{
				ReadOrSkip(reader, StateElement, () => ReadStateCode(reader, result));
			}
		}

		return result;
	}

	/// <summary>
	/// Reads the element the reader is on when it is named <paramref name="expectedElement"/>;
	/// skips it, children and all, when it is anything else.
	/// </summary>
	private static void ReadOrSkip(XmlReader reader, string expectedElement, Action read)
	{
		if (reader.LocalName == expectedElement)
		{
			read();
		}
		else
		{
			reader.Skip();
		}
	}

	private static void ReadStateCode(XmlReader reader, DtppMetafileDataCollection result)
	{
		string stateCode = TrimOrEmpty(reader.GetAttribute("ID"));
		string stateFullName = TrimOrEmpty(reader.GetAttribute("state_fullname"));

		bool isEmpty = reader.IsEmptyElement;
		reader.ReadStartElement();

		if (isEmpty)
		{
			return;
		}

		while (reader.NodeType == XmlNodeType.Element)
		{
			ReadOrSkip(reader, CityElement, () => ReadCityName(reader, result, stateCode, stateFullName));
		}

		reader.ReadEndElement();
	}

	private static void ReadCityName(XmlReader reader, DtppMetafileDataCollection result, string stateCode, string stateFullName)
	{
		string cityName = TrimOrEmpty(reader.GetAttribute("ID"));
		string volume = TrimOrEmpty(reader.GetAttribute("volume"));

		bool isEmpty = reader.IsEmptyElement;
		reader.ReadStartElement();

		if (isEmpty)
		{
			return;
		}

		while (reader.NodeType == XmlNodeType.Element)
		{
			ReadOrSkip(reader, AirportElement, () => ReadAirportName(reader, result, stateCode, stateFullName, cityName, volume));
		}

		reader.ReadEndElement();
	}

	private static void ReadAirportName(
		XmlReader reader,
		DtppMetafileDataCollection result,
		string stateCode,
		string stateFullName,
		string cityName,
		string volume)
	{
		// Materialize just this one airport (and its records) rather than the whole document.
		var airportElement = (XElement)XNode.ReadFrom(reader);

		string airportName = TrimOrEmpty((string?)airportElement.Attribute("ID"));
		string military = TrimOrEmpty((string?)airportElement.Attribute("military"));
		string aptIdent = TrimOrEmpty((string?)airportElement.Attribute("apt_ident"));
		string? icaoIdent = TrimOrNull((string?)airportElement.Attribute("icao_ident"));
		int alnum = ParseRequiredInt((string?)airportElement.Attribute("alnum"), "alnum", $"airport '{aptIdent}'");

		result.Airports.Add(new DtppMetafileXmlDataModel.Airport
		{
			StateCode = stateCode,
			StateFullName = stateFullName,
			CityName = cityName,
			Volume = volume,
			AirportName = airportName,
			Military = military,
			AptIdent = aptIdent,
			IcaoIdent = icaoIdent,
			Alnum = alnum,
		});

		foreach (XElement recordElement in airportElement.Elements("record"))
		{
			result.Records.Add(ReadRecord(
				recordElement, stateCode, stateFullName, cityName, volume, airportName, military, aptIdent, icaoIdent, alnum));
		}
	}

	private static DtppMetafileXmlDataModel.Record ReadRecord(
		XElement recordElement,
		string stateCode,
		string stateFullName,
		string cityName,
		string volume,
		string airportName,
		string military,
		string aptIdent,
		string? icaoIdent,
		int alnum)
	{
		string pdfName = TrimOrEmpty(ReadChildValue(recordElement, "pdf_name"));
		string chartName = TrimOrEmpty(ReadChildValue(recordElement, "chart_name"));
		string context = $"airport '{aptIdent}' record (pdf_name '{pdfName}', chart_name '{chartName}')";

		return new DtppMetafileXmlDataModel.Record
		{
			StateCode = stateCode,
			StateFullName = stateFullName,
			CityName = cityName,
			Volume = volume,
			AirportName = airportName,
			Military = military,
			AptIdent = aptIdent,
			IcaoIdent = icaoIdent,
			Alnum = alnum,
			ChartSeq = ParseRequiredInt(ReadChildValue(recordElement, "chartseq"), "chartseq", context),
			ChartCode = TrimOrEmpty(ReadChildValue(recordElement, "chart_code")),
			ChartName = chartName,
			UserAction = TrimOrNull(ReadChildValue(recordElement, "useraction")),
			PdfName = pdfName,
			CnFlg = TrimOrEmpty(ReadChildValue(recordElement, "cn_flg")),
			CnSection = TrimOrNull(ReadChildValue(recordElement, "cnsection")),
			CnPage = ParseNullableInt(ReadChildValue(recordElement, "cnpage")),
			BvSection = TrimOrNull(ReadChildValue(recordElement, "bvsection")),
			BvPage = ParseNullableInt(ReadChildValue(recordElement, "bvpage")),
			ProcUid = ParseNullableInt(ReadChildValue(recordElement, "procuid")),
			TwoColored = TrimOrEmpty(ReadChildValue(recordElement, "two_colored")),
			Civil = TrimOrNull(ReadChildValue(recordElement, "civil")),
			Faanfd18 = TrimOrNull(ReadChildValue(recordElement, "faanfd18")),
			Copter = TrimOrNull(ReadChildValue(recordElement, "copter")),
			AmdtNum = TrimOrNull(ReadChildValue(recordElement, "amdtnum")),
			AmdtDate = TrimOrNull(ReadChildValue(recordElement, "amdtdate")),
		};
	}

	/// <summary>A child element's raw (untrimmed) text, or <see langword="null"/> when the element is missing.</summary>
	private static string? ReadChildValue(XElement parent, string elementName) => parent.Element(elementName)?.Value;

	private static string TrimOrEmpty(string? value) => value?.Trim() ?? string.Empty;

	private static string? TrimOrNull(string? value)
	{
		string? trimmed = value?.Trim();
		return string.IsNullOrEmpty(trimmed) ? null : trimmed;
	}

	private static int? ParseNullableInt(string? value) =>
		int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : null;

	/// <summary>Parses a required integer field (<c>chartseq</c>, <c>alnum</c>).</summary>
	/// <param name="value">The field's raw text.</param>
	/// <param name="fieldName">The field's name, for the exception message.</param>
	/// <param name="context">Names the airport (and record, when applicable) the field belongs to.</param>
	/// <exception cref="InvalidDataException">Thrown when <paramref name="value"/> is blank or not an integer.</exception>
	private static int ParseRequiredInt(string? value, string fieldName, string context) =>
		int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
			? result
			: throw new InvalidDataException($"Invalid {fieldName} value '{value}' for {context}.");

	/// <summary>Parses a <c>HHMMZ  MM/DD/YY</c> effective-date attribute (any amount of whitespace after the <c>Z</c>).</summary>
	/// <returns>The UTC date/time, or <see langword="null"/> when it doesn't match or names an invalid date.</returns>
	private static DateTime? ParseEffectiveDate(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		Match match = EffectiveDatePattern().Match(value.Trim());
		if (!match.Success)
		{
			return null;
		}

		int hour = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
		int minute = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
		int month = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
		int day = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
		int year = 2000 + int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);

		try
		{
			return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
		}
		catch (ArgumentOutOfRangeException)
		{
			return null;
		}
	}

	/// <summary>HHMMZ, any amount of whitespace, then MM/DD/YY - e.g. <c>0901Z  09/03/26</c>.</summary>
	[GeneratedRegex(@"^(\d{2})(\d{2})Z\s+(\d{2})/(\d{2})/(\d{2})$")]
	private static partial Regex EffectiveDatePattern();
}
