using FEBuddyLibrary.Parsers.NASR.CSV;
using System;
using System.IO;

namespace FEBuddyTest;
internal static class Program
{
    public static void Main()
    {
        // Input/output directories
        string userSelectedSourceDirectory = @"C:\Users\ksand\Downloads\03_Sep_2026_CSV";
        string userSelectedOutputDirectory = @"C:\Users\ksand\Downloads";

        bool parseAptNasrCsv = true;
        bool parseAtcNasrCsv = true;
        bool parseAwyNasrCsv = true;
        bool parseArbNasrCsv = true;
        bool parseAwosNasrCsv = true;
        bool parseClsArspNasrCsv = true;
        bool parseCdrNasrCsv = true;
        bool parseComNasrCsv = true;
        bool parseDpNasrCsv = true;
        bool parseFixNasrCsv = true;
        bool parseFssNasrCsv = true;
        bool parseFrqNasrCsv = true;
        bool parseHpfNasrCsv = true;
        bool parseIlsNasrCsv = true;
        bool parseLidNasrCsv = true;
        bool parseMilOpsNasrCsv = true;
        bool parseMtrNasrCsv = true;
        bool parseMaaNasrCsv = true;
        bool parseNavNasrCsv = true;
        bool parsePjaNasrCsv = true;
        bool parsePfrNasrCsv = true;
        bool parseRdrNasrCsv = true;
        bool parseStarNasrCsv = true;
        bool parseWxlNasrCsv = true;


        // Parsed data is declared here so it remains available after each conditional block.
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

        if (parseAptNasrCsv)
        {
            Console.WriteLine("Parsing APT csv files");
            AptCsvParser aptCsvParser = new AptCsvParser();
            allParsedAptData = new AptCsvDataCollection();
            allParsedAptData.AptArs = aptCsvParser.ParseAptArs(Path.Combine(userSelectedSourceDirectory, "APT_ARS.csv")).AptArs;
            allParsedAptData.AptAtt = aptCsvParser.ParseAptAtt(Path.Combine(userSelectedSourceDirectory, "APT_ATT.csv")).AptAtt;
            allParsedAptData.AptBase = aptCsvParser.ParseAptBase(Path.Combine(userSelectedSourceDirectory, "APT_BASE.csv")).AptBase;
            allParsedAptData.AptCon = aptCsvParser.ParseAptCon(Path.Combine(userSelectedSourceDirectory, "APT_CON.csv")).AptCon;
            allParsedAptData.AptRmk = aptCsvParser.ParseAptRmk(Path.Combine(userSelectedSourceDirectory, "APT_RMK.csv")).AptRmk;
            allParsedAptData.AptRwy = aptCsvParser.ParseAptRwy(Path.Combine(userSelectedSourceDirectory, "APT_RWY.csv")).AptRwy;
            allParsedAptData.AptRwyEnd = aptCsvParser.ParseAptRwyEnd(Path.Combine(userSelectedSourceDirectory, "APT_RWY_END.csv")).AptRwyEnd;
        }

        if (parseAtcNasrCsv)
        {
            Console.WriteLine("Parsing Atc csv files");
            AtcCsvParser atcCsvParser = new AtcCsvParser();
            allParsedAtcData = new AtcCsvDataCollection();
            allParsedAtcData.AtcAtis = atcCsvParser.ParseAtcAtis(Path.Combine(userSelectedSourceDirectory, "ATC_ATIS.csv")).AtcAtis;
            allParsedAtcData.AtcBase = atcCsvParser.ParseAtcBase(Path.Combine(userSelectedSourceDirectory, "ATC_BASE.csv")).AtcBase;
            allParsedAtcData.AtcRmk = atcCsvParser.ParseAtcRmk(Path.Combine(userSelectedSourceDirectory, "ATC_RMK.csv")).AtcRmk;
            allParsedAtcData.AtcSvc = atcCsvParser.ParseAtcSvc(Path.Combine(userSelectedSourceDirectory, "ATC_SVC.csv")).AtcSvc;
        }

        if (parseAwyNasrCsv)
        {
            Console.WriteLine("Parsing Awy csv files");
            AwyCsvParser awyCsvParser = new AwyCsvParser();
            allParsedAwyData = new AwyCsvDataCollection();
            allParsedAwyData.AwyBase = awyCsvParser.ParseAwyBase(Path.Combine(userSelectedSourceDirectory, "AWY_BASE.csv")).AwyBase;
            allParsedAwyData.AwySegAlt = awyCsvParser.ParseAwySegAlt(Path.Combine(userSelectedSourceDirectory, "AWY_SEG_ALT.csv")).AwySegAlt;
        }

        if (parseArbNasrCsv)
        {
            Console.WriteLine("Parsing Arb csv files");
            ArbCsvParser arbCsvParser = new ArbCsvParser();
            allParsedArbData = new ArbCsvDataCollection();
            allParsedArbData.ArbBase = arbCsvParser.ParseArbBase(Path.Combine(userSelectedSourceDirectory, "ARB_BASE.csv")).ArbBase;
            allParsedArbData.ArbSeg = arbCsvParser.ParseArbSeg(Path.Combine(userSelectedSourceDirectory, "ARB_SEG.csv")).ArbSeg;
        }

        if (parseAwosNasrCsv)
        {
            Console.WriteLine("Parsing Awos csv files");
            AwosCsvParser awosCsvParser = new AwosCsvParser();
            allParsedAwosData = new AwosCsvDataCollection();
            allParsedAwosData.Awos = awosCsvParser.ParseAwos(Path.Combine(userSelectedSourceDirectory, "AWOS.csv")).Awos;
        }

        if (parseClsArspNasrCsv)
        {
            Console.WriteLine("Parsing ClsArsp csv files");
            ClsArspCsvParser clsArspCsvParser = new ClsArspCsvParser();
            allParsedClsArspData = new ClsArspCsvDataCollection();
            allParsedClsArspData.ClsArsp = clsArspCsvParser.ParseClsArsp(Path.Combine(userSelectedSourceDirectory, "CLS_ARSP.csv")).ClsArsp;
        }

        if (parseCdrNasrCsv)
        {
            Console.WriteLine("Parsing Cdr csv files");
            CdrCsvParser cdrCsvParser = new CdrCsvParser();
            allParsedCdrData = new CdrCsvDataCollection();
            allParsedCdrData.Cdr = cdrCsvParser.ParseCdr(Path.Combine(userSelectedSourceDirectory, "CDR.csv")).Cdr;
        }

        if (parseComNasrCsv)
        {
            Console.WriteLine("Parsing Com csv files");
            ComCsvParser comCsvParser = new ComCsvParser();
            allParsedComData = new ComCsvDataCollection();
            allParsedComData.Com = comCsvParser.ParseCom(Path.Combine(userSelectedSourceDirectory, "COM.csv")).Com;
        }

        if (parseDpNasrCsv)
        {
            Console.WriteLine("Parsing Dp csv files");
            DpCsvParser dpCsvParser = new DpCsvParser();
            allParsedDpData = new DpCsvDataCollection();
            allParsedDpData.DpApt = dpCsvParser.ParseDpApt(Path.Combine(userSelectedSourceDirectory, "DP_APT.csv")).DpApt;
            allParsedDpData.DpBase = dpCsvParser.ParseDpBase(Path.Combine(userSelectedSourceDirectory, "DP_BASE.csv")).DpBase;
            allParsedDpData.DpRte = dpCsvParser.ParseDpRte(Path.Combine(userSelectedSourceDirectory, "DP_RTE.csv")).DpRte;
        }

        if (parseFixNasrCsv)
        {
            Console.WriteLine("Parsing Fix csv files");
            FixCsvParser fixCsvParser = new FixCsvParser();
            allParsedFixData = new FixCsvDataCollection();
            allParsedFixData.FixBase = fixCsvParser.ParseFixBase(Path.Combine(userSelectedSourceDirectory, "FIX_BASE.csv")).FixBase;
            allParsedFixData.FixChrt = fixCsvParser.ParseFixChrt(Path.Combine(userSelectedSourceDirectory, "FIX_CHRT.csv")).FixChrt;
            allParsedFixData.FixNav = fixCsvParser.ParseFixNav(Path.Combine(userSelectedSourceDirectory, "FIX_NAV.csv")).FixNav;
        }

        if (parseFssNasrCsv)
        {
            Console.WriteLine("Parsing Fss csv files");
            FssCsvParser fssCsvParser = new FssCsvParser();
            allParsedFssData = new FssCsvDataCollection();
            allParsedFssData.FssBase = fssCsvParser.ParseFssBase(Path.Combine(userSelectedSourceDirectory, "FSS_BASE.csv")).FssBase;
            allParsedFssData.FssRmk = fssCsvParser.ParseFssRmk(Path.Combine(userSelectedSourceDirectory, "FSS_RMK.csv")).FssRmk;
        }

        if (parseFrqNasrCsv)
        {
            Console.WriteLine("Parsing Frq csv files");
            FrqCsvParser frqCsvParser = new FrqCsvParser();
            allParsedFrqData = new FrqCsvDataCollection();
            allParsedFrqData.Frq = frqCsvParser.ParseFrq(Path.Combine(userSelectedSourceDirectory, "FRQ.csv")).Frq;
        }

        if (parseHpfNasrCsv)
        {
            Console.WriteLine("Parsing Hpf csv files");
            HpfCsvParser hpfCsvParser = new HpfCsvParser();
            allParsedHpfData = new HpfCsvDataCollection();
            allParsedHpfData.HpfBase = hpfCsvParser.ParseHpfBase(Path.Combine(userSelectedSourceDirectory, "HPF_BASE.csv")).HpfBase;
            allParsedHpfData.HpfChrt = hpfCsvParser.ParseHpfChrt(Path.Combine(userSelectedSourceDirectory, "HPF_CHRT.csv")).HpfChrt;
            allParsedHpfData.HpfRmk = hpfCsvParser.ParseHpfRmk(Path.Combine(userSelectedSourceDirectory, "HPF_RMK.csv")).HpfRmk;
            allParsedHpfData.HpfSpdAlt = hpfCsvParser.ParseHpfSpdAlt(Path.Combine(userSelectedSourceDirectory, "HPF_SPD_ALT.csv")).HpfSpdAlt;
        }

        if (parseIlsNasrCsv)
        {
            Console.WriteLine("Parsing Ils csv files");
            IlsCsvParser ilsCsvParser = new IlsCsvParser();
            allParsedIlsData = new IlsCsvDataCollection();
            allParsedIlsData.IlsBase = ilsCsvParser.ParseIlsBase(Path.Combine(userSelectedSourceDirectory, "ILS_BASE.csv")).IlsBase;
            allParsedIlsData.IlsDme = ilsCsvParser.ParseIlsDme(Path.Combine(userSelectedSourceDirectory, "ILS_DME.csv")).IlsDme;
            allParsedIlsData.IlsGs = ilsCsvParser.ParseIlsGs(Path.Combine(userSelectedSourceDirectory, "ILS_GS.csv")).IlsGs;
            allParsedIlsData.IlsMkr = ilsCsvParser.ParseIlsMkr(Path.Combine(userSelectedSourceDirectory, "ILS_MKR.csv")).IlsMkr;
            allParsedIlsData.IlsRmk = ilsCsvParser.ParseIlsRmk(Path.Combine(userSelectedSourceDirectory, "ILS_RMK.csv")).IlsRmk;
        }

        if (parseLidNasrCsv)
        {
            Console.WriteLine("Parsing Lid csv files");
            LidCsvParser lidCsvParser = new LidCsvParser();
            allParsedLidData = new LidCsvDataCollection();
            allParsedLidData.Lid = lidCsvParser.ParseLid(Path.Combine(userSelectedSourceDirectory, "LID.csv")).Lid;
        }

        if (parseMilOpsNasrCsv)
        {
            Console.WriteLine("Parsing MilOps csv files");
            MilOpsCsvParser milCsvParser = new MilOpsCsvParser();
            allParsedMilData = new MilOpsCsvDataCollection();
            allParsedMilData.MilOps = milCsvParser.ParseMilOps(Path.Combine(userSelectedSourceDirectory, "MIL_OPS.csv")).MilOps;
        }

        if (parseMtrNasrCsv)
        {
            Console.WriteLine("Parsing Mtr csv files");
            MtrCsvParser mtrCsvParser = new MtrCsvParser();
            allParsedMtrData = new MtrCsvDataCollection();
            allParsedMtrData.MtrAgy = mtrCsvParser.ParseMtrAgy(Path.Combine(userSelectedSourceDirectory, "MTR_AGY.csv")).MtrAgy;
            allParsedMtrData.MtrBase = mtrCsvParser.ParseMtrBase(Path.Combine(userSelectedSourceDirectory, "MTR_BASE.csv")).MtrBase;
            allParsedMtrData.MtrPt = mtrCsvParser.ParseMtrPt(Path.Combine(userSelectedSourceDirectory, "MTR_PT.csv")).MtrPt;
            allParsedMtrData.MtrSop = mtrCsvParser.ParseMtrSop(Path.Combine(userSelectedSourceDirectory, "MTR_SOP.csv")).MtrSop;
            allParsedMtrData.MtrTerr = mtrCsvParser.ParseMtrTerr(Path.Combine(userSelectedSourceDirectory, "MTR_TERR.csv")).MtrTerr;
            allParsedMtrData.MtrWdth = mtrCsvParser.ParseMtrWdth(Path.Combine(userSelectedSourceDirectory, "MTR_WDTH.csv")).MtrWdth;
        }

        if (parseMaaNasrCsv)
        {
            Console.WriteLine("Parsing Maa csv files");
            MaaCsvParser maaCsvParser = new MaaCsvParser();
            allParsedMaaData = new MaaCsvDataCollection();
            allParsedMaaData.MaaBase = maaCsvParser.ParseMaaBase(Path.Combine(userSelectedSourceDirectory, "MAA_BASE.csv")).MaaBase;
            allParsedMaaData.MaaCon = maaCsvParser.ParseMaaCon(Path.Combine(userSelectedSourceDirectory, "MAA_CON.csv")).MaaCon;
            allParsedMaaData.MaaRmk = maaCsvParser.ParseMaaRmk(Path.Combine(userSelectedSourceDirectory, "MAA_RMK.csv")).MaaRmk;
            allParsedMaaData.MaaShp = maaCsvParser.ParseMaaShp(Path.Combine(userSelectedSourceDirectory, "MAA_SHP.csv")).MaaShp;
        }

        if (parseNavNasrCsv)
        {
            Console.WriteLine("Parsing Nav csv files");
            NavCsvParser navCsvParser = new NavCsvParser();
            allParsedNavData = new NavCsvDataCollection();
            allParsedNavData.NavBase = navCsvParser.ParseNavBase(Path.Combine(userSelectedSourceDirectory, "NAV_BASE.csv")).NavBase;
            allParsedNavData.NavCkpt = navCsvParser.ParseNavCkpt(Path.Combine(userSelectedSourceDirectory, "NAV_CKPT.csv")).NavCkpt;
            allParsedNavData.NavRmk = navCsvParser.ParseNavRmk(Path.Combine(userSelectedSourceDirectory, "NAV_RMK.csv")).NavRmk;
        }

        if (parsePjaNasrCsv)
        {
            Console.WriteLine("Parsing Pja csv files");
            PjaCsvParser pjaCsvParser = new PjaCsvParser();
            allParsedPjaData = new PjaCsvDataCollection();
            allParsedPjaData.PjaBase = pjaCsvParser.ParsePjaBase(Path.Combine(userSelectedSourceDirectory, "PJA_BASE.csv")).PjaBase;
            allParsedPjaData.PjaCon = pjaCsvParser.ParsePjaCon(Path.Combine(userSelectedSourceDirectory, "PJA_CON.csv")).PjaCon;
        }

        if (parsePfrNasrCsv)
        {
            Console.WriteLine("Parsing Pfr csv files");
            PfrCsvParser pfrCsvParser = new PfrCsvParser();
            allParsedPfrData = new PfrCsvDataCollection();
            allParsedPfrData.PfrBase = pfrCsvParser.ParsePfrBase(Path.Combine(userSelectedSourceDirectory, "PFR_BASE.csv")).PfrBase;
            allParsedPfrData.PfrRmtFmt = pfrCsvParser.ParsePfrRmtFmt(Path.Combine(userSelectedSourceDirectory, "PFR_RMT_FMT.csv")).PfrRmtFmt;
            allParsedPfrData.PfrSeg = pfrCsvParser.ParsePfrSeg(Path.Combine(userSelectedSourceDirectory, "PFR_SEG.csv")).PfrSeg;
        }

        if (parseRdrNasrCsv)
        {
            Console.WriteLine("Parsing Rdr csv files");
            RdrCsvParser rdrCsvParser = new RdrCsvParser();
            allParsedRdrData = new RdrCsvDataCollection();
            allParsedRdrData.Rdr = rdrCsvParser.ParseRdr(Path.Combine(userSelectedSourceDirectory, "RDR.csv")).Rdr;
        }

        if (parseStarNasrCsv)
        {
            Console.WriteLine("Parsing Star csv files");
            StarCsvParser starCsvParser = new StarCsvParser();
            allParsedStarData = new StarCsvDataCollection();
            allParsedStarData.StarApt = starCsvParser.ParseStarApt(Path.Combine(userSelectedSourceDirectory, "STAR_APT.csv")).StarApt;
            allParsedStarData.StarBase = starCsvParser.ParseStarBase(Path.Combine(userSelectedSourceDirectory, "STAR_BASE.csv")).StarBase;
            allParsedStarData.StarRte = starCsvParser.ParseStarRte(Path.Combine(userSelectedSourceDirectory, "STAR_RTE.csv")).StarRte;
        }

        if (parseWxlNasrCsv)
        {
            Console.WriteLine("Parsing Wxl csv files");
            WxlCsvParser wxlCsvParser = new WxlCsvParser();
            allParsedWxlData = new WxlCsvDataCollection();
            allParsedWxlData.WxlBase = wxlCsvParser.ParseWxlBase(Path.Combine(userSelectedSourceDirectory, "WXL_BASE.csv")).WxlBase;
            allParsedWxlData.WxlSvc = wxlCsvParser.ParseWxlSvc(Path.Combine(userSelectedSourceDirectory, "WXL_SVC.csv")).WxlSvc;
        }

        Console.WriteLine("NASR CSV parsing complete.");

        // GeoJSON creation will go here after the required
        // relationships/properties/geometries are defined.
    }
}
