# 210 · Code comments and XML docs: NASR models and parsers

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Comments and XML docs in the NASR models and parsers. The code stays even where unused, so only its docs were reviewed. Session [S7](020_Work-Plan.md#s7).

**25 findings** (3 Med, 22 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [C8-26](#c8-26) | Minor | Low | FYI: NASR groups the app never parses or reads (waiting for later) | Open | S7 |
| [C8-1](#c8-1) | Stale doc | Med | AiracCyclePublicationState says AiracCycleResolver uses a "static table" | Open | S7 |
| [C8-12](#c8-12) | Wrong doc | Med | AptRwyEnd: thirteen remarks copied from neighbouring columns | Open | S7 |
| [C8-7](#c8-7) | Wrong doc | Med | PfrRmtFmt inherits PFR CommonFields, which PFR_RMT_FMT.csv doesn't have | Open | S7 |
| [C8-17](#c8-17) | Wrong doc | Low | `string?` / "NULL" docs: a blank column is an empty string, never null | Open | S7 |
| [C8-2](#c8-2) | Wrong doc | Low | AiracCyclePublicationState value docs don't match the probe | Open | S7 |
| [C8-3](#c8-3) | Wrong doc | Low | AiracDownloadProgress.PercentComplete: says null outside Downloading, but 100 is reported | Open | S7 |
| [C8-10](#c8-10) | Wrong doc | Low | AptBase.Elev documented as not nullable but declared and parsed nullable | Open | S7 |
| [C8-11](#c8-11) | Wrong doc | Low | AptRwy.RwyLenSource remark is RwyLgtCode's edge-lighting values | Open | S7 |
| [C8-13](#c8-13) | Wrong doc | Low | ASDA expanded as "Actual Stop Distance Available" | Open | S7 |
| [C8-19](#c8-19) | Minor | Low | Typos in model docs | Open | S7 |
| [C8-20](#c8-20) | Wrong doc | Low | Small factual slips in APT remarks | Open | S7 |
| [C8-8](#c8-8) | Wrong doc | Low | APT_ARS/APT_BASE summaries and remarks copied from the neighbouring column | Open | S7 |
| [C8-9](#c8-9) | Wrong doc | Low | AptBase: "PropertyName changed" remark on the two properties that weren't renamed, missing on the two that were | Open | S7 |
| [C8-21](#c8-21) | Minor | Low | Inconsistent names and summaries across the 24 models | Open | S7 |
| [C8-14](#c8-14) | Wrong doc | Low | AtcBase: six summaries name the wrong column (primary/secondary, ID/type mixed up) | Open | S7 |
| [C8-5](#c8-5) | Wrong doc | Low | AWOS, CLS_ARSP and DP common fields copied from APT (wrong `_Src`, airport wording) | Open | S7 |
| [C8-6](#c8-6) | Wrong doc | Low | CDR and PFR_RMT_FMT `_Src` column names don't match the headers the parser reads | Open | S7 |
| [C8-18](#c8-18) | Minor | Low | Placeholder text left in XML docs | Open | S7 |
| [C8-15](#c8-15) | Wrong doc | Low | IlsBase.Owner summary says "Operator" | Open | S7 |
| [C8-16](#c8-16) | Wrong doc | Low | NavBase: TACAN decimal-latitude remark says "Hemisphere"; PitchFlag remark garbled | Open | S7 |
| [C8-4](#c8-4) | Wrong doc | Low | EffDate `_Src` says "All Apt_*.csv files" in 22 non-APT models | Open | S7 |
| [C8-22](#c8-22) | Minor | Low | NasrCsvParser: "own parser instance" comment implies a thread-safety need that doesn't exist; class could be static | Open | S7 |
| [C8-23](#c8-23) | Minor | Low | NasrCsvParser.ParseAsync/ParseAllAsync don't document what they throw | Open | S7 |
| [C8-25](#c8-25) | Minor | Low | FYI: files parsed every cycle (their group is in InUse) that no feature reads | Open | S7 |

## FeBuddy.Core/Infrastructure/Nasr

<a id="c8-26"></a>

### C8-26 · Minor · Low · FYI: NASR groups the app never parses or reads (waiting for later)

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Infrastructure/Nasr/NasrCsvGroups.cs:22-33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCsvGroups.cs#L22-L33); [`Models/NasrCsvDataCollection.cs:17-64`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/NasrCsvDataCollection.cs#L17-L64)

**Says:** InUse = Apt, Arb, Awy, ClsArsp, Dp, Fix, Frq, Nav, Star

**Problem:** information only. 15 of 24 groups are parsed only by `NasrCsvParser.ParseAllAsync` (FeBuddy.Harness/Program.cs:36 and tests) and read by no feature: ATC, AWOS, CDR, COM, FSS, HPF, ILS, LID, MAA, MIL_OPS, MTR, PFR, PJA, RDR, WXL. Their `Require…` methods have no production caller (RequireIls is used by one test, NasrCsvGroupParseTests). All 9 InUse groups are read by at least one feature.

**Proposed:** none (kept by the owner's choice). Obvious future uses: ILS (localizer GeoJSON), MTR/MAA/PJA (SUA-style maps), PFR/CDR (route aliases), ATC/FRQ (telephony/frequencies).

## FeBuddy.Core/Infrastructure/Nasr/Models

<a id="c8-1"></a>

### C8-1 · Stale doc · Med · AiracCyclePublicationState says AiracCycleResolver uses a "static table"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AiracCyclePublicationState.cs:4-6`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AiracCyclePublicationState.cs#L4-L6)

**Says:** `fact from whether <c>AiracCycleResolver</c> can name the cycle (it names next-cycle identities from a static table long before the FAA publishes the data).`

**Problem:** AiracCycleResolver has no table; it computes every cycle from one reference date and the 28-day cadence. Its own remarks say "every cycle can be calculated from any one known cycle - no lookup table to maintain" (Domain/Airac/AiracCycleResolver.cs:11-13).

**Proposed:** "Whether the FAA has published a cycle's NASR CSV subscription yet - a separate fact from whether &lt;see cref="AiracCycleResolver"/> can name the cycle (it works out the next cycle from the 28-day cadence long before the FAA publishes its data)." (add `using FeBuddy.Core.Domain.Airac;` or fully qualify the cref).

<a id="c8-12"></a>

### C8-12 · Wrong doc · Med · AptRwyEnd: thirteen remarks copied from neighbouring columns

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AptCsvDataModel.cs:1258`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L1258), 1354, 1425, 1667, 1700, 1718, 1736, 1754, 1772, 1781, 1840, 1858

**Says:** RwyEndLongDeg (int) `Examples: "31.60022222" "-31.60022222"`; DisplacedThrLatDeg `Hundredths of Degrees`; RwyGrad `Direction (LEFT or RIGHT) to the controlling object ...`; TkofRunAvbl/TkofDistAvbl `Format: YYYY/MM/DD`

**Problem:** 1258 RwyEndRwyEndId: "The Runway End described by the Arresting System Information" is from APT_ARS, and "by directory" should be "by direction". 1354: decimal examples on an int degrees field. 1425: VisualGlidePathAngle's unit on a degrees field. 1667: CntrlnDirCode's text on the runway gradient. 1700/1718/1736/1754 (RwyEndElevSource, DsplThrPsnSource, DsplThrElevSource, TdzElevSource): date format on source (text) fields. 1772/1781: date format on distances in feet. 1840/1858 (LatLahsoDecimal/LongLahsoDecimal): DMS examples `34-39-00.0210N` / `086-44-52.9867W` on decimal-degree fields.

**Proposed:** 1258 "PropertyName changed due to identical column name in other APT_*.csv files. The runway end in one direction, where RwyEndRwyId names both ends. Examples: "03L" "23""; 1354 delete the remarks; 1425 delete the remarks; 1667 "Gradient of the runway end, in percent."; 1700/1718/1736/1754 delete the remarks (or "Source of the ... data."); 1772/1781 delete the remarks (the summary already says "In Feet"); 1840 `Example: 34.65000583`; 1858 `Example: -86.74805186`.

<a id="c8-7"></a>

### C8-7 · Wrong doc · Med · PfrRmtFmt inherits PFR CommonFields, which PFR_RMT_FMT.csv doesn't have

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/PfrCsvDataModels.cs:9-60`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/PfrCsvDataModels.cs#L9-L60), 235; [`Parsers/PfrCsvParser.cs:56-72`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Parsers/PfrCsvParser.cs#L56-L72)

**Says:** `/// <summary>The columns every <c>PFR</c> file shares.</summary>` ... `public class PfrRmtFmt : CommonFields`

**Problem:** PFR_RMT_FMT.csv has none of EFF_DATE, ORIGIN_ID, DSTN_ID, PFR_TYPE_CODE, ROUTE_NO; ParsePfrRmtFmt never sets them, so every PfrRmtFmt row carries EffDate/OriginId/DstnId/PfrTypeCode = null (declared non-nullable `string`) and RouteNo = 0. Its own Orig/Dest/Type/Seq hold that data. The summary "every PFR file shares" is false, and nothing on PfrRmtFmt warns a future reader (only a test exemption list, NasrCsvParserMappingTests.cs:94-104, knows).

**Proposed:** preferred: `public class PfrRmtFmt` with no base (and remove the five "PfrRmtFmt.*" entries from NasrCsvParserMappingTests.PropertiesWithNoColumn). If the base is kept: CommonFields summary "The columns PFR_BASE.csv and PFR_SEG.csv share." and a remark on PfrRmtFmt: "PFR_RMT_FMT.csv has none of the CommonFields columns; they stay at their defaults. Use Orig, Dest, Type and Seq."

<a id="c8-17"></a>

### C8-17 · Wrong doc · Low · `string?` / "NULL" docs: a blank column is an empty string, never null

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** all Models/*CsvDataModel(s).cs (every `_Nullable: Yes` string); specifically [`ClsArspCsvDataModels.cs:96`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/ClsArspCsvDataModels.cs#L96),106,116,126; [`FixCsvDataModels.cs:248`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/FixCsvDataModels.cs#L248); [`FrqCsvDataModels.cs:36`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/FrqCsvDataModels.cs#L36),46; [`PfrCsvDataModels.cs:84`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/PfrCsvDataModels.cs#L84),112,396,406

**Says:** `Terminal Communication Facility containing Class B Airspace will be designated with "Y" else null`; `Null in this field identifies Non-Compulsory FIX.`; `NULL if outside the US.`

**Problem:** NasrCsvReader.ProcessLines stores every value as a non-null string ("a column the row is too short to reach reads as an empty string, never null", NasrCsvReader.cs:29-30,60-63), and parsers assign it as is. So `ClassBAirspace` etc. are "" when blank, never null, and `x is null` checks on these properties are always false. The FAA-layout wording reads as a statement about the property.

**Proposed:** add one sentence to each model class summary (or once in each CommonFields summary): "String columns the FAA leaves blank read as an empty string, never null; `_Nullable` describes the FAA's layout." and change "else null"/"NULL" in the listed remarks to "else blank".

<a id="c8-2"></a>

### C8-2 · Wrong doc · Low · AiracCyclePublicationState value docs don't match the probe

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AiracCyclePublicationState.cs:14-15`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AiracCyclePublicationState.cs#L14-L15), 20-21

**Says:** `The cycle's CSV archive returned "not found"` / `The probe could not reach the FAA (network error, timeout). ... the pipeline should retry rather than conclude anything.`

**Problem:** AiracCycleAvailability.SendAsync (lines 102-109) returns NotYetPublished for 404 **or 403**, and Unknown for any other non-success status (405, 5xx, unfollowed redirect) as well as network errors. On Unknown the cache probes once more and then "tries the download and lets it decide" (AiracCycleDataCache.cs:629-641) - it does conclude something: it treats the cycle as downloadable.

**Proposed:** NotYetPublished: "The cycle's CSV archive returned 404 or 403 - the FAA has not published it yet. ..." Unknown: "The probe was inconclusive: a network error or timeout, or an HTTP status other than success, 403 or 404. The cycle cache probes once more, then tries the download anyway and lets that decide."

<a id="c8-3"></a>

### C8-3 · Wrong doc · Low · AiracDownloadProgress.PercentComplete: says null outside Downloading, but 100 is reported

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AiracDownloadProgress.cs:23-27`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AiracDownloadProgress.cs#L23-L27)

**Says:** `0-100 while <see cref="Phase"/> is <see cref="AiracDownloadPhase.Downloading"/> and the server reported a content length; otherwise <see langword="null"/>`

**Problem:** NasrCycleDownloader reports `(AlreadyAvailable, 100)` (line 125) and `(Complete, 100)` (line 190); only Extracting (line 151) and a Downloading update without a length are null.

**Proposed:** "0-100 while Downloading when the server sent a Content-Length; 100 for AlreadyAvailable and Complete; otherwise &lt;see langword="null"/> (the FAA server does not always send a Content-Length header, and extraction has no meaningful percentage)."

<a id="c8-10"></a>

### C8-10 · Wrong doc · Low · AptBase.Elev documented as not nullable but declared and parsed nullable

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AptCsvDataModel.cs:336-343`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L336-L343); [`Parsers/AptCsvParser.cs:102`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Parsers/AptCsvParser.cs#L102)

**Says:** `/// _Nullable: No` ... `public double? Elev { get; set; }`; parser `Elev = NasrCsvReader.ParseNullableDouble(fields["ELEV"])`

**Problem:** the only property in all 24 models whose `_Nullable` disagrees with its C# type. The code is the safer choice (a blank ELEV would otherwise fail the whole APT group, which every airport feature needs), but the doc doesn't say so.

**Proposed:** keep the code; add `<remarks>... Read as nullable so a blank value can't fail the APT parse, though the FAA marks the column not nullable.</remarks>`.

<a id="c8-11"></a>

### C8-11 · Wrong doc · Low · AptRwy.RwyLenSource remark is RwyLgtCode's edge-lighting values

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AptCsvDataModel.cs:1187`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L1187)

**Says:** `<remarks>Possible values: HIGH, MED, LOW, NSTD (Non-Standard), NONE (No Edge Lighting).</remarks>` on RWY_LEN_SOURCE

**Problem:** RWY_LEN_SOURCE is the source of the runway length (free text, max 16), not an edge-light intensity.

**Proposed:** `<remarks>Source of the runway length data.</remarks>` (or delete the remarks).

<a id="c8-13"></a>

### C8-13 · Wrong doc · Low · ASDA expanded as "Actual Stop Distance Available"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AptCsvDataModel.cs:1784`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L1784)

**Says:** `/// Actual Stop Distance Available (ASDA), In Feet` on ACLT_STOP_DIST_AVBL

**Problem:** ASDA is Accelerate-Stop Distance Available (the column abbreviates ACLT = accelerate).

**Proposed:** "Accelerate-Stop Distance Available (ASDA), In Feet".

<a id="c8-19"></a>

### C8-19 · Minor · Low · Typos in model docs

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AptCsvDataModel.cs:1428`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L1428),1436,1444,1453,1461,1469,1477,1485,1494 ("Displace Threshold" x9), 1634; [`Models/DpCsvDataModels.cs:23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/DpCsvDataModels.cs#L23); [`Models/HpfCsvDataModels.cs:52`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/HpfCsvDataModels.cs#L52); [`Models/AwyCsvDataModels.cs:144`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AwyCsvDataModels.cs#L144); [`Models/NavCsvDataModels.cs:142`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/NavCsvDataModels.cs#L142); [`Models/StarCsvDataModels.cs:60`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/StarCsvDataModels.cs#L60),178; [`Models/LidCsvDataModels.cs:93`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/LidCsvDataModels.cs#L93); [`Models/FrqCsvDataModels.cs:155`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/FrqCsvDataModels.cs#L155)

**Says:** `Latitude Minutes at Displace Threshold`; `DP Commputer ID`; `Post Office County Code`; `The NAVIAD Facility City`; `NAVAID OPERATOR CODE - AME`; `Aditional Unique Body Sequence Identifier`; `the rare case that Body Name ...`; `DD=LDS/DME`; `Post Code of Serviced Facility`

**Problem:** spelling/wording slips (Lid's DD is LDA/DME, as IlsCsvDataModels.cs:104 says; FRQ SERVICED_COUNTRY is a country code, not a post code; DistFromThr says "Distance" twice).

**Proposed:** "Displaced Threshold"; "DP Computer ID"; "Post Office Country Code"; "NAVAID"; "NAVAID OPERATOR CODE - NAME"; "Additional"; "In the rare case ..."; "DD=LDA/DME"; "Country Post Office Code of Serviced Facility"; 1634 "Controlling Object Distance from Runway End (In Feet)".

<a id="c8-20"></a>

### C8-20 · Wrong doc · Low · Small factual slips in APT remarks

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AptCsvDataModel.cs:261`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L261), 306, 819, 1613, 1622

**Says:** `0 through 60. Includes milliseconds as decimals.`; ElevationSrcDate `Identifies the source of the airport's Elevation date data.`; FarPart77Code `<remarks>Utility Runway with Visual Approach; B(V)=...`; ObstnClncSlope `... value entered is 50.=`

**Problem:** the decimals are fractions of a second, not milliseconds; ELEVATION_SRC_DATE is a date, not a source; FarPart77Code's first entry lost its code; stray "=".

**Proposed:** 261/306 "0 through 60, with decimal fractions of a second."; 819 "Date the airport's elevation data was sourced. Format: YYYY/MM/DD."; 1613 start with "A(V)=Utility Runway with Visual Approach;"; 1622 end with "value entered is 50."

<a id="c8-8"></a>

### C8-8 · Wrong doc · Low · APT_ARS/APT_BASE summaries and remarks copied from the neighbouring column

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AptCsvDataModel.cs:95`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L95), 189, 489

**Says:** ArsRwyEndId (RWY_END_ID) summary `Runway Identification`; StateName remarks `Examples: "JAN" "MEM" "ATL"`; TollFreeNo summary `Local Phone Number from Airport to FSS for Administrative Services`

**Problem:** line 95 is the runway END id; line 189 repeats AdoCode's district-office examples on the state NAME; line 489 repeats BasePhoneNo's title on the toll-free number.

**Proposed:** 95 -> "Runway End Identification"; 189 -> `Examples: "ALABAMA" "TENNESSEE"`; 489 -> "Toll Free Phone Number from Airport to FSS for Pilot Briefing Services".

<a id="c8-9"></a>

### C8-9 · Wrong doc · Low · AptBase: "PropertyName changed" remark on the two properties that weren't renamed, missing on the two that were

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AptCsvDataModel.cs:270-271`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L270-L271), 279-280, 315-316, 324-325

**Says:** `LatHemis` / `LongHemis`: `PropertyName changed due to identical column name in other APT_*.csv files N, S, E, or W`; `BaseLatDecimal` / `BaseLongDecimal`: only `Examples: "31.60022222" "-31.60022222"`

**Problem:** LatHemis/LongHemis are exactly LAT_HEMIS/LONG_HEMIS (AptCsvParser.cs:94,99); it is BaseLatDecimal/BaseLongDecimal (LAT_DECIMAL/LONG_DECIMAL, lines 95,100) that were renamed. A latitude hemisphere is only N or S, a longitude only E or W. BaseLongDecimal's examples are latitudes.

**Proposed:** LatHemis remarks "N or S."; LongHemis "E or W."; BaseLatDecimal "PropertyName changed due to identical column name in other APT_*.csv files. Examples: "31.60022222" "-31.60022222""; BaseLongDecimal "PropertyName changed ... Examples: "-86.74863889" "144.79583333"".

<a id="c8-21"></a>

### C8-21 · Minor · Low · Inconsistent names and summaries across the 24 models

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/ArbCsvDataModels.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/ArbCsvDataModels.cs) ... [`Models/WxlCsvDataModels.cs`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/WxlCsvDataModels.cs) (22 files); [`Models/AptCsvDataModel.cs:3-5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L3-L5),10,81,116,160,914,997,1059,1238; [`Models/ComCsvDataModels.cs:3-5`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/ComCsvDataModels.cs#L3-L5); [`Models/FixCsvDataModels.cs:282-284`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/FixCsvDataModels.cs#L282-L284)

**Says:** file `ArbCsvDataModels.cs` holds `public class ArbCsvDataModel`; APT: `All model properties for the Apt_*.csv files.` / `Contains all model properties from APT_ARS.csv`; COM: `All model properties for the Com_*.csv files. FLIGHT SERVICE STATION COMMUNICATION FACILITIES DATA`

**Problem:** Code-Structure.md:8 "One main type per file, named after the file" - 22 model files are plural `…CsvDataModels.cs` while the type (and AptCsvDataModel.cs / AtcCsvDataModel.cs) is singular. APT and COM summaries use a different, older style from the other 22 ("Row models for the NASR &lt;c>ARB&lt;/c> CSV files (...): one nested class per file." / "One row of &lt;c>ARB_BASE.csv&lt;/c>."); FixNav's summary doesn't name its file.

**Proposed:** rename the 22 files to `…CsvDataModel.cs`. APT summary: "Row models for the NASR &lt;c>APT&lt;/c> CSV files (airports): one nested class per file."; CommonFields "The columns every &lt;c>APT&lt;/c> file shares."; nested "One row of &lt;c>APT_ARS.csv&lt;/c>." etc.; COM: "Row models for the NASR &lt;c>COM&lt;/c> CSV files (flight service station communication facilities): one nested class per file."; FixNav: "One row of &lt;c>FIX_NAV.csv&lt;/c>: a NAVAID near the fix, used to describe the fix's position."

<a id="c8-14"></a>

### C8-14 · Wrong doc · Low · AtcBase: six summaries name the wrong column (primary/secondary, ID/type mixed up)

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AtcCsvDataModel.cs:232`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AtcCsvDataModel.cs#L232), 242, 252, 281, 300, 310

**Says:** SecondaryApchRadioCall `Secondary Approach Control Facility ID`; ApchSProvider `Primary Approach Control Facility ID`; DepPProvTypeCd `Primary Departure Control Facility ID`; DepSProvider `Primary Departure Control Facility ID/Description`

**Problem:** 232 is a radio call, not an ID; 242 and 300 are the SECONDARY providers; 252, 281, 310 are provider TYPE codes (A/C/S/T), not facility IDs.

**Proposed:** 232 "Radio Call of Secondary Approach Control"; 242 "Secondary Approach Control Facility ID/Description"; 252 "Provider Agency Type Code for Agency that Operates the Secondary Approach Control Facility/Functions"; 281 "Provider Agency Type Code for Agency that Operates the Primary Departure Control Facility/Functions"; 300 "Secondary Departure Control Facility ID/Description"; 310 "Provider Agency Type Code for Agency that Operates the Secondary Departure Control Facility/Functions".

<a id="c8-5"></a>

### C8-5 · Wrong doc · Low · AWOS, CLS_ARSP and DP common fields copied from APT (wrong `_Src`, airport wording)

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/AwosCsvDataModels.cs:43`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AwosCsvDataModels.cs#L43),52-53,58,63,68; [`Models/ClsArspCsvDataModels.cs:24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/ClsArspCsvDataModels.cs#L24),34,44,54,64,74; [`Models/DpCsvDataModels.cs:24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/DpCsvDataModels.cs#L24); [`Models/CdrCsvDataModels.cs:14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/CdrCsvDataModels.cs#L14); [`Models/ComCsvDataModels.cs:24`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/ComCsvDataModels.cs#L24),34,44,54,64,74,84,94

**Says:** AWOS `_Src: All Apt_*.csv files(STATE_CODE)`, `Airport Associated City Name`, `<remarks>Name of city the airport is in.</remarks>`, `Country Post Office Code Airport Located`; DP `_Src: All Apt_*.csv files(DP_COMPUTER_CODE)`; CLS_ARSP `_Src: All CLS_ARSP_*.csv files(SITE_NO)`

**Problem:** AWOS STATE_CODE/CITY/COUNTRY_CODE come from AWOS.csv and describe the weather station, not an airport; DP_COMPUTER_CODE is in DP_APT/DP_BASE/DP_RTE; CLS_ARSP's STATE_CODE/CITY/COUNTRY_CODE say Apt_. The single-file groups (AWOS, CDR, CLS_ARSP, COM) say "All Xxx_*.csv files" but have exactly one file (AWOS.csv, CDR.csv, CLS_ARSP.csv, COM.csv).

**Proposed:** AWOS: `_Src: AWOS.csv(...)`, title "Weather System City", remarks "City the weather system is in." / "Country Post Office Code the weather system is in. Ex: "US""; DP:24 `_Src: All DP_*.csv files(DP_COMPUTER_CODE)`; CLS_ARSP: `_Src: CLS_ARSP.csv(...)` on all seven; CDR:14 `_Src: CDR.csv(RCode)`; COM: `_Src: COM.csv(...)`.

<a id="c8-6"></a>

### C8-6 · Wrong doc · Low · CDR and PFR_RMT_FMT `_Src` column names don't match the headers the parser reads

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/CdrCsvDataModels.cs:14`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/CdrCsvDataModels.cs#L14),31,41,51,60,87,96,106,115,125; [`Models/PfrCsvDataModels.cs:239`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/PfrCsvDataModels.cs#L239),249,259,269,279,289,298,307,317,326

**Says:** `_Src: CDR.csv(DEPFIX)` vs CdrCsvParser.cs:24 `fields["DepFix"]`; `_Src: PFR_RMT_FMT.csv(HOURS1)` vs PfrCsvParser.cs:63 `fields["Hours1"]`

**Problem:** these two files use mixed-case headers (`"RCode","Orig","Dest","DepFix","Route String","DCNTR","ACNTR","TCNTRs","CoordReq","Play","NavEqp","Length"` and `"Orig","Route String","Dest","Hours1","Type","Area","Altitude","Aircraft","Direction","Seq","DCNTR","ACNTR"`). The dictionary lookup is case-sensitive, so the documented names (RCODE, ORIG, DEPFIX, TCNTRS, COORDREQ, NAVEQP, ... HOURS1, SEQ ...) are not the real columns.

**Proposed:** use the exact header text in `_Src`: RCode, Orig, Dest, DepFix, Route String, TCNTRs, CoordReq, Play, NavEqp, Length; and Orig, Route String, Dest, Hours1, Type, Area, Altitude, Aircraft, Direction, Seq. Optionally add to the CDR/PFR class remarks: "CDR.csv and PFR_RMT_FMT.csv use mixed-case headers, unlike the other NASR files."

<a id="c8-18"></a>

### C8-18 · Minor · Low · Placeholder text left in XML docs

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/DpCsvDataModels.cs:69`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/DpCsvDataModels.cs#L69); [`Models/PjaCsvDataModels.cs:49`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/PjaCsvDataModels.cs#L49),159; [`Models/NavCsvDataModels.cs:91`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/NavCsvDataModels.cs#L91),700; [`Models/HpfCsvDataModels.cs:92`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/HpfCsvDataModels.cs#L92); [`Models/AptCsvDataModel.cs:1363`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/AptCsvDataModel.cs#L1363)

**Says:** `/// NoTitleYet`, `/// <remarks>NoRemarksYet</remarks>`, `/// <remarks></remarks>`

**Problem:** placeholders shown in IntelliSense as the summary/remarks.

**Proposed:** DpApt.AptBodySeq summary "Body Sequence Identifier"; PjaBase.NavType "NAVAID Type"; PjaBase.SiteTypeCode "Landing Facility Type Code"; delete the three NoRemarksYet remarks (NavBase.Name, NavBase.HiwasFlag, HpfBase.NavId) and the empty remarks on AptRwyEnd.RwyEndLongMin.

<a id="c8-15"></a>

### C8-15 · Wrong doc · Low · IlsBase.Owner summary says "Operator"

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/IlsCsvDataModels.cs:161`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/IlsCsvDataModels.cs#L161)

**Says:** `/// Operator Name and Code` ... `_Src: ILS_BASE.csv(OWNER)`

**Problem:** copied from the Operator property below (line 171); this is the owner.

**Proposed:** "Owner Name and Code".

<a id="c8-16"></a>

### C8-16 · Wrong doc · Low · NavBase: TACAN decimal-latitude remark says "Hemisphere"; PitchFlag remark garbled

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Models/NavCsvDataModels.cs:387`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/NavCsvDataModels.cs#L387), 662

**Says:** TacanDmeLatDecimal `<remarks>Latitude Hemisphere of TACAN Portion of VORTAC ...`; PitchFlag `<remarks>NoRemFairly obsolete system. Ref for more info: https://...AC_90-99.pdf arksYet</remarks>`

**Problem:** 387 copied from TacanDmeLatHemis; 662 is the AC 90-99 note pasted into the middle of the "NoRemarksYet" placeholder.

**Proposed:** 387 "Latitude in Decimal Format of TACAN Portion of VORTAC when TACAN is not sited with VOR"; 662 the same text as CatchFlag (line 672): "Fairly obsolete system. Ref for more info: https://www.faa.gov/documentLibrary/media/Advisory_Circular/AC_90-99.pdf".

## FeBuddy.Core/Infrastructure/Nasr/Models/{Arb

<a id="c8-4"></a>

### C8-4 · Wrong doc · Low · EffDate `_Src` says "All Apt_*.csv files" in 22 non-APT models

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Models/{Arb,Atc,Awos,Awy,ClsArsp,Com,Dp,Fix,Frq,Fss,Hpf,Ils,Lid,Maa,MilOps,Mtr,Nav,Pfr,Pja,Rdr,Star,Wxl}CsvDataModel(s).cs:14

**Says:** `/// _Src: All Apt_*.csv files(EFF_DATE)` (e.g. ArbCsvDataModels.cs:14)

**Problem:** copy-pasted from the APT model; each of these is the group's own files (ARB_BASE/ARB_SEG, ATC_*, ...). PFR is worse: PFR_RMT_FMT.csv has no EFF_DATE at all (see [C8-7](#c8-7)).

**Proposed:** in each file write the group's own prefix, e.g. `_Src: All ARB_*.csv files(EFF_DATE)`; for single-file groups name the file, e.g. `_Src: AWOS.csv(EFF_DATE)`; for PFR `_Src: PFR_BASE.csv and PFR_SEG.csv(EFF_DATE)`.

## FeBuddy.Core/Infrastructure/Nasr/Parsers

<a id="c8-22"></a>

### C8-22 · Minor · Low · NasrCsvParser: "own parser instance" comment implies a thread-safety need that doesn't exist; class could be static

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Parsers/NasrCsvParser.cs:33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Parsers/NasrCsvParser.cs#L33), 98-99; Parsers/*`CsvParser.cs` (24 classes)

**Says:** `public class NasrCsvParser` (only static members); `// Every file gets its own parser instance, so no parser is ever shared between threads.`

**Problem:** the 24 `XxxCsvParser` classes have no fields, so sharing one between threads would be safe; the comment gives a reason that isn't real. NasrCsvParser is never instantiated (grep "new NasrCsvParser" -> none) but isn't `static`, against Code-Structure.md:17 "Classes are mostly static".

**Proposed:** make NasrCsvParser `public static class`; change the comment to "Each group parses its files in parallel, then assembles the group once all are done." (drop the second sentence). Optionally make the 24 file parsers static too (they are created only in NasrCsvParser and NasrCsvParserMappingTests.cs:30-92).

<a id="c8-23"></a>

### C8-23 · Minor · Low · NasrCsvParser.ParseAsync/ParseAllAsync don't document what they throw

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Parsers/NasrCsvParser.cs:35-41`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Parsers/NasrCsvParser.cs#L35-L41), 43-52

**Says:** only `<exception cref="ArgumentOutOfRangeException">` on ParseAsync

**Problem:** ParseAsync also throws ArgumentNullException for null `groups` (line 54, tested in NasrCsvGroupParseTests.cs:52), and any file failure faults the whole call: FileNotFoundException/DirectoryNotFoundException for a missing file (tested, NasrCsvParserMappingTests.cs:193-198), KeyNotFoundException for a missing required column (`fields["X"]`), ArgumentNullException/FormatException from ParseInt/ParseDouble on a blank or bad required number. None is listed, and ParseAllAsync lists nothing.

**Proposed:** add to both: `<exception cref="IOException">A file of a requested group is missing or can't be read.</exception>`, `<exception cref="KeyNotFoundException">A file lacks a column its parser requires.</exception>`, `<exception cref="FormatException">A required number is not a number.</exception>`; ParseAsync also `<exception cref="ArgumentNullException"><paramref name="groups"/> is null.</exception>`.

<a id="c8-25"></a>

### C8-25 · Minor · Low · FYI: files parsed every cycle (their group is in InUse) that no feature reads

**Status:** Open · **Session:** [S7](020_Work-Plan.md#s7)

**Where:** [`Parsers/NasrCsvParser.cs:101-123`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/Parsers/NasrCsvParser.cs#L101-L123) (APT), 227-241 (FIX), 365-379 (NAV); [`Infrastructure/Nasr/NasrCsvGroups.cs:22-33`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Nasr/NasrCsvGroups.cs#L22-L33)

**Says:** `ParseAptAsync` parses all seven APT files whenever APT is in InUse

**Problem:** information only. Parsing is per group, so 7 of the 22 files in the 9 InUse groups are parsed for each of the 3 cycles but never read outside Nasr/ and tests: APT_ARS, APT_CON, APT_RMK (one of the largest NASR files), FIX_CHRT, FIX_NAV, NAV_CKPT, NAV_RMK. Read by features: APT_BASE/ATT/RWY/RWY_END, ARB_BASE/SEG, AWY_BASE/SEG_ALT, CLS_ARSP, DP_APT/BASE/RTE, FIX_BASE, FRQ, NAV_BASE, STAR_APT/BASE/RTE.

**Proposed:** none required (kept by the owner's choice). If launch parse time or memory matters later, ParseGroupAsync could take a file filter.

## Reported more than once

Each review slice was read by its own reviewer, so some findings were reported twice. These IDs point at the finding that is kept.

- <a id="c8-24"></a>**C8-24** · Code-Structure "A new NASR CSV group" list misses two places, one of which breaks every cycle parse · same as [D1b-2](500_Developer-Docs.md#d1b-2).
