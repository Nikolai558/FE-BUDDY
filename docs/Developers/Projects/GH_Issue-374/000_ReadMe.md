# GH Issue #374 · 3.0 pre-release review

This folder is the working record for [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374): a review of the whole repository before the public 3.0 release. The issue is where we talk and track progress. These files hold the detail, which is too big for GitHub comments.

**New here?** Read this page, then [010_Decisions.md](010_Decisions.md), then skim the Summary table at the top of whichever files you care about.

`docs/Developers/Projects/` holds one folder like this per big project, named `GH_Issue-<number>` after its GitHub issue, each starting with a `000_ReadMe.md`.

## Contents

1. [What the review covers](#what-the-review-covers)
2. [Where we are](#where-we-are)
3. [The files](#the-files)
4. [How to take part](#how-to-take-part)
5. [How to read a finding](#how-to-read-a-finding)
6. [Which code the review read](#which-code-the-review-read)
7. [Keeping these files up to date](#keeping-these-files-up-to-date)
8. [When the project is done](#when-the-project-is-done)

## What the review covers

Four things, across every project, test, script and doc:

1. **Accuracy.** Every code comment, XML doc and doc, checked against what the code actually does.
2. **Unused code.** Old code nothing uses and that we're unlikely to need, and code that only exists for older FE-Buddy versions. The NASR models and parsers stay even where unused: we'll use them later.
3. **History wording.** Comments that explain code by what older code did ("used to", "2.x did", "beta.4 and earlier", issue numbers). The exception is a note explaining why we do something a certain way because of a bug we found.
4. **GUI lag.** Work the GUI does on the UI thread, and business rules living in the GUI that belong in Core.

And the user manuals: they should be very short. Anything that isn't common sense belongs in the GUI, so nobody needs to read a manual.

**The ground rules behind it** (Kyle):

- The GUI must never lag because it is processing something. Processing belongs in FeBuddy.Core, off the UI thread.
- User manuals are short; anything that isn't common sense is explained in the GUI.
- The NASR models and parsers stay, even where unused.

## Where we are

As of 2026-10-19: the review is done and Nikolas has answered every decision and question in [010_Decisions.md](010_Decisions.md). **139 of 704 findings fixed** so far; 22 are deferred to [after 3.0](020_Work-Plan.md#after-30). The 704 include 39 found on 2026-10-19 in a review of the code [S2](020_Work-Plan.md#s2) to [S5](020_Work-Plan.md#s5) added (N1-5 to N1-43, session [S14](020_Work-Plan.md#s14)). Nikolas stepped away on 2026-10-19; Kyle has the rest, the installer included.

| File | Findings | High | Med | Low | Done | Session |
|---|---:|---:|---:|---:|---:|---|
| [100_Bugs.md](100_Bugs.md) | 71 | 2 | 16 | 53 | 49 | [S1](020_Work-Plan.md#s1), [S2](020_Work-Plan.md#s2), [S14](020_Work-Plan.md#s14) |
| [110_GUI-Lag.md](110_GUI-Lag.md) | 33 | 2 | 10 | 21 | 30 | [S3](020_Work-Plan.md#s3), [S4](020_Work-Plan.md#s4), [S14](020_Work-Plan.md#s14) |
| [120_Logic-In-GUI.md](120_Logic-In-GUI.md) | 25 |  | 3 | 22 | 24 | [S5](020_Work-Plan.md#s5), [S14](020_Work-Plan.md#s14) |
| [130_Old-Version-Code.md](130_Old-Version-Code.md) | 18 |  | 5 | 13 |  | [S6](020_Work-Plan.md#s6) |
| [140_Unused-Code.md](140_Unused-Code.md) | 78 |  | 3 | 75 |  | [S6](020_Work-Plan.md#s6) |
| [200_Comments-Core.md](200_Comments-Core.md) | 142 |  | 8 | 134 | 2 | [S7](020_Work-Plan.md#s7), [S14](020_Work-Plan.md#s14) |
| [210_Comments-NASR.md](210_Comments-NASR.md) | 25 |  | 3 | 22 |  | [S7](020_Work-Plan.md#s7) |
| [220_Comments-GUI.md](220_Comments-GUI.md) | 98 |  | 2 | 96 | 1 | [S8](020_Work-Plan.md#s8), [S14](020_Work-Plan.md#s14) |
| [300_Unit-Tests.md](300_Unit-Tests.md) | 106 |  | 7 | 99 | 1 | [S9](020_Work-Plan.md#s9), [S14](020_Work-Plan.md#s14) |
| [400_Installer-Harness-CI.md](400_Installer-Harness-CI.md) | 55 |  | 1 | 54 |  | [S10](020_Work-Plan.md#s10) |
| [500_Developer-Docs.md](500_Developer-Docs.md) | 37 | 1 | 4 | 32 | 32 | [S11](020_Work-Plan.md#s11), [S14](020_Work-Plan.md#s14) |
| [600_User-Docs-Proposal.md](600_User-Docs-Proposal.md) | 69 sections | | | | | [S12](020_Work-Plan.md#s12) |
| [610_On-Screen-Text.md](610_On-Screen-Text.md) | 16 |  | 1 | 15 |  | [S12](020_Work-Plan.md#s12) |
| **Total** | **704** | **5** | **63** | **636** | **139** | |

**Done so far:** [C1-5](100_Bugs.md#c1-5), fixed by Nikolas ([#377](https://github.com/Nikolai558/FE-BUDDY/pull/377)). Fixed, not yet merged: [U1-1](110_GUI-Lag.md#u1-1), [W1-1](110_GUI-Lag.md#w1-1), [U1-2](110_GUI-Lag.md#u1-2), [W1-3](110_GUI-Lag.md#w1-3), [U1-14](110_GUI-Lag.md#u1-14), [U1-6](110_GUI-Lag.md#u1-6), [W4-7](100_Bugs.md#w4-7), [C6-30](110_GUI-Lag.md#c6-30), [U1-7](110_GUI-Lag.md#u1-7), [W4-5](110_GUI-Lag.md#w4-5), [W2-6](110_GUI-Lag.md#w2-6), [W2-27](110_GUI-Lag.md#w2-27), [W3-7](110_GUI-Lag.md#w3-7), [W2-5](100_Bugs.md#w2-5), [W1-5](110_GUI-Lag.md#w1-5), [W4-4](110_GUI-Lag.md#w4-4), [U1-15](110_GUI-Lag.md#u1-15), [W6-6](110_GUI-Lag.md#w6-6), [W1-14](100_Bugs.md#w1-14), [W2-10](100_Bugs.md#w2-10), [W2-8](100_Bugs.md#w2-8), [W2-9](100_Bugs.md#w2-9), [W1-15](100_Bugs.md#w1-15), [U1-17](120_Logic-In-GUI.md#u1-17), [C7-4](110_GUI-Lag.md#c7-4), [W7-1](110_GUI-Lag.md#w7-1), [U1-13](110_GUI-Lag.md#u1-13), [W3-1](110_GUI-Lag.md#w3-1), [W3-3](110_GUI-Lag.md#w3-3), [W3-4](110_GUI-Lag.md#w3-4), [C6-31](110_GUI-Lag.md#c6-31), [T3-6](110_GUI-Lag.md#t3-6), [W7-2](110_GUI-Lag.md#w7-2), [W7-7](110_GUI-Lag.md#w7-7), [U1-9](110_GUI-Lag.md#u1-9), [W3-5](110_GUI-Lag.md#w3-5), [W3-8](110_GUI-Lag.md#w3-8), [W2-19](120_Logic-In-GUI.md#w2-19), [W2-20](120_Logic-In-GUI.md#w2-20), [W1-6](120_Logic-In-GUI.md#w1-6), [W1-12](120_Logic-In-GUI.md#w1-12), [W2-18](120_Logic-In-GUI.md#w2-18), [W1-9](120_Logic-In-GUI.md#w1-9), [W1-8](120_Logic-In-GUI.md#w1-8), [W4-6](120_Logic-In-GUI.md#w4-6), [W2-7](120_Logic-In-GUI.md#w2-7), [W2-22](120_Logic-In-GUI.md#w2-22), [W1-11](120_Logic-In-GUI.md#w1-11), [W2-23](120_Logic-In-GUI.md#w2-23), [W6-8](120_Logic-In-GUI.md#w6-8), [W1-10](120_Logic-In-GUI.md#w1-10), [W7-6](120_Logic-In-GUI.md#w7-6), [W3-12](120_Logic-In-GUI.md#w3-12), [W2-21](120_Logic-In-GUI.md#w2-21), [W2-25](120_Logic-In-GUI.md#w2-25), [W3-14](120_Logic-In-GUI.md#w3-14), [W3-13](120_Logic-In-GUI.md#w3-13), [W3-10](120_Logic-In-GUI.md#w3-10), [W3-9](120_Logic-In-GUI.md#w3-9), [C2-6](120_Logic-In-GUI.md#c2-6), [W6-3](100_Bugs.md#w6-3), [W1-7](100_Bugs.md#w1-7), [W2-37](100_Bugs.md#w2-37), [W3-16](100_Bugs.md#w3-16), [W4-8](100_Bugs.md#w4-8), [W5-3](100_Bugs.md#w5-3), [W7-22](100_Bugs.md#w7-22), [O1-23](100_Bugs.md#o1-23), [O1-30](100_Bugs.md#o1-30), [C6-13](200_Comments-Core.md#c6-13), [D1a-3](500_Developer-Docs.md#d1a-3), [D2-46](600_User-Docs-Proposal.md#d2-46) ([Q7](010_Decisions.md#q7)), [W2-12](220_Comments-GUI.md#w2-12), [W2-1](100_Bugs.md#w2-1), [N1-4](100_Bugs.md#n1-4), [W7-3](100_Bugs.md#w7-3), [W2-11](100_Bugs.md#w2-11), [W7-4](100_Bugs.md#w7-4), [C3-18](100_Bugs.md#c3-18), [U1-3](100_Bugs.md#u1-3), [C1-1](100_Bugs.md#c1-1), [C5-1](100_Bugs.md#c5-1), [W1-13](100_Bugs.md#w1-13), [O1-1](100_Bugs.md#o1-1), [W3-15](100_Bugs.md#w3-15), [C1-2](100_Bugs.md#c1-2), [C1-3](100_Bugs.md#c1-3), [C4-11](100_Bugs.md#c4-11), [C2-3](100_Bugs.md#c2-3), [C3-16](100_Bugs.md#c3-16), [C5-8](100_Bugs.md#c5-8), [C5-23](100_Bugs.md#c5-23), [C6-23](100_Bugs.md#c6-23), [C6-29](100_Bugs.md#c6-29), [C7-10](100_Bugs.md#c7-10), [O1-2](100_Bugs.md#o1-2), [O1-42](100_Bugs.md#o1-42), [T1c-26](100_Bugs.md#t1c-26), [T2-8](100_Bugs.md#t2-8), [T2-9](100_Bugs.md#t2-9), [D1b-2](500_Developer-Docs.md#d1b-2), [D1b-3](500_Developer-Docs.md#d1b-3), [D1b-9](500_Developer-Docs.md#d1b-9), [W5-22](500_Developer-Docs.md#w5-22), [W7-26](500_Developer-Docs.md#w7-26), [D1b-16](500_Developer-Docs.md#d1b-16), and the other 24 S11 findings listed in [S11](020_Work-Plan.md#s11); in S14, [N1-5](100_Bugs.md#n1-5), [N1-10](100_Bugs.md#n1-10), [N1-11](100_Bugs.md#n1-11), [N1-12](100_Bugs.md#n1-12), [N1-13](100_Bugs.md#n1-13), [N1-29](110_GUI-Lag.md#n1-29), [N1-35](200_Comments-Core.md#n1-35), [N1-39](300_Unit-Tests.md#n1-39), [N1-43](500_Developer-Docs.md#n1-43). The house rules [H1 to H5](010_Decisions.md#proposed-house-rules) are written into `Code-Structure.md`.

**Since the review:** Nikolas opened [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) (a side effect of the [C1-5](100_Bugs.md#c1-5) fix; fixed in [S1](020_Work-Plan.md#s1) and tested on a VM, not yet merged) and [#379](https://github.com/Nikolai558/FE-BUDDY/issues/379) (low risk; fixed in [S2](020_Work-Plan.md#s2), not yet merged or tested with two accounts). See [020_Work-Plan.md](020_Work-Plan.md#related-issues).

**Next:** [S14](020_Work-Plan.md#s14), then [S6](020_Work-Plan.md#s6). [S1](020_Work-Plan.md#s1) to [S5](020_Work-Plan.md#s5) are done (not yet merged), so every bug not deferred is, and every GUI lag and logic-in-GUI finding, and the Settings page's keys are in its own node. S14 fixes what the review of that new code found. S11 is done but for the findings that go with S6 and the release day. Kyle has confirmed [D3](010_Decisions.md#d3), the last open detail.

## The files

| File | What it's for |
|---|---|
| `000_ReadMe.md` | This page: what the project is, how the files work, how to take part. |
| [010_Decisions.md](010_Decisions.md) | Kyle's proposed decisions (D1 to D7), the questions (Q1 to Q10), the house rules (H1 to H5), and the log of what was decided. **Read this first.** |
| [020_Work-Plan.md](020_Work-Plan.md) | The work split into sessions S1 to S13, each one Claude Code brief and one pull request; the Fix-first bugs; what was deferred to after 3.0; notes on the related issues. |
| [100_Bugs.md](100_Bugs.md) | Things that behave wrongly. |
| [110_GUI-Lag.md](110_GUI-Lag.md) | Work done on the UI thread. |
| [120_Logic-In-GUI.md](120_Logic-In-GUI.md) | Business rules in view-models that belong in Core. |
| [130_Old-Version-Code.md](130_Old-Version-Code.md) | Code that only exists for older FE-Buddy versions. |
| [140_Unused-Code.md](140_Unused-Code.md) | App code nothing uses, or only tests use. |
| [200_Comments-Core.md](200_Comments-Core.md) | Comments and XML docs in FeBuddy.Core. |
| [210_Comments-NASR.md](210_Comments-NASR.md) | Comments and XML docs in the NASR models and parsers. |
| [220_Comments-GUI.md](220_Comments-GUI.md) | Comments and XML docs in FeBuddy.Wpf. |
| [300_Unit-Tests.md](300_Unit-Tests.md) | The unit tests. |
| [400_Installer-Harness-CI.md](400_Installer-Harness-CI.md) | Installer, setup window, custom actions, Harness, Versioning, CI, build scripts. |
| [500_Developer-Docs.md](500_Developer-Docs.md) | `docs/Developers`, `README.md`, `SECURITY.md`. |
| [600_User-Docs-Proposal.md](600_User-Docs-Proposal.md) | The proposal to shrink the user manuals, section by section. |
| [610_On-Screen-Text.md](610_On-Screen-Text.md) | Text the user sees in the app; the tests that pin it. |
| [900_Review-Method.md](900_Review-Method.md) | How the review was done, its limits, what was checked and found fine. |

The numbers group the files: 0xx is the project itself, 1xx code behaviour, 2xx code comments, 3xx tests, 4xx build and installer, 5xx and 6xx docs, 9xx background.

## How to take part

1. **Comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374), quoting IDs** (`W2-1`, `D3`, `Q2`, `S4`). One comment can cover many. Say whether you agree, disagree, or would fix it another way, and why.
2. **The open questions need answers most.** Each Q in [010_Decisions.md](010_Decisions.md) has options; pick one or suggest another.
3. **Kyle records the outcome** in these files: the decision log in 010, and the Status line of every finding it affects.
4. **Spotted a mistake in a finding** (wrong line, wrong claim)? Say so on the issue. A pull request fixing a finding's text is fine too; don't delete or renumber findings.
5. **Found something new?** Add it as a finding (see [Keeping these files up to date](#keeping-these-files-up-to-date)) and mention it on the issue.

## How to read a finding

Each finding has a heading and a few labelled lines:

```text
### W2-1 · Bug · High · General tab's Save and "Undo last save" act on the whole `Services.AiracService` subtree
Status: Open · Session: S1
Where:    the files and lines, linked
Says:     what the comment or doc says (comment and doc findings only)
Problem:  what is wrong, and how we know
Proposed: the fix, often with replacement text
Future use: whether removed code might be needed later (unused and old-version code only)
Note:     duplicates, related findings, Kyle's decisions, re-checks
```

Every file starts with a **Summary** table of its findings, then the findings in full. Comment and doc files group them by folder; the others by severity. At the end, **Reported more than once** lists IDs merged into another finding.

### IDs

An ID is the review slice that found it, then a number: `C6-30` is the 30th finding of slice C6 (Core settings). IDs never change. [900_Review-Method.md](900_Review-Method.md#slices) lists every slice.

| Prefix | Slice |
|---|---|
| C1 to C8 | FeBuddy.Core, by feature area (C8: NASR models and parsers) |
| W1 to W7 | FeBuddy.Wpf, by layer |
| U1 | A sweep of the whole GUI for UI-thread work |
| O1 | Installer, setup window, custom actions, Harness, Versioning, CI, build scripts |
| T1a to T3 | Unit tests |
| D1a, D1b | Developer docs |
| D2 | User manuals (findings are sections of the manuals) |
| N1 | Found after the review, in code merged since |

Decisions (`D1` to `D7`), questions (`Q1` to `Q10`), house rules (`H1` to `H5`) and sessions (`S1` to `S13`) have no hyphen, so `D2` (a decision) and `D2-46` (a user-docs finding) can't be confused.

### Kinds

| Kind | Meaning |
|---|---|
| Bug | Behaves wrongly. |
| GUI lag | Work done on the UI thread. |
| Logic in GUI | A business rule in the GUI that belongs in Core. |
| Old-version code | Only exists for older FE-Buddy versions. |
| Unused | Nothing uses it. |
| Test-only | Only the tests use it. |
| Wrong doc | A comment or doc says something the code doesn't do. |
| Stale doc | Was true once, isn't now. |
| History wording | Explains code by what older code did. |
| Minor | Small slips: wording, naming, layout, a missing header. |

The user-docs proposal uses verdicts instead: Cut, Move into the GUI, Keep (shortened), Fix.

### Severity

- **High:** data loss, or something many users will hit.
- **Med:** a real defect, or a lag users will notice.
- **Low:** everything else, including most comment and doc fixes.

### Status

| Status | Meaning |
|---|---|
| Open | Not settled yet. Every finding starts here. |
| Agreed | The team agreed with the proposal; it's ready for its session. |
| Agreed, changed | Agreed with a different fix; the Status line says what changed. |
| Deferred | Agreed, but done after 3.0; the Status line says what was agreed. Listed under [After 3.0](020_Work-Plan.md#after-30). |
| Won't fix | Leaving it, with the reason. |
| Fixed | Done, with the pull request (and commit). |
| Partly fixed | Some done; the Status line says what's left. |

### Session

The session in [020_Work-Plan.md](020_Work-Plan.md) that will fix it, or After 3.0.

## Which code the review read

The review read [`dc08390`](https://github.com/Nikolai558/FE-BUDDY/tree/dc08390d05e3ce3f2be53b3106e9c5854380b236) (3.0.0-beta.12). Every code link in its findings is a permalink to it, so a link keeps showing the lines a finding is about even after the code changes. Findings made since (N1) link to the commit they were found at. To see the current code, change the commit in the link to a branch name.

Since then, [#376](https://github.com/Nikolai558/FE-BUDDY/pull/376) ([#375](https://github.com/Nikolai558/FE-BUDDY/issues/375), the shortcut icon) and [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377) (the [C1-5](100_Bugs.md#c1-5) fix) merged into `v3-B13-Dev`. Every finding in the files they changed was re-checked at `e15a634`, and the new code was reviewed too ([N1-1](500_Developer-Docs.md#n1-1) to [N1-3](610_On-Screen-Text.md#n1-3)). Details: [900_Review-Method.md](900_Review-Method.md#re-checked-after-the-review).

The review was AI-assisted and static: nothing was built or run. Timings are estimates, and test pass/fail claims come from reading the code. Treat a finding as a lead to check, not a verdict.

## Keeping these files up to date

- **IDs never change and are never reused.** A finding that turns out to be wrong gets `Won't fix` and the reason; it isn't deleted.
- **Change a Status** by editing the finding's Status line and its row in the file's Summary table, in the same pull request as the fix.
- **Add a finding** with the next N1 number (N1-44, N1-45, …), in the file for its kind, in the same format: an anchor line (`<a id="n1-4"></a>`), the heading, then the labelled lines. Add a row to the Summary table. Link code with a permalink to the commit you read.
- **Finish a session** by updating the Status of every finding it closed, then ticking the session on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).
- **Record a decision** in the decision log in [010_Decisions.md](010_Decisions.md#decision-log), and change the decision's or question's Status.
- **Links between these files** are relative (`100_Bugs.md#w2-1`), so they work on any branch. Every finding has an anchor: its ID in lower case.
- **Encoding:** UTF-8 without a byte-order mark, like every file in the repository (`FileEncodingTests` checks).

## When the project is done

When every session is ticked and [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374) is closed, we decide whether to keep this folder as a record or delete it. Git keeps it either way.
