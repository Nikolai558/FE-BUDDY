namespace FeBuddy.Core.Infrastructure.Nasr.Models;

/// <summary>
/// One group of files in a NASR CSV cycle: the files whose names start with its code, such as
/// <c>APT_BASE.csv</c> and <c>APT_RWY.csv</c> for <see cref="Apt"/>. Each is one property of
/// <see cref="NasrCsvDataCollection"/>.
/// </summary>
public enum NasrCsvGroup
{
	/// <summary><c>APT</c>: airports.</summary>
	Apt,

	/// <summary><c>ARB</c>: ARTCC boundaries.</summary>
	Arb,

	/// <summary><c>ATC</c>: air traffic control facilities.</summary>
	Atc,

	/// <summary><c>AWOS</c>: automated weather observing systems.</summary>
	Awos,

	/// <summary><c>AWY</c>: airways.</summary>
	Awy,

	/// <summary><c>CDR</c>: coded departure routes.</summary>
	Cdr,

	/// <summary><c>CLS_ARSP</c>: class airspace at airports.</summary>
	ClsArsp,

	/// <summary><c>COM</c>: flight service station communication facilities.</summary>
	Com,

	/// <summary><c>DP</c>: departure procedures.</summary>
	Dp,

	/// <summary><c>FIX</c>: fixes and reporting points.</summary>
	Fix,

	/// <summary><c>FRQ</c>: frequencies.</summary>
	Frq,

	/// <summary><c>FSS</c>: flight service stations.</summary>
	Fss,

	/// <summary><c>HPF</c>: holding patterns.</summary>
	Hpf,

	/// <summary><c>ILS</c>: instrument landing systems.</summary>
	Ils,

	/// <summary><c>LID</c>: location identifiers.</summary>
	Lid,

	/// <summary><c>MAA</c>: miscellaneous activity areas.</summary>
	Maa,

	/// <summary><c>MIL_OPS</c>: military operations at airports.</summary>
	MilOps,

	/// <summary><c>MTR</c>: military training routes.</summary>
	Mtr,

	/// <summary><c>NAV</c>: NAVAIDs.</summary>
	Nav,

	/// <summary><c>PFR</c>: preferred routes.</summary>
	Pfr,

	/// <summary><c>PJA</c>: parachute jump areas.</summary>
	Pja,

	/// <summary><c>RDR</c>: radar facilities.</summary>
	Rdr,

	/// <summary><c>STAR</c>: standard terminal arrivals.</summary>
	Star,

	/// <summary><c>WXL</c>: weather reporting locations.</summary>
	Wxl,
}
