# 010 · Decisions and open questions

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Answer on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374), quoting the ID (`D3`, `Q2`, `H1`). Kyle records the outcome here and in the [decision log](#decision-log), and changes the Status of the findings it affects.

## Summary

| ID | Topic | Status |
|---|---|---|
| [D1](#d1) | Remove code that only exists for 3.0 alpha/beta testers | Agreed, changed |
| [D2](#d2) | Delete `docs/Developers/Reference/AliasParser.cs` | Agreed, changed |
| [D3](#d3) | The duplicate-alias review keeps the wrong line by default | Agreed |
| [D4](#d4) | d-TPP timing: "approximately 20 days prior to" | Agreed, changed |
| [D5](#d5) | CRC `xOffset`/`yOffset` accept negative values | Agreed |
| [D6](#d6) | Keep the Harness, with a switch | Agreed, changed |
| [D7](#d7) | Leave the Departures/Arrivals near-copies for after 3.0 | Agreed |
| [Q1](#q1) | [W2-1](100_Bugs.md#w2-1): how to fix the General tab's save and undo | Answered |
| [Q2](#q2) | [C1-4](100_Bugs.md#c1-4): the AIRAC date moves during a session | Deferred |
| [Q3](#q3) | [C5-22](100_Bugs.md#c5-22): a wrong file picked for DAT/SCT conversion | Answered, deferred |
| [Q4](#q4) | [C6-29](100_Bugs.md#c6-29): `Shared.json` is never migrated | Answered |
| [Q5](#q5) | Test-only code: delete or keep? | Answered |
| [Q6](#q6) | GUI framework simplifications: before or after 3.0? | Answered, deferred |
| [Q7](#q7) | [D2-46](600_User-Docs-Proposal.md#d2-46): the General tab saves the cycle ID | Answered |
| [Q8](#q8) | [C2-11](200_Comments-Core.md#c2-11): does every airway in `AWY_SEG_ALT.csv` end with a row for its last point? | Answered |
| [Q9](#q9) | [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379): before 3.0 or after? | Answered |
| [Q10](#q10) | [D2](#d2): also purge `AliasParser.cs` from git history? | Answered |
| [H1](#h1) | Call the settings files by their real names | Agreed, changed |
| [H2](#h2) | No issue or feature numbers in comments | Agreed, changed |
| [H3](#h3) | No history narration in comments | Agreed |
| [H4](#h4) | Naming and layout | Agreed |
| [H5](#h5) | One settings node per page or tab | Agreed |

## Proposed decisions

Kyle's proposals from the review. Comment if you disagree.

<a id="d1"></a>

### D1 · Remove code that only exists for 3.0 alpha/beta testers

**Status:** Agreed, changed (Nikolas, 2026-10-17)

None of it matters to 2.x users upgrading to 3.0. What goes:

- **Settings:** the layout 1 → 2 migration ([C6-1](130_Old-Version-Code.md#c6-1)); moving beta.2/beta.3's single `UserConfig.json` into a profile ([C6-2](130_Old-Version-Code.md#c6-2), [C6-3](130_Old-Version-Code.md#c6-3)); the `IncludeRoiAirports` fallback ([C4-12](130_Old-Version-Code.md#c4-12))
- **Telephony:** the beta.4 single `vatsim_radar_airlines.json` ([C3-1](130_Old-Version-Code.md#c3-1))
- **Procedure Charts:** converting saved full procedure names to root names ([W1-18](130_Old-Version-Code.md#w1-18))
- **News:** the alpha.1 News stub `FeBuddy/FeBuddy.Core/News.md` ([C7-1](130_Old-Version-Code.md#c7-1))
- **Small leftovers:** the alpha crash file in `%TEMP%` ([C6-5](130_Old-Version-Code.md#c6-5)); a 32-bit registry fallback no MSI ever wrote ([O1-24](130_Old-Version-Code.md#o1-24)); an alpha.2 test ([T1b-6](130_Old-Version-Code.md#t1b-6))
- **Their tests:** [T1a-20](130_Old-Version-Code.md#t1a-20), [T2-1](130_Old-Version-Code.md#t2-1), [T2-2](130_Old-Version-Code.md#t2-2), [T2-6](130_Old-Version-Code.md#t2-6), [T2-7](130_Old-Version-Code.md#t2-7), [T1b-21](140_Unused-Code.md#t1b-21), [T3-29](130_Old-Version-Code.md#t3-29), [T3-30](130_Old-Version-Code.md#t3-30)

**What it costs:** a tester jumping straight from beta.2–beta.5 to 3.0 starts on default settings, but can still import their old file by hand. A beta.4-or-earlier tester loses one stale airline list.

**What stays:** the migration framework, for future layout changes ([C6-27](140_Unused-Code.md#c6-27)), and all 2.x upgrade code: the Squirrel uninstall, the dead shortcuts, the `FEBUDDY_GITHUB_TOKEN` notice and `REINSTALLMODE=amus` ([C1-16](130_Old-Version-Code.md#c1-16), [C1-17](130_Old-Version-Code.md#c1-17)). 3.0 is when 2.x users upgrade.

**Decision:** Agreed, except the News stub. Checked against the 2.x code (`development`, 2.9.3): 2.x never wrote a `UserConfig.json` (its settings were .NET user settings, which 3.0 doesn't read), and its MSI was always x64, so neither [C6-2](130_Old-Version-Code.md#c6-2) nor [O1-24](130_Old-Version-Code.md#o1-24) touches 2.x users. **Change:** keep the News stub ([C7-1](130_Old-Version-Code.md#c7-1)) through 3.0.0 and delete it in the first release after: alpha.1 only sees the update nudge once 3.0.0 stable is out. The 3.0 release notes should say that testers on an old beta start on defaults and can import their settings by hand.

<a id="d2"></a>

### D2 · Delete `docs/Developers/Reference/AliasParser.cs`

**Status:** Agreed, changed (Nikolas, 2026-10-17)

[D1b-1](500_Developer-Docs.md#d1b-1). It's CRC's own closed-source alias parser (`namespace Vatsim.Nas.Crc.CommandProcessing`), with no licence, in our public GPL-3 repository. Delete it, with `docs/Developers/Reference/README.md` and its row in `docs/README.md`. Whether to also purge it from git history is [Q10](#q10).

**Decision:** Delete it, in [S6](020_Work-Plan.md#s6). **Change:** the reason is that nothing needs it any more, not licensing: the CRC developer gave verbal permission to keep it in the public repository, for a feature we were working on in 2.x. Git history is left alone ([Q10](#q10)).

<a id="d3"></a>

### D3 · The duplicate-alias review keeps the wrong line by default

**Status:** Agreed (Nikolas, 2026-10-17); confirmed (Kyle, 2026-10-18)

[W2-12](220_Comments-GUI.md#w2-12). CRC reads an alias file top to bottom and the **last** duplicate wins (`Combined_Alias.txt` already relies on this). The review window defaults to keeping the *first* line and says that's "what CRC would have done anyway". It should default to the last, and its comment should say why.

**Decision:** Agreed. Per Kyle, the CRC developers said the last matching alias wins, which is why custom aliases are written at the bottom of the combined file. Kyle confirmed it.

<a id="d4"></a>

### D4 · d-TPP timing: "approximately 20 days prior to"

**Status:** Agreed, changed (Nikolas, 2026-10-17)

[C7-9](200_Comments-Core.md#c7-9). We say the FAA posts the Metafile "15–18 days" before a cycle; the FAA says 20. Change every place (code, the General tab tooltip, the run advisory, the docs) to "approximately 20 days prior to…", from one Core constant.

**Decision:** Correct the wording everywhere before 3.0: users see the wrong number today. **Change:** the single Core constant waits until after 3.0, together with a wider look for values hard-coded in more than one place ([After 3.0](020_Work-Plan.md#after-30)).

<a id="d5"></a>

### D5 · CRC `xOffset`/`yOffset` accept negative values

**Status:** Agreed (Nikolas, 2026-10-17)

[C5-12](200_Comments-Core.md#c5-12). Kyle tested it: negative values work in CRC. The code is right; only the comment needs fixing ("any integer, negative included").

**Decision:** Agreed. Comment only; `CRC_Geojsons.md` is outside this repository.

<a id="d6"></a>

### D6 · Keep the Harness, with a switch

**Status:** Agreed, changed (Nikolas, 2026-10-17)

[O1-4](400_Installer-Harness-CI.md#o1-4), [O1-5](400_Installer-Harness-CI.md#o1-5), [O1-16](400_Installer-Harness-CI.md#o1-16) and others. The Harness is for big one-off testing before a feature has a GUI. Replace its commented-out lines (most no longer compile) with a simple switch choosing which runs to do, and bring it up to the XML-doc standard.

**Decision:** Keep the Harness. **Change:** it is a development tool with no effect on any release, so all of its cleanup waits until after 3.0: the switch, the XML docs and the hard-coded machine paths ([O1-17](400_Installer-Harness-CI.md#o1-17)). Every finding in 400's `FeBuddy.Harness` section moves to [After 3.0](020_Work-Plan.md#after-30). When it's done, it should compile cleanly and run on any developer's machine.

<a id="d7"></a>

### D7 · Leave the Departures/Arrivals near-copies for after 3.0

**Status:** Agreed (Nikolas, 2026-10-17)

[C2-29](200_Comments-Core.md#c2-29), [C5-25](200_Comments-Core.md#c5-25). Don't refactor now; add a `TODO.md` item to share the code after 3.0.

**Decision:** Agreed. The code should not repeat itself, and the refactor can come after 3.0. The S7 brief only adds the TODO item ([S11](020_Work-Plan.md#s11)); it does not refactor.

## Open questions

Raised by the review. Input wanted.

<a id="q1"></a>

### Q1 · [W2-1](100_Bugs.md#w2-1): how to fix the General tab's save and undo

**Status:** Answered (Nikolas, 2026-10-17). Done (not yet merged): the General tab's node in [S1](020_Work-Plan.md#s1), the Settings page's keys in [S5](020_Work-Plan.md#s5)

- **A (proposed):** give the General tab a settings node of its own, plus a settings-layout step that moves its keys.
- **B:** make save and undo skip child nodes.

**Answer:** A, without the settings-layout step. Checked on `v3-B13-Dev`: the bug is real, and the General tab's **Undo last save** is enabled as soon as any AIRAC tab is saved, the duplicate-alias review is switched, or a default ROI is set. The General tab shouldn't own every AIRAC tab's settings. Move its keys to `Services.AiracService.General`; beta testers just get the defaults (everything included, every output on). Add a test that fails if any tab's settings node sits inside another tab's node ([H5](#h5)). Also move the Settings page's values (the Facility, the coordinate precision and the default ROI) out of `Services.AiracService` into the Settings page's own node ([S5](020_Work-Plan.md#s5)).

<a id="q2"></a>

### Q2 · [C1-4](100_Bugs.md#c1-4): the AIRAC date moves during a session

**Status:** Deferred (Nikolas, 2026-10-17)

The shell's re-check moves the AIRAC date, so after midnight on an effective date the GUI can name a cycle the cache never loaded.

- **A:** freeze the date for the session.
- **B:** re-prepare the cycles when the date moves.

**Answer:** After 3.0: the risk is low. Neither option as written. When FE-Buddy learns of a new cycle it should handle it without asking for a restart, which is closest to B. The approach needs discussing before anyone works on it.

<a id="q3"></a>

### Q3 · [C5-22](100_Bugs.md#c5-22): a wrong file picked for DAT/SCT conversion

**Status:** Answered, deferred (Nikolas, 2026-10-17)

Today it's only an advisory. Should it fail the file, as ERAM does?

**Answer:** Yes: a wrong file should fail, and loudly. It isn't urgent, so it can come after 3.0. Check a few real DAT and SCT files first, so that valid ones don't fail.

<a id="q4"></a>

### Q4 · [C6-29](100_Bugs.md#c6-29): `Shared.json` is never migrated

**Status:** Answered (Nikolas, 2026-10-17)

So a future settings-layout step can't rename a shared setting. Document the limit, or migrate `Shared.json` too?

**Answer:** Migrate it, before 3.0. `Shared.json` goes through the same settings-layout steps as the profile files. Add a test that renames a shared setting and checks it survives a restart. With D1 there are no real steps left, so the test uses one of its own.

<a id="q5"></a>

### Q5 · Test-only code: delete or keep?

**Status:** Answered (Nikolas, 2026-10-17)

Delete code only the tests use, or keep it where it's a deliberate test seam or a likely future use? Each finding has a proposal. Examples: launch progress types ([C1-6](140_Unused-Code.md#c1-6), proposed keep), the DMS half of `Location` ([C5-2](140_Unused-Code.md#c5-2)), the d-TPP and Wx row fields ([C7-15](140_Unused-Code.md#c7-15)), unused Markdown features ([C7-21](140_Unused-Code.md#c7-21), proposed keep), `Airway.AwyLocation`/`MaxAuthAlt` as future `feb.*` properties ([C2-26](140_Unused-Code.md#c2-26)).

**Answer:** Delete app code that nothing in the app calls and only its own tests use, together with those tests. Keep real test seams the tests need. CI fails if FeBuddy.Core drops below 95% line or 90% branch coverage (`ci.yml`), and deleting well-covered dead code can lower the percentage, so run coverage after each batch. [C5-2](140_Unused-Code.md#c5-2) needs fixing either way (its cost per `Location` and the lower-case hemisphere bug).

<a id="q6"></a>

### Q6 · GUI framework simplifications: before or after 3.0?

**Status:** Answered, deferred (Nikolas, 2026-10-17)

[W4-18](140_Unused-Code.md#w4-18), [W4-19](140_Unused-Code.md#w4-19), [W4-20](140_Unused-Code.md#w4-20): interfaces and base classes with a single user.

**Answer:** After 3.0. The GUI framework should be clean and easy to maintain, but these don't change any naming or conventions, so they can wait.

<a id="q7"></a>

### Q7 · [D2-46](600_User-Docs-Proposal.md#d2-46): the General tab saves the cycle ID

**Status:** Answered (Nikolas, 2026-10-17)

So last cycle's "Current" opens as "Previous" next cycle, and only Getting-Started warns about it. A user could re-run the old cycle without noticing.

- **A:** save the cycle *position* (Current), so it always opens on the cycle in effect.
- **B:** keep the ID and show an amber "this is the previous cycle" line.

**Answer:** A, before 3.0. Save the position the user picked (Current or Next), so they reopen on the same kind of cycle they last ran. Save it as a new key under the General tab's new node ([Q1](#q1)); saved cycle IDs are ignored and the user starts on Current, so no settings-layout step is needed. If the saved position isn't available (Next not published yet), open on Current and say why (see [W2-9](100_Bugs.md#w2-9)).

<a id="q8"></a>

### Q8 · [C2-11](200_Comments-Core.md#c2-11): does every airway in `AWY_SEG_ALT.csv` end with a row for its last point?

**Status:** Answered (Nikolas, 2026-10-17)

Someone with a real cycle on disk can check. The answer decides whether [C2-11](200_Comments-Core.md#c2-11) is a bug.

**Answer:** Yes, so it isn't a bug. Checked cycles 2609, 2610 and 2611 (about 1,550 airways and 19,500 rows each): every airway ends with a row whose `TO_POINT` is blank, no airway has a blank `TO_POINT` before its end, and every `TO_POINT` is also a `FROM_POINT` in the same airway. Use the replacement comment and make no code change. The only rows with a blank `FROM_PT_TYPE` are border points (`U.S. CANADIAN BORDER-1` and the like), which [C2-12](200_Comments-Core.md#c2-12) covers.

<a id="q9"></a>

### Q9 · [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379): before 3.0 or after?

**Status:** Answered (Nikolas, 2026-10-17). Done (not yet merged) in [S2](020_Work-Plan.md#s2), both ways: a watcher offers the restart, and a run waits for it

FE-Buddy left open in another Windows account keeps running the old version after an update. Low risk. See [020_Work-Plan.md](020_Work-Plan.md#related-issues).

**Answer:** Before 3.0, in [S2](020_Work-Plan.md#s2): it's a small change, and FE-Buddy is installed per machine, not per user. Either the version check when a run starts or a file watcher that waits for the update to finish works. A per-user install option is a separate discussion for after 3.0.

<a id="q10"></a>

### Q10 · [D2](#d2): also purge `AliasParser.cs` from git history?

**Status:** Answered (Nikolas, 2026-10-17)

Rewriting history removes it from old commits too, but every clone and fork has to be re-cloned, and old links to commits break.

**Answer:** No. Do not rewrite git history. We had permission to keep the file in the public repository, so there is no reason to. A rewrite would also break every clone, every fork and every permalink in this review: `AliasParser.cs` was added before `dc08390`.

## Proposed house rules

Patterns the review kept finding. Agreeing on a rule stops them coming back. Session [S11](020_Work-Plan.md#s11) writes the agreed ones into `Code-Structure.md`.

<a id="h1"></a>

### H1 · Call the settings files by their real names

**Status:** Done (not yet merged): in `Code-Structure.md`'s "Rules for Core and the app", for code and the developer docs. Agreed, changed (Nikolas, 2026-10-17)

`UserConfig.json` is still named in about 10 comments, including one the user sees ([D1b-24](220_Comments-GUI.md#d1b-24), [W5-2](610_On-Screen-Text.md#w5-2)). It's `UserConfig.<Profile>.json` and `Shared.json` now.

**Decision:** Agreed. Nothing may point at a file that no longer exists. **Change:** code comments and developer docs use the real names (`UserConfig.<Profile>.json`, `Shared.json`); GUI text says "your settings profile" rather than a file name. Whichever wording is used, use it the same way everywhere. The findings cover more than 20 places, not about 10.

<a id="h2"></a>

### H2 · No issue or feature numbers in comments

**Status:** Done (not yet merged): in `Code-Structure.md`'s "Rules for Core and the app", for code and the developer docs except `ChangeLog.md` and `RELEASING*.md`. Agreed, changed (Nikolas, 2026-10-17)

[#318](https://github.com/Nikolai558/FE-BUDDY/issues/318), [#335](https://github.com/Nikolai558/FE-BUDDY/issues/335), [#364](https://github.com/Nikolai558/FE-BUDDY/issues/364), [#367](https://github.com/Nikolai558/FE-BUDDY/issues/367), [#371](https://github.com/Nikolai558/FE-BUDDY/issues/371) and others appear in dozens of places. `Code-Structure.md` already says no task numbers. Keep only bug references that explain a choice. A repository test could enforce it.

**Decision:** Agreed. **Change:** stricter than proposed: no issue, pull-request or feature numbers in code comments, bug references included. That history belongs in commits and pull requests. Where code needs a comment because of a bug, the comment explains the reason in words, and the bug's regression test documents it. Findings that propose keeping a bug number (for example #241, #299, #151) drop the number too.

<a id="h3"></a>

### H3 · No history narration in comments

**Status:** Done (not yet merged): in `Code-Structure.md`'s "Rules for Core and the app", for code and the developer docs except `ChangeLog.md` and `RELEASING*.md`. Agreed (Nikolas, 2026-10-17)

"as in 2.x", "beta.N and earlier", "now", "still", "the old …". Keep it only where it explains a choice made because of a bug we found.

**Decision:** Agreed. Comments don't narrate what the code used to do or how 3.x differs from 2.x. If a bug keeps coming back, its test explains why. Comments about a quirk outside our code are fine, such as "the FAA has changed this format before" or a CRC or WPF behaviour we have to work around.

<a id="h4"></a>

### H4 · Naming and layout

**Status:** Done (not yet merged): in `Code-Structure.md`'s "Rules for Core and the app" and "Naming by role"; the renames themselves are in [S6](020_Work-Plan.md#s6). Agreed (Nikolas, 2026-10-17)

`*Service` only for entry points; data types in `Models/`; one type per file.

**Decision:** Agreed, and the renames happen before 3.0: changing conventions after the release would leave two standards in a public repository. They go after the code moves (S3 to S5) and before the comment sessions (S7, S8): see [S6](020_Work-Plan.md#s6).

<a id="h5"></a>

### H5 · One settings node per page or tab

**Status:** Done (not yet merged): in `Code-Structure.md`'s "Rules for Core and the app", pointed to from `Settings-Reference.md`'s "Adding a setting", and checked by `UserConfigLayoutTests.no_settings_node_sits_inside_another`. Agreed (Nikolas, 2026-10-17)

Raised by [Q1](#q1). The General tab's node (`Services.AiracService`) is the parent of every other AIRAC tab's node, which is what caused [W2-1](100_Bugs.md#w2-1). The Settings page also writes the Facility, the coordinate precision and the default ROI inside that node.

**Rule:** every page or tab owns exactly one settings node, and no node sits inside another page's or tab's node. A repository test checks it. A page that needs another page's setting reads it, or calls that page's code to change it; it doesn't write that node itself.

## Decision log

| Date | ID | Outcome | Who |
|---|---|---|---|
| 2026-10-14 | D1-D7 | Proposed after the review | Kyle |
| 2026-10-14 | [C1-5](100_Bugs.md#c1-5) | Fixed: setup reopens FE-Buddy as the signed-in user ([#377](https://github.com/Nikolai558/FE-BUDDY/pull/377)) | Nikolas |
| 2026-10-16 | [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) | Opened: a declined UAC prompt reports success. Comes from the C1-5 fix; added to [S1](020_Work-Plan.md#s1) | Nikolas |
| 2026-10-16 | [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379) | Opened: FE-Buddy left open in another account keeps running the old version. Timing is [Q9](#q9) | Nikolas |
| 2026-10-17 | [D1](#d1) | Agreed, changed: keep the News stub (C7-1) through 3.0.0 | Nikolas |
| 2026-10-17 | [D2](#d2) | Agreed, changed: delete because it's no longer needed; permission was given, history left alone | Nikolas |
| 2026-10-17 | [D3](#d3) | Agreed; Kyle to confirm CRC's last-wins rule | Nikolas |
| 2026-10-17 | [D4](#d4) | Agreed, changed: wording before 3.0, single constant after | Nikolas |
| 2026-10-17 | [D5](#d5) | Agreed | Nikolas |
| 2026-10-17 | [D6](#d6) | Agreed, changed: keep the Harness; all its cleanup after 3.0 | Nikolas |
| 2026-10-17 | [D7](#d7) | Agreed | Nikolas |
| 2026-10-17 | [Q1](#q1) | A, without the settings-layout step; Settings-page keys move too; new rule [H5](#h5) | Nikolas |
| 2026-10-17 | [Q2](#q2) | After 3.0; handle a new cycle without a restart | Nikolas |
| 2026-10-17 | [Q3](#q3) | Fail the file; after 3.0 | Nikolas |
| 2026-10-17 | [Q4](#q4) | Migrate `Shared.json` too, before 3.0 | Nikolas |
| 2026-10-17 | [Q5](#q5) | Delete test-only code, keep test seams, watch the coverage floor | Nikolas |
| 2026-10-17 | [Q6](#q6) | After 3.0 | Nikolas |
| 2026-10-17 | [Q7](#q7) | A: save the cycle position, before 3.0 | Nikolas |
| 2026-10-17 | [Q8](#q8) | Yes; C2-11 is a comment fix, not a bug | Nikolas |
| 2026-10-17 | [Q9](#q9) | Before 3.0, in S2 | Nikolas |
| 2026-10-17 | [Q10](#q10) | No history rewrite | Nikolas |
| 2026-10-17 | [H1](#h1) | Agreed, changed: real names in code and docs, "your settings profile" in the GUI | Nikolas |
| 2026-10-17 | [H2](#h2) | Agreed, changed: no numbers in comments at all, bug references included | Nikolas |
| 2026-10-17 | [H3](#h3) | Agreed | Nikolas |
| 2026-10-17 | [H4](#h4) | Agreed; renames before 3.0, in S6 | Nikolas |
| 2026-10-17 | [H5](#h5) | New rule from Q1; agreed | Nikolas |
| 2026-10-18 | [C1-3](100_Bugs.md#c1-3) | Keep checking the previous cycle's route for deleted and changed procedures; fix the failure instead | Kyle |
| 2026-10-18 | [D3](#d3) | Confirmed: CRC reads an alias file top to bottom, and the last duplicate is the one it uses | Kyle |
| 2026-10-18 | [W1-13](100_Bugs.md#w1-13) | Agreed: a blank `JsonFields` means no optional fields | Kyle |
| 2026-10-18 | H1-H3 | Apply to the developer docs too, except `ChangeLog.md` and `RELEASING*.md` | Kyle |
| 2026-10-18 | H1-H5 | Written into `Code-Structure.md` | Kyle |
| 2026-10-18 | [D1b-11](500_Developer-Docs.md#d1b-11) | Once 3.0.0 ships, every earlier version is unsupported | Kyle |
| 2026-10-18 | [D1b-4](500_Developer-Docs.md#d1b-4), [D1a-11](500_Developer-Docs.md#d1a-11) | Their doc lines go with the code in S6 | Kyle |
