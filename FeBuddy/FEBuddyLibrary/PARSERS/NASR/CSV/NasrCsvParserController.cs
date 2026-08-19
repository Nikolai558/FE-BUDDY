using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Parsers.NASR.CSV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEBuddyLibrary.Parsers.NASR.CSV;

/// <summary>
/// Primary call-point to parse all NASR CSV files and combine them into a single collection of data.
/// </summary>
public class NasrCsvParserController
{
	public static NasrCsvDataCollection Main(string[] args)
	{
		// Check if the source directory argument is provided
		string sourceDirectory = args[0];


		// Initialize data collections for each NASR CSV type
		AptCsvDataCollection? allParsedAptData = null;
		AtcCsvDataCollection? allParsedAtcData = null;
		AwyCsvDataCollection? allParsedAwyData = null;
		ArbCsvDataCollection? allParsedArbData = null;
		AwosCsvDataCollection? allParsedAwosData = null;
		ClsArspCsvDataCollection? allParsedClsArspData = null;
		CdrCsvDataCollection? allParsedCdrData = null;
		ComCsvDataCollection? allParsedComData = null;
		DpCsvDataCollection? allParsedDpData = null;
		FixCsvDataCollection? allParsedFixData = null;
		FssCsvDataCollection? allParsedFssData = null;
		FrqCsvDataCollection? allParsedFrqData = null;
		HpfCsvDataCollection? allParsedHpfData = null;
		IlsCsvDataCollection? allParsedIlsData = null;
		LidCsvDataCollection? allParsedLidData = null;
		MilOpsCsvDataCollection? allParsedMilData = null;
		MtrCsvDataCollection? allParsedMtrData = null;
		MaaCsvDataCollection? allParsedMaaData = null;
		NavCsvDataCollection? allParsedNavData = null;
		PjaCsvDataCollection? allParsedPjaData = null;
		PfrCsvDataCollection? allParsedPfrData = null;
		RdrCsvDataCollection? allParsedRdrData = null;
		StarCsvDataCollection? allParsedStarData = null;
		WxlCsvDataCollection? allParsedWxlData = null;


		// Parse APT csv files
		AptCsvParser aptCsvParser = new AptCsvParser();
		allParsedAptData = new AptCsvDataCollection();
		allParsedAptData.AptArs = aptCsvParser.ParseAptArs(Path.Combine(sourceDirectory, "APT_ARS.csv")).AptArs;
		allParsedAptData.AptAtt = aptCsvParser.ParseAptAtt(Path.Combine(sourceDirectory, "APT_ATT.csv")).AptAtt;
		allParsedAptData.AptBase = aptCsvParser.ParseAptBase(Path.Combine(sourceDirectory, "APT_BASE.csv")).AptBase;
		allParsedAptData.AptCon = aptCsvParser.ParseAptCon(Path.Combine(sourceDirectory, "APT_CON.csv")).AptCon;
		allParsedAptData.AptRmk = aptCsvParser.ParseAptRmk(Path.Combine(sourceDirectory, "APT_RMK.csv")).AptRmk;
		allParsedAptData.AptRwy = aptCsvParser.ParseAptRwy(Path.Combine(sourceDirectory, "APT_RWY.csv")).AptRwy;
		allParsedAptData.AptRwyEnd = aptCsvParser.ParseAptRwyEnd(Path.Combine(sourceDirectory, "APT_RWY_END.csv")).AptRwyEnd;

		// Parse ATC csv files
		AtcCsvParser atcCsvParser = new AtcCsvParser();
		allParsedAtcData = new AtcCsvDataCollection();
		allParsedAtcData.AtcAtis = atcCsvParser.ParseAtcAtis(Path.Combine(sourceDirectory, "ATC_ATIS.csv")).AtcAtis;
		allParsedAtcData.AtcBase = atcCsvParser.ParseAtcBase(Path.Combine(sourceDirectory, "ATC_BASE.csv")).AtcBase;
		allParsedAtcData.AtcRmk = atcCsvParser.ParseAtcRmk(Path.Combine(sourceDirectory, "ATC_RMK.csv")).AtcRmk;
		allParsedAtcData.AtcSvc = atcCsvParser.ParseAtcSvc(Path.Combine(sourceDirectory, "ATC_SVC.csv")).AtcSvc;

		// Parse AWY csv files
		AwyCsvParser awyCsvParser = new AwyCsvParser();
		allParsedAwyData = new AwyCsvDataCollection();
		allParsedAwyData.AwyBase = awyCsvParser.ParseAwyBase(Path.Combine(sourceDirectory, "AWY_BASE.csv")).AwyBase;
		allParsedAwyData.AwySegAlt = awyCsvParser.ParseAwySegAlt(Path.Combine(sourceDirectory, "AWY_SEG_ALT.csv")).AwySegAlt;

		// Parse ARB csv files
		ArbCsvParser arbCsvParser = new ArbCsvParser();
		allParsedArbData = new ArbCsvDataCollection();
		allParsedArbData.ArbBase = arbCsvParser.ParseArbBase(Path.Combine(sourceDirectory, "ARB_BASE.csv")).ArbBase;
		allParsedArbData.ArbSeg = arbCsvParser.ParseArbSeg(Path.Combine(sourceDirectory, "ARB_SEG.csv")).ArbSeg;

		// Parse AWOS csv files
		AwosCsvParser awosCsvParser = new AwosCsvParser();
		allParsedAwosData = new AwosCsvDataCollection();
		allParsedAwosData.Awos = awosCsvParser.ParseAwos(Path.Combine(sourceDirectory, "AWOS.csv")).Awos;

		// Parse CLS_ARSP csv files
		ClsArspCsvParser clsArspCsvParser = new ClsArspCsvParser();
		allParsedClsArspData = new ClsArspCsvDataCollection();
		allParsedClsArspData.ClsArsp = clsArspCsvParser.ParseClsArsp(Path.Combine(sourceDirectory, "CLS_ARSP.csv")).ClsArsp;

		// Parse CDR csv files
		CdrCsvParser cdrCsvParser = new CdrCsvParser();
		allParsedCdrData = new CdrCsvDataCollection();
		allParsedCdrData.Cdr = cdrCsvParser.ParseCdr(Path.Combine(sourceDirectory, "CDR.csv")).Cdr;

		// Parse COM csv files
		ComCsvParser comCsvParser = new ComCsvParser();
		allParsedComData = new ComCsvDataCollection();
		allParsedComData.Com = comCsvParser.ParseCom(Path.Combine(sourceDirectory, "COM.csv")).Com;

		// Parse DP csv files
		DpCsvParser dpCsvParser = new DpCsvParser();
		allParsedDpData = new DpCsvDataCollection();
		allParsedDpData.DpApt = dpCsvParser.ParseDpApt(Path.Combine(sourceDirectory, "DP_APT.csv")).DpApt;
		allParsedDpData.DpBase = dpCsvParser.ParseDpBase(Path.Combine(sourceDirectory, "DP_BASE.csv")).DpBase;
		allParsedDpData.DpRte = dpCsvParser.ParseDpRte(Path.Combine(sourceDirectory, "DP_RTE.csv")).DpRte;

		// Parse FIX csv files
		FixCsvParser fixCsvParser = new FixCsvParser();
		allParsedFixData = new FixCsvDataCollection();
		allParsedFixData.FixBase = fixCsvParser.ParseFixBase(Path.Combine(sourceDirectory, "FIX_BASE.csv")).FixBase;
		allParsedFixData.FixChrt = fixCsvParser.ParseFixChrt(Path.Combine(sourceDirectory, "FIX_CHRT.csv")).FixChrt;
		allParsedFixData.FixNav = fixCsvParser.ParseFixNav(Path.Combine(sourceDirectory, "FIX_NAV.csv")).FixNav;

		// Parse FSS csv files
		FssCsvParser fssCsvParser = new FssCsvParser();
		allParsedFssData = new FssCsvDataCollection();
		allParsedFssData.FssBase = fssCsvParser.ParseFssBase(Path.Combine(sourceDirectory, "FSS_BASE.csv")).FssBase;
		allParsedFssData.FssRmk = fssCsvParser.ParseFssRmk(Path.Combine(sourceDirectory, "FSS_RMK.csv")).FssRmk;

		// Parse FRQ csv files
		FrqCsvParser frqCsvParser = new FrqCsvParser();
		allParsedFrqData = new FrqCsvDataCollection();
		allParsedFrqData.Frq = frqCsvParser.ParseFrq(Path.Combine(sourceDirectory, "FRQ.csv")).Frq;

		// Parse HPF csv files
		HpfCsvParser hpfCsvParser = new HpfCsvParser();
		allParsedHpfData = new HpfCsvDataCollection();
		allParsedHpfData.HpfBase = hpfCsvParser.ParseHpfBase(Path.Combine(sourceDirectory, "HPF_BASE.csv")).HpfBase;
		allParsedHpfData.HpfChrt = hpfCsvParser.ParseHpfChrt(Path.Combine(sourceDirectory, "HPF_CHRT.csv")).HpfChrt;
		allParsedHpfData.HpfRmk = hpfCsvParser.ParseHpfRmk(Path.Combine(sourceDirectory, "HPF_RMK.csv")).HpfRmk;
		allParsedHpfData.HpfSpdAlt = hpfCsvParser.ParseHpfSpdAlt(Path.Combine(sourceDirectory, "HPF_SPD_ALT.csv")).HpfSpdAlt;

		// Parse ILS csv files
		IlsCsvParser ilsCsvParser = new IlsCsvParser();
		allParsedIlsData = new IlsCsvDataCollection();
		allParsedIlsData.IlsBase = ilsCsvParser.ParseIlsBase(Path.Combine(sourceDirectory, "ILS_BASE.csv")).IlsBase;
		allParsedIlsData.IlsDme = ilsCsvParser.ParseIlsDme(Path.Combine(sourceDirectory, "ILS_DME.csv")).IlsDme;
		allParsedIlsData.IlsGs = ilsCsvParser.ParseIlsGs(Path.Combine(sourceDirectory, "ILS_GS.csv")).IlsGs;
		allParsedIlsData.IlsMkr = ilsCsvParser.ParseIlsMkr(Path.Combine(sourceDirectory, "ILS_MKR.csv")).IlsMkr;
		allParsedIlsData.IlsRmk = ilsCsvParser.ParseIlsRmk(Path.Combine(sourceDirectory, "ILS_RMK.csv")).IlsRmk;

		// Parse LID csv files
		LidCsvParser lidCsvParser = new LidCsvParser();
		allParsedLidData = new LidCsvDataCollection();
		allParsedLidData.Lid = lidCsvParser.ParseLid(Path.Combine(sourceDirectory, "LID.csv")).Lid;

		// Parse MIL_OPS csv files
		MilOpsCsvParser milCsvParser = new MilOpsCsvParser();
		allParsedMilData = new MilOpsCsvDataCollection();
		allParsedMilData.MilOps = milCsvParser.ParseMilOps(Path.Combine(sourceDirectory, "MIL_OPS.csv")).MilOps;

		// Parse MTR csv files
		MtrCsvParser mtrCsvParser = new MtrCsvParser();
		allParsedMtrData = new MtrCsvDataCollection();
		allParsedMtrData.MtrAgy = mtrCsvParser.ParseMtrAgy(Path.Combine(sourceDirectory, "MTR_AGY.csv")).MtrAgy;
		allParsedMtrData.MtrBase = mtrCsvParser.ParseMtrBase(Path.Combine(sourceDirectory, "MTR_BASE.csv")).MtrBase;
		allParsedMtrData.MtrPt = mtrCsvParser.ParseMtrPt(Path.Combine(sourceDirectory, "MTR_PT.csv")).MtrPt;
		allParsedMtrData.MtrSop = mtrCsvParser.ParseMtrSop(Path.Combine(sourceDirectory, "MTR_SOP.csv")).MtrSop;
		allParsedMtrData.MtrTerr = mtrCsvParser.ParseMtrTerr(Path.Combine(sourceDirectory, "MTR_TERR.csv")).MtrTerr;
		allParsedMtrData.MtrWdth = mtrCsvParser.ParseMtrWdth(Path.Combine(sourceDirectory, "MTR_WDTH.csv")).MtrWdth;

		// Parse MAA csv files
		MaaCsvParser maaCsvParser = new MaaCsvParser();
		allParsedMaaData = new MaaCsvDataCollection();
		allParsedMaaData.MaaBase = maaCsvParser.ParseMaaBase(Path.Combine(sourceDirectory, "MAA_BASE.csv")).MaaBase;
		allParsedMaaData.MaaCon = maaCsvParser.ParseMaaCon(Path.Combine(sourceDirectory, "MAA_CON.csv")).MaaCon;
		allParsedMaaData.MaaRmk = maaCsvParser.ParseMaaRmk(Path.Combine(sourceDirectory, "MAA_RMK.csv")).MaaRmk;
		allParsedMaaData.MaaShp = maaCsvParser.ParseMaaShp(Path.Combine(sourceDirectory, "MAA_SHP.csv")).MaaShp;

		// Parse NAV csv files
		NavCsvParser navCsvParser = new NavCsvParser();
		allParsedNavData = new NavCsvDataCollection();
		allParsedNavData.NavBase = navCsvParser.ParseNavBase(Path.Combine(sourceDirectory, "NAV_BASE.csv")).NavBase;
		allParsedNavData.NavCkpt = navCsvParser.ParseNavCkpt(Path.Combine(sourceDirectory, "NAV_CKPT.csv")).NavCkpt;
		allParsedNavData.NavRmk = navCsvParser.ParseNavRmk(Path.Combine(sourceDirectory, "NAV_RMK.csv")).NavRmk;

		// Parse PJA csv files
		PjaCsvParser pjaCsvParser = new PjaCsvParser();
		allParsedPjaData = new PjaCsvDataCollection();
		allParsedPjaData.PjaBase = pjaCsvParser.ParsePjaBase(Path.Combine(sourceDirectory, "PJA_BASE.csv")).PjaBase;
		allParsedPjaData.PjaCon = pjaCsvParser.ParsePjaCon(Path.Combine(sourceDirectory, "PJA_CON.csv")).PjaCon;

		// Parse PFR csv files
		PfrCsvParser pfrCsvParser = new PfrCsvParser();
		allParsedPfrData = new PfrCsvDataCollection();
		allParsedPfrData.PfrBase = pfrCsvParser.ParsePfrBase(Path.Combine(sourceDirectory, "PFR_BASE.csv")).PfrBase;
		allParsedPfrData.PfrRmtFmt = pfrCsvParser.ParsePfrRmtFmt(Path.Combine(sourceDirectory, "PFR_RMT_FMT.csv")).PfrRmtFmt;
		allParsedPfrData.PfrSeg = pfrCsvParser.ParsePfrSeg(Path.Combine(sourceDirectory, "PFR_SEG.csv")).PfrSeg;

		// Parse RDR csv files
		RdrCsvParser rdrCsvParser = new RdrCsvParser();
		allParsedRdrData = new RdrCsvDataCollection();
		allParsedRdrData.Rdr = rdrCsvParser.ParseRdr(Path.Combine(sourceDirectory, "RDR.csv")).Rdr;

		// Parse STAR csv files
		StarCsvParser starCsvParser = new StarCsvParser();
		allParsedStarData = new StarCsvDataCollection();
		allParsedStarData.StarApt = starCsvParser.ParseStarApt(Path.Combine(sourceDirectory, "STAR_APT.csv")).StarApt;
		allParsedStarData.StarBase = starCsvParser.ParseStarBase(Path.Combine(sourceDirectory, "STAR_BASE.csv")).StarBase;
		allParsedStarData.StarRte = starCsvParser.ParseStarRte(Path.Combine(sourceDirectory, "STAR_RTE.csv")).StarRte;

		// Parse WXL csv files
		WxlCsvParser wxlCsvParser = new WxlCsvParser();
		allParsedWxlData = new WxlCsvDataCollection();
		allParsedWxlData.WxlBase = wxlCsvParser.ParseWxlBase(Path.Combine(sourceDirectory, "WXL_BASE.csv")).WxlBase;
		allParsedWxlData.WxlSvc = wxlCsvParser.ParseWxlSvc(Path.Combine(sourceDirectory, "WXL_SVC.csv")).WxlSvc;


		// Combine all parsed NASR CSV data into a single collection "allNasrCsvData"
		NasrCsvDataCollection allNasrCsvData = new()
		{
			Apt = allParsedAptData,
			Atc = allParsedAtcData,
			Awy = allParsedAwyData,
			Arb = allParsedArbData,
			Awos = allParsedAwosData,
			ClsArsp = allParsedClsArspData,
			Cdr = allParsedCdrData,
			Com = allParsedComData,
			Dp = allParsedDpData,
			Fix = allParsedFixData,
			Fss = allParsedFssData,
			Frq = allParsedFrqData,
			Hpf = allParsedHpfData,
			Ils = allParsedIlsData,
			Lid = allParsedLidData,
			MilOps = allParsedMilData,
			Mtr = allParsedMtrData,
			Maa = allParsedMaaData,
			Nav = allParsedNavData,
			Pja = allParsedPjaData,
			Pfr = allParsedPfrData,
			Rdr = allParsedRdrData,
			Star = allParsedStarData,
			Wxl = allParsedWxlData
		};

		return allNasrCsvData;
	}
}
