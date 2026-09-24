using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// One cycle's parsed NASR subscription: every CSV group, as <see cref="Parsers.NasrCsvParser.ParseAllAsync"/> returns it.
/// </summary>
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
}