using FeBuddy.Core.Infrastructure.Nasr.Models;

using static FeBuddy.Core.Infrastructure.Nasr.Models.AptCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.ArbCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.AtcCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.AwosCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.AwyCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.CdrCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.ClsArspCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.ComCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.DpCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.FixCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.FrqCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.FssCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.HpfCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.IlsCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.LidCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.MaaCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.MilOpsCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.MtrCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.NavCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.PfrCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.PjaCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.RdrCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.StarCsvDataModel;
using static FeBuddy.Core.Infrastructure.Nasr.Models.WxlCsvDataModel;

namespace FeBuddy.Core.Infrastructure.Nasr.Parsers;

/// <summary>
/// Parses a whole NASR CSV cycle into one <see cref="NasrCsvDataCollection"/>.
/// </summary>
public class NasrCsvParser
{
	/// <summary>
	/// Parses all NASR CSV file groups concurrently and combines them into a single collection.
	/// Each top-level group (APT, ATC, AWY, ...) runs in parallel, and within a group each
	/// individual file is also parsed in parallel, since none of the files depend on
	/// one another's output.
	/// </summary>
	/// <param name="sourceDirectory">The folder holding one cycle's extracted NASR CSV files.</param>
	/// <returns>Every group's parsed data.</returns>
	public static async Task<NasrCsvDataCollection> ParseAllAsync(string sourceDirectory)
	{
		// Start every group before awaiting any, so they all run at once.
		Task<AptCsvDataCollection> aptTask = ParseAptAsync(sourceDirectory);
		Task<AtcCsvDataCollection> atcTask = ParseAtcAsync(sourceDirectory);
		Task<AwyCsvDataCollection> awyTask = ParseAwyAsync(sourceDirectory);
		Task<ArbCsvDataCollection> arbTask = ParseArbAsync(sourceDirectory);
		Task<AwosCsvDataCollection> awosTask = ParseAwosAsync(sourceDirectory);
		Task<ClsArspCsvDataCollection> clsArspTask = ParseClsArspAsync(sourceDirectory);
		Task<CdrCsvDataCollection> cdrTask = ParseCdrAsync(sourceDirectory);
		Task<ComCsvDataCollection> comTask = ParseComAsync(sourceDirectory);
		Task<DpCsvDataCollection> dpTask = ParseDpAsync(sourceDirectory);
		Task<FixCsvDataCollection> fixTask = ParseFixAsync(sourceDirectory);
		Task<FssCsvDataCollection> fssTask = ParseFssAsync(sourceDirectory);
		Task<FrqCsvDataCollection> frqTask = ParseFrqAsync(sourceDirectory);
		Task<HpfCsvDataCollection> hpfTask = ParseHpfAsync(sourceDirectory);
		Task<IlsCsvDataCollection> ilsTask = ParseIlsAsync(sourceDirectory);
		Task<LidCsvDataCollection> lidTask = ParseLidAsync(sourceDirectory);
		Task<MilOpsCsvDataCollection> milTask = ParseMilOpsAsync(sourceDirectory);
		Task<MtrCsvDataCollection> mtrTask = ParseMtrAsync(sourceDirectory);
		Task<MaaCsvDataCollection> maaTask = ParseMaaAsync(sourceDirectory);
		Task<NavCsvDataCollection> navTask = ParseNavAsync(sourceDirectory);
		Task<PjaCsvDataCollection> pjaTask = ParsePjaAsync(sourceDirectory);
		Task<PfrCsvDataCollection> pfrTask = ParsePfrAsync(sourceDirectory);
		Task<RdrCsvDataCollection> rdrTask = ParseRdrAsync(sourceDirectory);
		Task<StarCsvDataCollection> starTask = ParseStarAsync(sourceDirectory);
		Task<WxlCsvDataCollection> wxlTask = ParseWxlAsync(sourceDirectory);

		await Task.WhenAll(
			aptTask, atcTask, awyTask, arbTask, awosTask, clsArspTask, cdrTask, comTask,
			dpTask, fixTask, fssTask, frqTask, hpfTask, ilsTask, lidTask, milTask,
			mtrTask, maaTask, navTask, pjaTask, pfrTask, rdrTask, starTask, wxlTask
		);

		NasrCsvDataCollection allNasrCsvData = new()
		{
			Apt = aptTask.Result,
			Atc = atcTask.Result,
			Awy = awyTask.Result,
			Arb = arbTask.Result,
			Awos = awosTask.Result,
			ClsArsp = clsArspTask.Result,
			Cdr = cdrTask.Result,
			Com = comTask.Result,
			Dp = dpTask.Result,
			Fix = fixTask.Result,
			Fss = fssTask.Result,
			Frq = frqTask.Result,
			Hpf = hpfTask.Result,
			Ils = ilsTask.Result,
			Lid = lidTask.Result,
			MilOps = milTask.Result,
			Mtr = mtrTask.Result,
			Maa = maaTask.Result,
			Nav = navTask.Result,
			Pja = pjaTask.Result,
			Pfr = pfrTask.Result,
			Rdr = rdrTask.Result,
			Star = starTask.Result,
			Wxl = wxlTask.Result
		};

		return allNasrCsvData;
	}

	// Each group parses its files in parallel, then assembles the group once all are done. Every
	// file gets its own parser instance, so no parser is ever shared between threads.

	private static async Task<AptCsvDataCollection> ParseAptAsync(string sourceDirectory)
	{
		Task<List<AptArs>> arsTask = Task.Run(() => new AptCsvParser().ParseAptArs(Path.Combine(sourceDirectory, "APT_ARS.csv")).AptArs);
		Task<List<AptAtt>> attTask = Task.Run(() => new AptCsvParser().ParseAptAtt(Path.Combine(sourceDirectory, "APT_ATT.csv")).AptAtt);
		Task<List<AptBase>> baseTask = Task.Run(() => new AptCsvParser().ParseAptBase(Path.Combine(sourceDirectory, "APT_BASE.csv")).AptBase);
		Task<List<AptCon>> conTask = Task.Run(() => new AptCsvParser().ParseAptCon(Path.Combine(sourceDirectory, "APT_CON.csv")).AptCon);
		Task<List<AptRmk>> rmkTask = Task.Run(() => new AptCsvParser().ParseAptRmk(Path.Combine(sourceDirectory, "APT_RMK.csv")).AptRmk);
		Task<List<AptRwy>> rwyTask = Task.Run(() => new AptCsvParser().ParseAptRwy(Path.Combine(sourceDirectory, "APT_RWY.csv")).AptRwy);
		Task<List<AptRwyEnd>> rwyEndTask = Task.Run(() => new AptCsvParser().ParseAptRwyEnd(Path.Combine(sourceDirectory, "APT_RWY_END.csv")).AptRwyEnd);

		await Task.WhenAll(arsTask, attTask, baseTask, conTask, rmkTask, rwyTask, rwyEndTask);

		return new AptCsvDataCollection
		{
			AptArs = arsTask.Result,
			AptAtt = attTask.Result,
			AptBase = baseTask.Result,
			AptCon = conTask.Result,
			AptRmk = rmkTask.Result,
			AptRwy = rwyTask.Result,
			AptRwyEnd = rwyEndTask.Result
		};
	}

	private static async Task<AtcCsvDataCollection> ParseAtcAsync(string sourceDirectory)
	{
		Task<List<AtcAtis>> atisTask = Task.Run(() => new AtcCsvParser().ParseAtcAtis(Path.Combine(sourceDirectory, "ATC_ATIS.csv")).AtcAtis);
		Task<List<AtcBase>> baseTask = Task.Run(() => new AtcCsvParser().ParseAtcBase(Path.Combine(sourceDirectory, "ATC_BASE.csv")).AtcBase);
		Task<List<AtcRmk>> rmkTask = Task.Run(() => new AtcCsvParser().ParseAtcRmk(Path.Combine(sourceDirectory, "ATC_RMK.csv")).AtcRmk);
		Task<List<AtcSvc>> svcTask = Task.Run(() => new AtcCsvParser().ParseAtcSvc(Path.Combine(sourceDirectory, "ATC_SVC.csv")).AtcSvc);

		await Task.WhenAll(atisTask, baseTask, rmkTask, svcTask);

		return new AtcCsvDataCollection
		{
			AtcAtis = atisTask.Result,
			AtcBase = baseTask.Result,
			AtcRmk = rmkTask.Result,
			AtcSvc = svcTask.Result
		};
	}

	private static async Task<AwyCsvDataCollection> ParseAwyAsync(string sourceDirectory)
	{
		Task<List<AwyBase>> baseTask = Task.Run(() => new AwyCsvParser().ParseAwyBase(Path.Combine(sourceDirectory, "AWY_BASE.csv")).AwyBase);
		Task<List<AwySegAlt>> segAltTask = Task.Run(() => new AwyCsvParser().ParseAwySegAlt(Path.Combine(sourceDirectory, "AWY_SEG_ALT.csv")).AwySegAlt);

		await Task.WhenAll(baseTask, segAltTask);

		return new AwyCsvDataCollection
		{
			AwyBase = baseTask.Result,
			AwySegAlt = segAltTask.Result
		};
	}

	private static async Task<ArbCsvDataCollection> ParseArbAsync(string sourceDirectory)
	{
		Task<List<ArbBase>> baseTask = Task.Run(() => new ArbCsvParser().ParseArbBase(Path.Combine(sourceDirectory, "ARB_BASE.csv")).ArbBase);
		Task<List<ArbSeg>> segTask = Task.Run(() => new ArbCsvParser().ParseArbSeg(Path.Combine(sourceDirectory, "ARB_SEG.csv")).ArbSeg);

		await Task.WhenAll(baseTask, segTask);

		return new ArbCsvDataCollection
		{
			ArbBase = baseTask.Result,
			ArbSeg = segTask.Result
		};
	}

	private static async Task<AwosCsvDataCollection> ParseAwosAsync(string sourceDirectory)
	{
		List<Awos> awos = await Task.Run(() => new AwosCsvParser().ParseAwos(Path.Combine(sourceDirectory, "AWOS.csv")).Awos);

		return new AwosCsvDataCollection
		{
			Awos = awos
		};
	}

	private static async Task<ClsArspCsvDataCollection> ParseClsArspAsync(string sourceDirectory)
	{
		List<ClsArsp> clsArsp = await Task.Run(() => new ClsArspCsvParser().ParseClsArsp(Path.Combine(sourceDirectory, "CLS_ARSP.csv")).ClsArsp);

		return new ClsArspCsvDataCollection
		{
			ClsArsp = clsArsp
		};
	}

	private static async Task<CdrCsvDataCollection> ParseCdrAsync(string sourceDirectory)
	{
		List<Cdr> cdr = await Task.Run(() => new CdrCsvParser().ParseCdr(Path.Combine(sourceDirectory, "CDR.csv")).Cdr);

		return new CdrCsvDataCollection
		{
			Cdr = cdr
		};
	}

	private static async Task<ComCsvDataCollection> ParseComAsync(string sourceDirectory)
	{
		List<Com> com = await Task.Run(() => new ComCsvParser().ParseCom(Path.Combine(sourceDirectory, "COM.csv")).Com);

		return new ComCsvDataCollection
		{
			Com = com
		};
	}

	private static async Task<DpCsvDataCollection> ParseDpAsync(string sourceDirectory)
	{
		Task<List<DpApt>> aptTask = Task.Run(() => new DpCsvParser().ParseDpApt(Path.Combine(sourceDirectory, "DP_APT.csv")).DpApt);
		Task<List<DpBase>> baseTask = Task.Run(() => new DpCsvParser().ParseDpBase(Path.Combine(sourceDirectory, "DP_BASE.csv")).DpBase);
		Task<List<DpRte>> rteTask = Task.Run(() => new DpCsvParser().ParseDpRte(Path.Combine(sourceDirectory, "DP_RTE.csv")).DpRte);

		await Task.WhenAll(aptTask, baseTask, rteTask);

		return new DpCsvDataCollection
		{
			DpApt = aptTask.Result,
			DpBase = baseTask.Result,
			DpRte = rteTask.Result
		};
	}

	private static async Task<FixCsvDataCollection> ParseFixAsync(string sourceDirectory)
	{
		Task<List<FixBase>> baseTask = Task.Run(() => new FixCsvParser().ParseFixBase(Path.Combine(sourceDirectory, "FIX_BASE.csv")).FixBase);
		Task<List<FixChrt>> chrtTask = Task.Run(() => new FixCsvParser().ParseFixChrt(Path.Combine(sourceDirectory, "FIX_CHRT.csv")).FixChrt);
		Task<List<FixNav>> navTask = Task.Run(() => new FixCsvParser().ParseFixNav(Path.Combine(sourceDirectory, "FIX_NAV.csv")).FixNav);

		await Task.WhenAll(baseTask, chrtTask, navTask);

		return new FixCsvDataCollection
		{
			FixBase = baseTask.Result,
			FixChrt = chrtTask.Result,
			FixNav = navTask.Result
		};
	}

	private static async Task<FssCsvDataCollection> ParseFssAsync(string sourceDirectory)
	{
		Task<List<FssBase>> baseTask = Task.Run(() => new FssCsvParser().ParseFssBase(Path.Combine(sourceDirectory, "FSS_BASE.csv")).FssBase);
		Task<List<FssRmk>> rmkTask = Task.Run(() => new FssCsvParser().ParseFssRmk(Path.Combine(sourceDirectory, "FSS_RMK.csv")).FssRmk);

		await Task.WhenAll(baseTask, rmkTask);

		return new FssCsvDataCollection
		{
			FssBase = baseTask.Result,
			FssRmk = rmkTask.Result
		};
	}

	private static async Task<FrqCsvDataCollection> ParseFrqAsync(string sourceDirectory)
	{
		List<Frq> frq = await Task.Run(() => new FrqCsvParser().ParseFrq(Path.Combine(sourceDirectory, "FRQ.csv")).Frq);

		return new FrqCsvDataCollection
		{
			Frq = frq
		};
	}

	private static async Task<HpfCsvDataCollection> ParseHpfAsync(string sourceDirectory)
	{
		Task<List<HpfBase>> baseTask = Task.Run(() => new HpfCsvParser().ParseHpfBase(Path.Combine(sourceDirectory, "HPF_BASE.csv")).HpfBase);
		Task<List<HpfChrt>> chrtTask = Task.Run(() => new HpfCsvParser().ParseHpfChrt(Path.Combine(sourceDirectory, "HPF_CHRT.csv")).HpfChrt);
		Task<List<HpfRmk>> rmkTask = Task.Run(() => new HpfCsvParser().ParseHpfRmk(Path.Combine(sourceDirectory, "HPF_RMK.csv")).HpfRmk);
		Task<List<HpfSpdAlt>> spdAltTask = Task.Run(() => new HpfCsvParser().ParseHpfSpdAlt(Path.Combine(sourceDirectory, "HPF_SPD_ALT.csv")).HpfSpdAlt);

		await Task.WhenAll(baseTask, chrtTask, rmkTask, spdAltTask);

		return new HpfCsvDataCollection
		{
			HpfBase = baseTask.Result,
			HpfChrt = chrtTask.Result,
			HpfRmk = rmkTask.Result,
			HpfSpdAlt = spdAltTask.Result
		};
	}

	private static async Task<IlsCsvDataCollection> ParseIlsAsync(string sourceDirectory)
	{
		Task<List<IlsBase>> baseTask = Task.Run(() => new IlsCsvParser().ParseIlsBase(Path.Combine(sourceDirectory, "ILS_BASE.csv")).IlsBase);
		Task<List<IlsDme>> dmeTask = Task.Run(() => new IlsCsvParser().ParseIlsDme(Path.Combine(sourceDirectory, "ILS_DME.csv")).IlsDme);
		Task<List<IlsGs>> gsTask = Task.Run(() => new IlsCsvParser().ParseIlsGs(Path.Combine(sourceDirectory, "ILS_GS.csv")).IlsGs);
		Task<List<IlsMkr>> mkrTask = Task.Run(() => new IlsCsvParser().ParseIlsMkr(Path.Combine(sourceDirectory, "ILS_MKR.csv")).IlsMkr);
		Task<List<IlsRmk>> rmkTask = Task.Run(() => new IlsCsvParser().ParseIlsRmk(Path.Combine(sourceDirectory, "ILS_RMK.csv")).IlsRmk);

		await Task.WhenAll(baseTask, dmeTask, gsTask, mkrTask, rmkTask);

		return new IlsCsvDataCollection
		{
			IlsBase = baseTask.Result,
			IlsDme = dmeTask.Result,
			IlsGs = gsTask.Result,
			IlsMkr = mkrTask.Result,
			IlsRmk = rmkTask.Result
		};
	}

	private static async Task<LidCsvDataCollection> ParseLidAsync(string sourceDirectory)
	{
		List<Lid> lid = await Task.Run(() => new LidCsvParser().ParseLid(Path.Combine(sourceDirectory, "LID.csv")).Lid);

		return new LidCsvDataCollection
		{
			Lid = lid
		};
	}

	private static async Task<MilOpsCsvDataCollection> ParseMilOpsAsync(string sourceDirectory)
	{
		List<MilOps> milOps = await Task.Run(() => new MilOpsCsvParser().ParseMilOps(Path.Combine(sourceDirectory, "MIL_OPS.csv")).MilOps);

		return new MilOpsCsvDataCollection
		{
			MilOps = milOps
		};
	}

	private static async Task<MtrCsvDataCollection> ParseMtrAsync(string sourceDirectory)
	{
		Task<List<MtrAgy>> agyTask = Task.Run(() => new MtrCsvParser().ParseMtrAgy(Path.Combine(sourceDirectory, "MTR_AGY.csv")).MtrAgy);
		Task<List<MtrBase>> baseTask = Task.Run(() => new MtrCsvParser().ParseMtrBase(Path.Combine(sourceDirectory, "MTR_BASE.csv")).MtrBase);
		Task<List<MtrPt>> ptTask = Task.Run(() => new MtrCsvParser().ParseMtrPt(Path.Combine(sourceDirectory, "MTR_PT.csv")).MtrPt);
		Task<List<MtrSop>> sopTask = Task.Run(() => new MtrCsvParser().ParseMtrSop(Path.Combine(sourceDirectory, "MTR_SOP.csv")).MtrSop);
		Task<List<MtrTerr>> terrTask = Task.Run(() => new MtrCsvParser().ParseMtrTerr(Path.Combine(sourceDirectory, "MTR_TERR.csv")).MtrTerr);
		Task<List<MtrWdth>> wdthTask = Task.Run(() => new MtrCsvParser().ParseMtrWdth(Path.Combine(sourceDirectory, "MTR_WDTH.csv")).MtrWdth);

		await Task.WhenAll(agyTask, baseTask, ptTask, sopTask, terrTask, wdthTask);

		return new MtrCsvDataCollection
		{
			MtrAgy = agyTask.Result,
			MtrBase = baseTask.Result,
			MtrPt = ptTask.Result,
			MtrSop = sopTask.Result,
			MtrTerr = terrTask.Result,
			MtrWdth = wdthTask.Result
		};
	}

	private static async Task<MaaCsvDataCollection> ParseMaaAsync(string sourceDirectory)
	{
		Task<List<MaaBase>> baseTask = Task.Run(() => new MaaCsvParser().ParseMaaBase(Path.Combine(sourceDirectory, "MAA_BASE.csv")).MaaBase);
		Task<List<MaaCon>> conTask = Task.Run(() => new MaaCsvParser().ParseMaaCon(Path.Combine(sourceDirectory, "MAA_CON.csv")).MaaCon);
		Task<List<MaaRmk>> rmkTask = Task.Run(() => new MaaCsvParser().ParseMaaRmk(Path.Combine(sourceDirectory, "MAA_RMK.csv")).MaaRmk);
		Task<List<MaaShp>> shpTask = Task.Run(() => new MaaCsvParser().ParseMaaShp(Path.Combine(sourceDirectory, "MAA_SHP.csv")).MaaShp);

		await Task.WhenAll(baseTask, conTask, rmkTask, shpTask);

		return new MaaCsvDataCollection
		{
			MaaBase = baseTask.Result,
			MaaCon = conTask.Result,
			MaaRmk = rmkTask.Result,
			MaaShp = shpTask.Result
		};
	}

	private static async Task<NavCsvDataCollection> ParseNavAsync(string sourceDirectory)
	{
		Task<List<NavBase>> baseTask = Task.Run(() => new NavCsvParser().ParseNavBase(Path.Combine(sourceDirectory, "NAV_BASE.csv")).NavBase);
		Task<List<NavCkpt>> ckptTask = Task.Run(() => new NavCsvParser().ParseNavCkpt(Path.Combine(sourceDirectory, "NAV_CKPT.csv")).NavCkpt);
		Task<List<NavRmk>> rmkTask = Task.Run(() => new NavCsvParser().ParseNavRmk(Path.Combine(sourceDirectory, "NAV_RMK.csv")).NavRmk);

		await Task.WhenAll(baseTask, ckptTask, rmkTask);

		return new NavCsvDataCollection
		{
			NavBase = baseTask.Result,
			NavCkpt = ckptTask.Result,
			NavRmk = rmkTask.Result
		};
	}

	private static async Task<PjaCsvDataCollection> ParsePjaAsync(string sourceDirectory)
	{
		Task<List<PjaBase>> baseTask = Task.Run(() => new PjaCsvParser().ParsePjaBase(Path.Combine(sourceDirectory, "PJA_BASE.csv")).PjaBase);
		Task<List<PjaCon>> conTask = Task.Run(() => new PjaCsvParser().ParsePjaCon(Path.Combine(sourceDirectory, "PJA_CON.csv")).PjaCon);

		await Task.WhenAll(baseTask, conTask);

		return new PjaCsvDataCollection
		{
			PjaBase = baseTask.Result,
			PjaCon = conTask.Result
		};
	}

	private static async Task<PfrCsvDataCollection> ParsePfrAsync(string sourceDirectory)
	{
		Task<List<PfrBase>> baseTask = Task.Run(() => new PfrCsvParser().ParsePfrBase(Path.Combine(sourceDirectory, "PFR_BASE.csv")).PfrBase);
		Task<List<PfrRmtFmt>> rmtFmtTask = Task.Run(() => new PfrCsvParser().ParsePfrRmtFmt(Path.Combine(sourceDirectory, "PFR_RMT_FMT.csv")).PfrRmtFmt);
		Task<List<PfrSeg>> segTask = Task.Run(() => new PfrCsvParser().ParsePfrSeg(Path.Combine(sourceDirectory, "PFR_SEG.csv")).PfrSeg);

		await Task.WhenAll(baseTask, rmtFmtTask, segTask);

		return new PfrCsvDataCollection
		{
			PfrBase = baseTask.Result,
			PfrRmtFmt = rmtFmtTask.Result,
			PfrSeg = segTask.Result
		};
	}

	private static async Task<RdrCsvDataCollection> ParseRdrAsync(string sourceDirectory)
	{
		List<Rdr> rdr = await Task.Run(() => new RdrCsvParser().ParseRdr(Path.Combine(sourceDirectory, "RDR.csv")).Rdr);

		return new RdrCsvDataCollection
		{
			Rdr = rdr
		};
	}

	private static async Task<StarCsvDataCollection> ParseStarAsync(string sourceDirectory)
	{
		Task<List<StarApt>> aptTask = Task.Run(() => new StarCsvParser().ParseStarApt(Path.Combine(sourceDirectory, "STAR_APT.csv")).StarApt);
		Task<List<StarBase>> baseTask = Task.Run(() => new StarCsvParser().ParseStarBase(Path.Combine(sourceDirectory, "STAR_BASE.csv")).StarBase);
		Task<List<StarRte>> rteTask = Task.Run(() => new StarCsvParser().ParseStarRte(Path.Combine(sourceDirectory, "STAR_RTE.csv")).StarRte);

		await Task.WhenAll(aptTask, baseTask, rteTask);

		return new StarCsvDataCollection
		{
			StarApt = aptTask.Result,
			StarBase = baseTask.Result,
			StarRte = rteTask.Result
		};
	}

	private static async Task<WxlCsvDataCollection> ParseWxlAsync(string sourceDirectory)
	{
		Task<List<WxlBase>> baseTask = Task.Run(() => new WxlCsvParser().ParseWxlBase(Path.Combine(sourceDirectory, "WXL_BASE.csv")).WxlBase);
		Task<List<WxlSvc>> svcTask = Task.Run(() => new WxlCsvParser().ParseWxlSvc(Path.Combine(sourceDirectory, "WXL_SVC.csv")).WxlSvc);

		await Task.WhenAll(baseTask, svcTask);

		return new WxlCsvDataCollection
		{
			WxlBase = baseTask.Result,
			WxlSvc = svcTask.Result
		};
	}
}