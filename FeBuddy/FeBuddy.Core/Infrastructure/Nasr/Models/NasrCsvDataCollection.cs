using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// One cycle's parsed NASR subscription, a property per CSV group, as
/// <see cref="Parsers.NasrCsvParser.ParseAsync"/> returns it. A group that wasn't parsed is
/// <see langword="null"/>: the AIRAC cycle cache parses only <see cref="NasrCsvGroups.InUse"/>.
/// </summary>
/// <remarks>
/// Read a group through its <c>Require…</c> method, such as <see cref="RequireApt"/>: a group that
/// wasn't parsed then fails with an error that names it (<see cref="NasrCsvGroups.NotParsed"/>),
/// not a <see cref="NullReferenceException"/> or a silently empty list.
/// </remarks>
public class NasrCsvDataCollection
{
	/// <summary>The <c>APT</c> files (airports), or <see langword="null"/> when not parsed.</summary>
	public AptCsvDataCollection? Apt { get; set; }
	/// <summary>The <c>ATC</c> files (air traffic control facilities), or <see langword="null"/> when not parsed.</summary>
	public AtcCsvDataCollection? Atc { get; set; }
	/// <summary>The <c>AWY</c> files (airways), or <see langword="null"/> when not parsed.</summary>
	public AwyCsvDataCollection? Awy { get; set; }
	/// <summary>The <c>ARB</c> files (ARTCC boundaries), or <see langword="null"/> when not parsed.</summary>
	public ArbCsvDataCollection? Arb { get; set; }
	/// <summary>The <c>AWOS</c> files (automated weather observing systems), or <see langword="null"/> when not parsed.</summary>
	public AwosCsvDataCollection? Awos { get; set; }
	/// <summary>The <c>CLS_ARSP</c> files (class airspace at airports), or <see langword="null"/> when not parsed.</summary>
	public ClsArspCsvDataCollection? ClsArsp { get; set; }
	/// <summary>The <c>CDR</c> files (coded departure routes), or <see langword="null"/> when not parsed.</summary>
	public CdrCsvDataCollection? Cdr { get; set; }
	/// <summary>The <c>COM</c> files (flight service station communication facilities), or <see langword="null"/> when not parsed.</summary>
	public ComCsvDataCollection? Com { get; set; }
	/// <summary>The <c>DP</c> files (departure procedures), or <see langword="null"/> when not parsed.</summary>
	public DpCsvDataCollection? Dp { get; set; }
	/// <summary>The <c>FIX</c> files (fixes and reporting points), or <see langword="null"/> when not parsed.</summary>
	public FixCsvDataCollection? Fix { get; set; }
	/// <summary>The <c>FSS</c> files (flight service stations), or <see langword="null"/> when not parsed.</summary>
	public FssCsvDataCollection? Fss { get; set; }
	/// <summary>The <c>FRQ</c> files (frequencies), or <see langword="null"/> when not parsed.</summary>
	public FrqCsvDataCollection? Frq { get; set; }
	/// <summary>The <c>HPF</c> files (holding patterns), or <see langword="null"/> when not parsed.</summary>
	public HpfCsvDataCollection? Hpf { get; set; }
	/// <summary>The <c>ILS</c> files (instrument landing systems), or <see langword="null"/> when not parsed.</summary>
	public IlsCsvDataCollection? Ils { get; set; }
	/// <summary>The <c>LID</c> files (location identifiers), or <see langword="null"/> when not parsed.</summary>
	public LidCsvDataCollection? Lid { get; set; }
	/// <summary>The <c>MIL_OPS</c> files (military operations at airports), or <see langword="null"/> when not parsed.</summary>
	public MilOpsCsvDataCollection? MilOps { get; set; }
	/// <summary>The <c>MTR</c> files (military training routes), or <see langword="null"/> when not parsed.</summary>
	public MtrCsvDataCollection? Mtr { get; set; }
	/// <summary>The <c>MAA</c> files (miscellaneous activity areas), or <see langword="null"/> when not parsed.</summary>
	public MaaCsvDataCollection? Maa { get; set; }
	/// <summary>The <c>NAV</c> files (navaids), or <see langword="null"/> when not parsed.</summary>
	public NavCsvDataCollection? Nav { get; set; }
	/// <summary>The <c>PJA</c> files (parachute jump areas), or <see langword="null"/> when not parsed.</summary>
	public PjaCsvDataCollection? Pja { get; set; }
	/// <summary>The <c>PFR</c> files (preferred routes), or <see langword="null"/> when not parsed.</summary>
	public PfrCsvDataCollection? Pfr { get; set; }
	/// <summary>The <c>RDR</c> files (radar facilities), or <see langword="null"/> when not parsed.</summary>
	public RdrCsvDataCollection? Rdr { get; set; }
	/// <summary>The <c>STAR</c> files (standard terminal arrivals), or <see langword="null"/> when not parsed.</summary>
	public StarCsvDataCollection? Star { get; set; }
	/// <summary>The <c>WXL</c> files (weather reporting locations), or <see langword="null"/> when not parsed.</summary>
	public WxlCsvDataCollection? Wxl { get; set; }

	/// <summary>The groups that were parsed: those that aren't <see langword="null"/>.</summary>
	/// <returns>The groups, in <see cref="NasrCsvGroup"/> order.</returns>
	public IReadOnlyList<NasrCsvGroup> ParsedGroups() => [.. NasrCsvGroups.All.Where(group => Group(group) is not null)];

	/// <summary>The <c>APT</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public AptCsvDataCollection RequireApt() => Apt ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Apt);
	/// <summary>The <c>ATC</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public AtcCsvDataCollection RequireAtc() => Atc ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Atc);
	/// <summary>The <c>AWY</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public AwyCsvDataCollection RequireAwy() => Awy ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Awy);
	/// <summary>The <c>ARB</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public ArbCsvDataCollection RequireArb() => Arb ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Arb);
	/// <summary>The <c>AWOS</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public AwosCsvDataCollection RequireAwos() => Awos ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Awos);
	/// <summary>The <c>CLS_ARSP</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public ClsArspCsvDataCollection RequireClsArsp() => ClsArsp ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.ClsArsp);
	/// <summary>The <c>CDR</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public CdrCsvDataCollection RequireCdr() => Cdr ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Cdr);
	/// <summary>The <c>COM</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public ComCsvDataCollection RequireCom() => Com ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Com);
	/// <summary>The <c>DP</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public DpCsvDataCollection RequireDp() => Dp ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Dp);
	/// <summary>The <c>FIX</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public FixCsvDataCollection RequireFix() => Fix ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Fix);
	/// <summary>The <c>FSS</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public FssCsvDataCollection RequireFss() => Fss ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Fss);
	/// <summary>The <c>FRQ</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public FrqCsvDataCollection RequireFrq() => Frq ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Frq);
	/// <summary>The <c>HPF</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public HpfCsvDataCollection RequireHpf() => Hpf ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Hpf);
	/// <summary>The <c>ILS</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public IlsCsvDataCollection RequireIls() => Ils ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Ils);
	/// <summary>The <c>LID</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public LidCsvDataCollection RequireLid() => Lid ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Lid);
	/// <summary>The <c>MIL_OPS</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public MilOpsCsvDataCollection RequireMilOps() => MilOps ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.MilOps);
	/// <summary>The <c>MTR</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public MtrCsvDataCollection RequireMtr() => Mtr ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Mtr);
	/// <summary>The <c>MAA</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public MaaCsvDataCollection RequireMaa() => Maa ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Maa);
	/// <summary>The <c>NAV</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public NavCsvDataCollection RequireNav() => Nav ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Nav);
	/// <summary>The <c>PJA</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public PjaCsvDataCollection RequirePja() => Pja ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Pja);
	/// <summary>The <c>PFR</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public PfrCsvDataCollection RequirePfr() => Pfr ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Pfr);
	/// <summary>The <c>RDR</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public RdrCsvDataCollection RequireRdr() => Rdr ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Rdr);
	/// <summary>The <c>STAR</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public StarCsvDataCollection RequireStar() => Star ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Star);
	/// <summary>The <c>WXL</c> files.</summary>
	/// <returns>The group.</returns>
	/// <exception cref="InvalidOperationException">It wasn't parsed.</exception>
	public WxlCsvDataCollection RequireWxl() => Wxl ?? throw NasrCsvGroups.NotParsed(NasrCsvGroup.Wxl);

	/// <summary>A group's data, whatever its type, or <see langword="null"/> when it wasn't parsed.</summary>
	/// <param name="group">The group.</param>
	/// <returns>The group's data.</returns>
	internal object? Group(NasrCsvGroup group) => group switch
	{
		NasrCsvGroup.Apt => Apt,
		NasrCsvGroup.Arb => Arb,
		NasrCsvGroup.Atc => Atc,
		NasrCsvGroup.Awos => Awos,
		NasrCsvGroup.Awy => Awy,
		NasrCsvGroup.Cdr => Cdr,
		NasrCsvGroup.ClsArsp => ClsArsp,
		NasrCsvGroup.Com => Com,
		NasrCsvGroup.Dp => Dp,
		NasrCsvGroup.Fix => Fix,
		NasrCsvGroup.Frq => Frq,
		NasrCsvGroup.Fss => Fss,
		NasrCsvGroup.Hpf => Hpf,
		NasrCsvGroup.Ils => Ils,
		NasrCsvGroup.Lid => Lid,
		NasrCsvGroup.Maa => Maa,
		NasrCsvGroup.MilOps => MilOps,
		NasrCsvGroup.Mtr => Mtr,
		NasrCsvGroup.Nav => Nav,
		NasrCsvGroup.Pfr => Pfr,
		NasrCsvGroup.Pja => Pja,
		NasrCsvGroup.Rdr => Rdr,
		NasrCsvGroup.Star => Star,
		NasrCsvGroup.Wxl => Wxl,
		_ => throw new ArgumentOutOfRangeException(nameof(group), group, "Not a NASR CSV group."),
	};
}
