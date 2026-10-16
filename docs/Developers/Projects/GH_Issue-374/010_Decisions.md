# 010 · Decisions and open questions

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Answer on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374), quoting the ID (`D3`, `Q2`, `H1`). Kyle records the outcome here and in the [decision log](#decision-log), and changes the Status of the findings it affects.

## Summary

| ID | Topic | Status |
|---|---|---|
| [D1](#d1) | Remove code that only exists for 3.0 alpha/beta testers | Proposed by Kyle |
| [D2](#d2) | Delete `docs/Developers/Reference/AliasParser.cs` | Proposed by Kyle |
| [D3](#d3) | The duplicate-alias review keeps the wrong line by default | Proposed by Kyle |
| [D4](#d4) | d-TPP timing: "approximately 20 days prior to" | Proposed by Kyle |
| [D5](#d5) | CRC `xOffset`/`yOffset` accept negative values | Proposed by Kyle |
| [D6](#d6) | Keep the Harness, with a switch | Proposed by Kyle |
| [D7](#d7) | Leave the Departures/Arrivals near-copies for after 3.0 | Proposed by Kyle |
| [Q1](#q1) | [W2-1](100_Bugs.md#w2-1): how to fix the General tab's save and undo | Open |
| [Q2](#q2) | [C1-4](100_Bugs.md#c1-4): the AIRAC date moves during a session | Open |
| [Q3](#q3) | [C5-22](100_Bugs.md#c5-22): a wrong file picked for DAT/SCT conversion | Open |
| [Q4](#q4) | [C6-29](100_Bugs.md#c6-29): `Shared.json` is never migrated | Open |
| [Q5](#q5) | Test-only code: delete or keep? | Open |
| [Q6](#q6) | GUI framework simplifications: before or after 3.0? | Open |
| [Q7](#q7) | [D2-46](600_User-Docs-Proposal.md#d2-46): the General tab saves the cycle ID | Open |
| [Q8](#q8) | [C2-11](200_Comments-Core.md#c2-11): does every airway in `AWY_SEG_ALT.csv` end with a row for its last point? | Open |
| [Q9](#q9) | [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379): before 3.0 or after? | Open |
| [Q10](#q10) | [D2](#d2): also purge `AliasParser.cs` from git history? | Open |
| [H1](#h1) | Call the settings files by their real names | Proposed |
| [H2](#h2) | No issue or feature numbers in comments | Proposed |
| [H3](#h3) | No history narration in comments | Proposed |
| [H4](#h4) | Naming and layout | Proposed |

## Proposed decisions

Kyle's proposals from the review. Comment if you disagree.

<a id="d1"></a>

### D1 · Remove code that only exists for 3.0 alpha/beta testers

**Status:** Proposed by Kyle

None of it matters to 2.x users upgrading to 3.0. What goes:

- **Settings:** the layout 1 → 2 migration ([C6-1](130_Old-Version-Code.md#c6-1)); moving beta.2/beta.3's single `UserConfig.json` into a profile ([C6-2](130_Old-Version-Code.md#c6-2), [C6-3](130_Old-Version-Code.md#c6-3)); the `IncludeRoiAirports` fallback ([C4-12](130_Old-Version-Code.md#c4-12))
- **Telephony:** the beta.4 single `vatsim_radar_airlines.json` ([C3-1](130_Old-Version-Code.md#c3-1))
- **Procedure Charts:** converting saved full procedure names to root names ([W1-18](130_Old-Version-Code.md#w1-18))
- **News:** the alpha.1 News stub `FeBuddy/FeBuddy.Core/News.md` ([C7-1](130_Old-Version-Code.md#c7-1))
- **Small leftovers:** the alpha crash file in `%TEMP%` ([C6-5](130_Old-Version-Code.md#c6-5)); a 32-bit registry fallback no MSI ever wrote ([O1-24](130_Old-Version-Code.md#o1-24)); an alpha.2 test ([T1b-6](130_Old-Version-Code.md#t1b-6))
- **Their tests:** [T1a-20](130_Old-Version-Code.md#t1a-20), [T2-1](130_Old-Version-Code.md#t2-1), [T2-2](130_Old-Version-Code.md#t2-2), [T2-6](130_Old-Version-Code.md#t2-6), [T2-7](130_Old-Version-Code.md#t2-7), [T1b-21](140_Unused-Code.md#t1b-21), [T3-29](130_Old-Version-Code.md#t3-29), [T3-30](130_Old-Version-Code.md#t3-30)

**What it costs:** a tester jumping straight from beta.2–beta.5 to 3.0 starts on default settings, but can still import their old file by hand. A beta.4-or-earlier tester loses one stale airline list.

**What stays:** the migration framework, for future layout changes ([C6-27](140_Unused-Code.md#c6-27)), and all 2.x upgrade code: the Squirrel uninstall, the dead shortcuts, the `FEBUDDY_GITHUB_TOKEN` notice and `REINSTALLMODE=amus` ([C1-16](130_Old-Version-Code.md#c1-16), [C1-17](130_Old-Version-Code.md#c1-17)). 3.0 is when 2.x users upgrade.

<a id="d2"></a>

### D2 · Delete `docs/Developers/Reference/AliasParser.cs`

**Status:** Proposed by Kyle

[D1b-1](500_Developer-Docs.md#d1b-1). It's CRC's own closed-source alias parser (`namespace Vatsim.Nas.Crc.CommandProcessing`), with no licence, in our public GPL-3 repository. Delete it, with `docs/Developers/Reference/README.md` and its row in `docs/README.md`. Whether to also purge it from git history is [Q10](#q10).

<a id="d3"></a>

### D3 · The duplicate-alias review keeps the wrong line by default

**Status:** Proposed by Kyle

[W2-12](220_Comments-GUI.md#w2-12). CRC reads an alias file top to bottom and the **last** duplicate wins (`Combined_Alias.txt` already relies on this). The review window defaults to keeping the *first* line and says that's "what CRC would have done anyway". It should default to the last, and its comment should say why.

<a id="d4"></a>

### D4 · d-TPP timing: "approximately 20 days prior to"

**Status:** Proposed by Kyle

[C7-9](200_Comments-Core.md#c7-9). We say the FAA posts the Metafile "15–18 days" before a cycle; the FAA says 20. Change every place (code, the General tab tooltip, the run advisory, the docs) to "approximately 20 days prior to…", from one Core constant.

<a id="d5"></a>

### D5 · CRC `xOffset`/`yOffset` accept negative values

**Status:** Proposed by Kyle

[C5-12](200_Comments-Core.md#c5-12). Kyle tested it: negative values work in CRC. The code is right; only the comment needs fixing ("any integer, negative included").

<a id="d6"></a>

### D6 · Keep the Harness, with a switch

**Status:** Proposed by Kyle

[O1-4](400_Installer-Harness-CI.md#o1-4), [O1-5](400_Installer-Harness-CI.md#o1-5), [O1-16](400_Installer-Harness-CI.md#o1-16) and others. The Harness is for big one-off testing before a feature has a GUI. Replace its commented-out lines (most no longer compile) with a simple switch choosing which runs to do, and bring it up to the XML-doc standard.

<a id="d7"></a>

### D7 · Leave the Departures/Arrivals near-copies for after 3.0

**Status:** Proposed by Kyle

[C2-29](200_Comments-Core.md#c2-29), [C5-25](200_Comments-Core.md#c5-25). Don't refactor now; add a `TODO.md` item to share the code after 3.0.

## Open questions

Raised by the review. Input wanted.

<a id="q1"></a>

### Q1 · [W2-1](100_Bugs.md#w2-1): how to fix the General tab's save and undo

**Status:** Open

- **A (proposed):** give the General tab a settings node of its own, plus a settings-layout step that moves its keys.
- **B:** make save and undo skip child nodes.

**Answer:** -

<a id="q2"></a>

### Q2 · [C1-4](100_Bugs.md#c1-4): the AIRAC date moves during a session

**Status:** Open

The shell's re-check moves the AIRAC date, so after midnight on an effective date the GUI can name a cycle the cache never loaded.

- **A:** freeze the date for the session.
- **B:** re-prepare the cycles when the date moves.

**Answer:** -

<a id="q3"></a>

### Q3 · [C5-22](100_Bugs.md#c5-22): a wrong file picked for DAT/SCT conversion

**Status:** Open

Today it's only an advisory. Should it fail the file, as ERAM does?

**Answer:** -

<a id="q4"></a>

### Q4 · [C6-29](100_Bugs.md#c6-29): `Shared.json` is never migrated

**Status:** Open

So a future settings-layout step can't rename a shared setting. Document the limit, or migrate `Shared.json` too?

**Answer:** -

<a id="q5"></a>

### Q5 · Test-only code: delete or keep?

**Status:** Open

Delete code only the tests use, or keep it where it's a deliberate test seam or a likely future use? Each finding has a proposal. Examples: launch progress types ([C1-6](140_Unused-Code.md#c1-6), proposed keep), the DMS half of `Location` ([C5-2](140_Unused-Code.md#c5-2)), the d-TPP and Wx row fields ([C7-15](140_Unused-Code.md#c7-15)), unused Markdown features ([C7-21](140_Unused-Code.md#c7-21), proposed keep), `Airway.AwyLocation`/`MaxAuthAlt` as future `feb.*` properties ([C2-26](140_Unused-Code.md#c2-26)).

**Answer:** -

<a id="q6"></a>

### Q6 · GUI framework simplifications: before or after 3.0?

**Status:** Open

[W4-18](140_Unused-Code.md#w4-18), [W4-19](140_Unused-Code.md#w4-19), [W4-20](140_Unused-Code.md#w4-20): interfaces and base classes with a single user.

**Answer:** -

<a id="q7"></a>

### Q7 · [D2-46](600_User-Docs-Proposal.md#d2-46): the General tab saves the cycle ID

**Status:** Open

So last cycle's "Current" opens as "Previous" next cycle, and only Getting-Started warns about it. A user could re-run the old cycle without noticing.

- **A:** save the cycle *position* (Current), so it always opens on the cycle in effect.
- **B:** keep the ID and show an amber "this is the previous cycle" line.

**Answer:** -

<a id="q8"></a>

### Q8 · [C2-11](200_Comments-Core.md#c2-11): does every airway in `AWY_SEG_ALT.csv` end with a row for its last point?

**Status:** Open

Someone with a real cycle on disk can check. The answer decides whether [C2-11](200_Comments-Core.md#c2-11) is a bug.

**Answer:** -

<a id="q9"></a>

### Q9 · [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379): before 3.0 or after?

**Status:** Open

FE-Buddy left open in another Windows account keeps running the old version after an update. Low risk. See [020_Work-Plan.md](020_Work-Plan.md#related-issues).

**Answer:** -

<a id="q10"></a>

### Q10 · [D2](#d2): also purge `AliasParser.cs` from git history?

**Status:** Open

Rewriting history removes it from old commits too, but every clone and fork has to be re-cloned, and old links to commits break.

**Answer:** -

## Proposed house rules

Patterns the review kept finding. Agreeing on a rule stops them coming back. Session [S11](020_Work-Plan.md#s11) writes the agreed ones into `Code-Structure.md`.

<a id="h1"></a>

### H1 · Call the settings files by their real names

**Status:** Proposed

`UserConfig.json` is still named in about 10 comments, including one the user sees ([D1b-24](220_Comments-GUI.md#d1b-24), [W5-2](610_On-Screen-Text.md#w5-2)). It's `UserConfig.<Profile>.json` and `Shared.json` now.

<a id="h2"></a>

### H2 · No issue or feature numbers in comments

**Status:** Proposed

[#318](https://github.com/Nikolai558/FE-BUDDY/issues/318), [#335](https://github.com/Nikolai558/FE-BUDDY/issues/335), [#364](https://github.com/Nikolai558/FE-BUDDY/issues/364), [#367](https://github.com/Nikolai558/FE-BUDDY/issues/367), [#371](https://github.com/Nikolai558/FE-BUDDY/issues/371) and others appear in dozens of places. `Code-Structure.md` already says no task numbers. Keep only bug references that explain a choice. A repository test could enforce it.

<a id="h3"></a>

### H3 · No history narration in comments

**Status:** Proposed

"as in 2.x", "beta.N and earlier", "now", "still", "the old …". Keep it only where it explains a choice made because of a bug we found.

<a id="h4"></a>

### H4 · Naming and layout

**Status:** Proposed

`*Service` only for entry points; data types in `Models/`; one type per file.

## Decision log

| Date | ID | Outcome | Who |
|---|---|---|---|
| 2026-10-14 | D1-D7 | Proposed after the review | Kyle |
| 2026-10-14 | [C1-5](100_Bugs.md#c1-5) | Fixed: setup reopens FE-Buddy as the signed-in user ([#377](https://github.com/Nikolai558/FE-BUDDY/pull/377)) | Nikolas |
| 2026-10-16 | [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) | Opened: a declined UAC prompt reports success. Comes from the C1-5 fix; added to [S1](020_Work-Plan.md#s1) | Nikolas |
| 2026-10-16 | [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379) | Opened: FE-Buddy left open in another account keeps running the old version. Timing is [Q9](#q9) | Nikolas |
