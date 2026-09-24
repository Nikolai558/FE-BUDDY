using System.Globalization;
using System.Reflection;

using CsvHelper;
using CsvHelper.Configuration;

using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Infrastructure.Nasr.Parsers;

/// <summary>
/// Runs every NASR CSV parser against <c>Fixtures/Nasr</c> - the real header row and first
/// three records of each file from cycle 2609 - and checks that each model property holds the
/// value of the column with the same name (<c>TacanDmeLatDeg</c> &lt;-&gt; <c>TACAN_DME_LAT_DEG</c>).
/// </summary>
/// <remarks>
/// The parsers are several hundred hand-written <c>Prop = fields["COLUMN"]</c> lines, so the
/// likely bug is a copy-paste one: a property reading its neighbour's column. Comparing every
/// property against its own column catches that without a hand-written assertion per field.
/// </remarks>
public sealed class NasrCsvParserMappingTests
{
	private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Nasr");

	/// <summary>Every NASR file FE-Buddy reads, with the parser call that reads it.</summary>
	private static readonly Dictionary<string, Func<string, IEnumerable<object>>> ParseCalls = new()
	{
		["APT_ARS.csv"] = p => new AptCsvParser().ParseAptArs(p).AptArs,
		["APT_ATT.csv"] = p => new AptCsvParser().ParseAptAtt(p).AptAtt,
		["APT_BASE.csv"] = p => new AptCsvParser().ParseAptBase(p).AptBase,
		["APT_CON.csv"] = p => new AptCsvParser().ParseAptCon(p).AptCon,
		["APT_RMK.csv"] = p => new AptCsvParser().ParseAptRmk(p).AptRmk,
		["APT_RWY.csv"] = p => new AptCsvParser().ParseAptRwy(p).AptRwy,
		["APT_RWY_END.csv"] = p => new AptCsvParser().ParseAptRwyEnd(p).AptRwyEnd,
		["ARB_BASE.csv"] = p => new ArbCsvParser().ParseArbBase(p).ArbBase,
		["ARB_SEG.csv"] = p => new ArbCsvParser().ParseArbSeg(p).ArbSeg,
		["ATC_ATIS.csv"] = p => new AtcCsvParser().ParseAtcAtis(p).AtcAtis,
		["ATC_BASE.csv"] = p => new AtcCsvParser().ParseAtcBase(p).AtcBase,
		["ATC_RMK.csv"] = p => new AtcCsvParser().ParseAtcRmk(p).AtcRmk,
		["ATC_SVC.csv"] = p => new AtcCsvParser().ParseAtcSvc(p).AtcSvc,
		["AWOS.csv"] = p => new AwosCsvParser().ParseAwos(p).Awos,
		["AWY_BASE.csv"] = p => new AwyCsvParser().ParseAwyBase(p).AwyBase,
		["AWY_SEG_ALT.csv"] = p => new AwyCsvParser().ParseAwySegAlt(p).AwySegAlt,
		["CDR.csv"] = p => new CdrCsvParser().ParseCdr(p).Cdr,
		["CLS_ARSP.csv"] = p => new ClsArspCsvParser().ParseClsArsp(p).ClsArsp,
		["COM.csv"] = p => new ComCsvParser().ParseCom(p).Com,
		["DP_APT.csv"] = p => new DpCsvParser().ParseDpApt(p).DpApt,
		["DP_BASE.csv"] = p => new DpCsvParser().ParseDpBase(p).DpBase,
		["DP_RTE.csv"] = p => new DpCsvParser().ParseDpRte(p).DpRte,
		["FIX_BASE.csv"] = p => new FixCsvParser().ParseFixBase(p).FixBase,
		["FIX_CHRT.csv"] = p => new FixCsvParser().ParseFixChrt(p).FixChrt,
		["FIX_NAV.csv"] = p => new FixCsvParser().ParseFixNav(p).FixNav,
		["FRQ.csv"] = p => new FrqCsvParser().ParseFrq(p).Frq,
		["FSS_BASE.csv"] = p => new FssCsvParser().ParseFssBase(p).FssBase,
		["FSS_RMK.csv"] = p => new FssCsvParser().ParseFssRmk(p).FssRmk,
		["HPF_BASE.csv"] = p => new HpfCsvParser().ParseHpfBase(p).HpfBase,
		["HPF_CHRT.csv"] = p => new HpfCsvParser().ParseHpfChrt(p).HpfChrt,
		["HPF_RMK.csv"] = p => new HpfCsvParser().ParseHpfRmk(p).HpfRmk,
		["HPF_SPD_ALT.csv"] = p => new HpfCsvParser().ParseHpfSpdAlt(p).HpfSpdAlt,
		["ILS_BASE.csv"] = p => new IlsCsvParser().ParseIlsBase(p).IlsBase,
		["ILS_DME.csv"] = p => new IlsCsvParser().ParseIlsDme(p).IlsDme,
		["ILS_GS.csv"] = p => new IlsCsvParser().ParseIlsGs(p).IlsGs,
		["ILS_MKR.csv"] = p => new IlsCsvParser().ParseIlsMkr(p).IlsMkr,
		["ILS_RMK.csv"] = p => new IlsCsvParser().ParseIlsRmk(p).IlsRmk,
		["LID.csv"] = p => new LidCsvParser().ParseLid(p).Lid,
		["MAA_BASE.csv"] = p => new MaaCsvParser().ParseMaaBase(p).MaaBase,
		["MAA_CON.csv"] = p => new MaaCsvParser().ParseMaaCon(p).MaaCon,
		["MAA_RMK.csv"] = p => new MaaCsvParser().ParseMaaRmk(p).MaaRmk,
		["MAA_SHP.csv"] = p => new MaaCsvParser().ParseMaaShp(p).MaaShp,
		["MIL_OPS.csv"] = p => new MilOpsCsvParser().ParseMilOps(p).MilOps,
		["MTR_AGY.csv"] = p => new MtrCsvParser().ParseMtrAgy(p).MtrAgy,
		["MTR_BASE.csv"] = p => new MtrCsvParser().ParseMtrBase(p).MtrBase,
		["MTR_PT.csv"] = p => new MtrCsvParser().ParseMtrPt(p).MtrPt,
		["MTR_SOP.csv"] = p => new MtrCsvParser().ParseMtrSop(p).MtrSop,
		["MTR_TERR.csv"] = p => new MtrCsvParser().ParseMtrTerr(p).MtrTerr,
		["MTR_WDTH.csv"] = p => new MtrCsvParser().ParseMtrWdth(p).MtrWdth,
		["NAV_BASE.csv"] = p => new NavCsvParser().ParseNavBase(p).NavBase,
		["NAV_CKPT.csv"] = p => new NavCsvParser().ParseNavCkpt(p).NavCkpt,
		["NAV_RMK.csv"] = p => new NavCsvParser().ParseNavRmk(p).NavRmk,
		["PFR_BASE.csv"] = p => new PfrCsvParser().ParsePfrBase(p).PfrBase,
		["PFR_RMT_FMT.csv"] = p => new PfrCsvParser().ParsePfrRmtFmt(p).PfrRmtFmt,
		["PFR_SEG.csv"] = p => new PfrCsvParser().ParsePfrSeg(p).PfrSeg,
		["PJA_BASE.csv"] = p => new PjaCsvParser().ParsePjaBase(p).PjaBase,
		["PJA_CON.csv"] = p => new PjaCsvParser().ParsePjaCon(p).PjaCon,
		["RDR.csv"] = p => new RdrCsvParser().ParseRdr(p).Rdr,
		["STAR_APT.csv"] = p => new StarCsvParser().ParseStarApt(p).StarApt,
		["STAR_BASE.csv"] = p => new StarCsvParser().ParseStarBase(p).StarBase,
		["STAR_RTE.csv"] = p => new StarCsvParser().ParseStarRte(p).StarRte,
		["WXL_BASE.csv"] = p => new WxlCsvParser().ParseWxlBase(p).WxlBase,
		["WXL_SVC.csv"] = p => new WxlCsvParser().ParseWxlSvc(p).WxlSvc,
	};

	/// <summary>Model properties the FAA file has no column for; the parser leaves them at their default.</summary>
	private static readonly HashSet<string> PropertiesWithNoColumn = new()
	{
		// PfrRmtFmt inherits the PFR common fields, but PFR_RMT_FMT.csv carries its own
		// Orig/Dest/Type columns instead of any of them.
		"PfrRmtFmt.EffDate",
		"PfrRmtFmt.OriginId",
		"PfrRmtFmt.DstnId",
		"PfrRmtFmt.PfrTypeCode",
		"PfrRmtFmt.RouteNo",
	};

	/// <summary>The file names, for <see cref="MemberDataAttribute"/>.</summary>
	public static TheoryData<string> Files => new(ParseCalls.Keys);

	/// <summary>Each parser reads every record, and every property holds its own column's value.</summary>
	[Theory]
	[MemberData(nameof(Files))]
	public void Parser_MapsEveryPropertyFromItsOwnColumn(string fileName)
	{
		string path = Path.Combine(FixtureDirectory, fileName);
		(string[] header, List<Dictionary<string, string>> rows) = ReadFixture(path);

		List<object> records = ParseCalls[fileName](path).ToList();

		Assert.Equal(rows.Count, records.Count);

		List<string> mismatches = new();
		for (int i = 0; i < records.Count; i++)
		{
			foreach (PropertyInfo property in records[i].GetType().GetProperties())
			{
				string? column = FindColumn(property.Name, header);
				if (column is null && PropertiesWithNoColumn.Contains($"{records[i].GetType().Name}.{property.Name}"))
				{
					object? unset = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
					Assert.Equal(unset, property.GetValue(records[i]));
					continue;
				}

				Assert.True(column is not null, $"{fileName}: property {property.Name} has no matching column.");

				object? expected = Convert(rows[i][column!], property.PropertyType);
				object? actual = property.GetValue(records[i]);

				if (!Equals(expected, actual))
				{
					mismatches.Add($"row {i + 1} {property.Name}: expected '{expected}' from {column}, got '{actual}'");
				}
			}
		}

		Assert.True(mismatches.Count == 0, $"{fileName}:{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}");
	}

	/// <summary>Every column in the file is read into some property - nothing FAA publishes is silently dropped.</summary>
	[Theory]
	[MemberData(nameof(Files))]
	public void Parser_ReadsEveryColumnInTheFile(string fileName)
	{
		string path = Path.Combine(FixtureDirectory, fileName);
		(string[] header, _) = ReadFixture(path);

		object record = ParseCalls[fileName](path).First();
		HashSet<string?> mapped = record.GetType().GetProperties().Select(p => FindColumn(p.Name, header)).ToHashSet();

		Assert.DoesNotContain(header, column => !mapped.Contains(column));
	}

	/// <summary>
	/// <see cref="NasrCsvParser.ParseAllAsync"/> parses every group from one directory and
	/// fills every list of the combined collection.
	/// </summary>
	[Fact]
	public async Task MainAsync_ParsesEveryFileIntoTheCombinedCollection()
	{
		NasrCsvDataCollection all = await NasrCsvParser.ParseAllAsync(FixtureDirectory);

		int lists = 0;
		foreach (PropertyInfo groupProperty in typeof(NasrCsvDataCollection).GetProperties())
		{
			object? group = groupProperty.GetValue(all);
			Assert.True(group is not null, $"{groupProperty.Name} was not parsed.");

			foreach (PropertyInfo listProperty in group!.GetType().GetProperties())
			{
				var list = (System.Collections.ICollection)listProperty.GetValue(group)!;
				Assert.True(list.Count == 3, $"{groupProperty.Name}.{listProperty.Name} has {list.Count} records, expected 3.");
				lists++;
			}
		}

		Assert.Equal(ParseCalls.Count, lists);
		Assert.Equal("0J0", all.Apt!.AptBase[0].ArptId);
	}

	/// <summary>A cycle folder missing a file fails the whole parse rather than returning partial data.</summary>
	[Fact]
	public async Task MainAsync_MissingDirectory_Throws()
	{
		string missing = Path.Combine(Path.GetTempPath(), "FeBuddyTests_NoSuchCycle_" + Guid.NewGuid().ToString("N"));

		await Assert.ThrowsAnyAsync<IOException>(() => NasrCsvParser.ParseAllAsync(missing));
	}

	/// <summary>
	/// The column a property reads: the one with the same name ignoring case and underscores
	/// (<c>TacanDmeLatDeg</c> -&gt; <c>TACAN_DME_LAT_DEG</c>, <c>GSAngle</c> -&gt; <c>G_S_ANGLE</c>),
	/// or else the longest column the name ends with, since some models prefix the table
	/// (<c>ConPhoneNo</c> -&gt; <c>PHONE_NO</c>).
	/// </summary>
	private static string? FindColumn(string propertyName, string[] header)
	{
		string property = propertyName.ToUpperInvariant();

		return header.FirstOrDefault(c => Normalize(c) == property)
			?? header.Where(c => property.EndsWith(Normalize(c), StringComparison.Ordinal))
				.OrderByDescending(c => c.Length)
				.FirstOrDefault();

		// CDR and PFR_RMT_FMT use mixed-case, spaced headers ("Route String").
		static string Normalize(string column) =>
			column.Replace("_", string.Empty, StringComparison.Ordinal)
				.Replace(" ", string.Empty, StringComparison.Ordinal)
				.ToUpperInvariant();
	}

	/// <summary>What the parser should produce for a raw CSV value, mirroring <see cref="NasrCsvReader"/>.</summary>
	private static object? Convert(string raw, Type type)
	{
		if (type == typeof(string))
		{
			return raw;
		}

		if (type == typeof(int))
		{
			return int.Parse(raw);
		}

		if (type == typeof(int?))
		{
			return int.TryParse(raw, out int i) ? i : null;
		}

		if (type == typeof(double))
		{
			return double.Parse(raw);
		}

		if (type == typeof(double?))
		{
			return double.TryParse(raw, out double d) ? d : null;
		}

		throw new NotSupportedException($"No conversion for {type}.");
	}

	private static (string[] Header, List<Dictionary<string, string>> Rows) ReadFixture(string path)
	{
		using StreamReader reader = new(path);
		using CsvReader csv = new(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { TrimOptions = TrimOptions.Trim });

		csv.Read();
		csv.ReadHeader();
		string[] header = csv.HeaderRecord!;

		List<Dictionary<string, string>> rows = new();
		while (csv.Read())
		{
			rows.Add(header.ToDictionary(h => h, h => csv.GetField(h) ?? string.Empty));
		}

		return (header, rows);
	}
}
