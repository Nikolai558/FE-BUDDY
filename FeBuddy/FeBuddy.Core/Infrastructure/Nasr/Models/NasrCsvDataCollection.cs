using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.Core.Infrastructure.Nasr.Models;

public class NasrCsvDataCollection
{
	public AptCsvDataCollection? Apt { get; set; }
	public AtcCsvDataCollection? Atc { get; set; }
	public AwyCsvDataCollection? Awy { get; set; }
	public ArbCsvDataCollection? Arb { get; set; }
	public AwosCsvDataCollection? Awos { get; set; }
	public ClsArspCsvDataCollection? ClsArsp { get; set; }
	public CdrCsvDataCollection? Cdr { get; set; }
	public ComCsvDataCollection? Com { get; set; }
	public DpCsvDataCollection? Dp { get; set; }
	public FixCsvDataCollection? Fix { get; set; }
	public FssCsvDataCollection? Fss { get; set; }
	public FrqCsvDataCollection? Frq { get; set; }
	public HpfCsvDataCollection? Hpf { get; set; }
	public IlsCsvDataCollection? Ils { get; set; }
	public LidCsvDataCollection? Lid { get; set; }
	public MilOpsCsvDataCollection? MilOps { get; set; }
	public MtrCsvDataCollection? Mtr { get; set; }
	public MaaCsvDataCollection? Maa { get; set; }
	public NavCsvDataCollection? Nav { get; set; }
	public PjaCsvDataCollection? Pja { get; set; }
	public PfrCsvDataCollection? Pfr { get; set; }
	public RdrCsvDataCollection? Rdr { get; set; }
	public StarCsvDataCollection? Star { get; set; }
	public WxlCsvDataCollection? Wxl { get; set; }
}