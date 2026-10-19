# 610 · On-screen text and other user-doc findings

Part of the [3.0 pre-release review](000_ReadMe.md) ([#374](https://github.com/Nikolai558/FE-BUDDY/issues/374)).

Text the user sees in the app, and the user-doc findings that aren't part of the proposal in [600_User-Docs-Proposal.md](600_User-Docs-Proposal.md). At the end: the tests that pin on-screen wording word for word, which need updating with any text change. Session [S12](020_Work-Plan.md#s12).

**16 findings** (1 Med, 15 Low). Quote the ID when you comment on [#374](https://github.com/Nikolai558/FE-BUDDY/issues/374).

## Summary

| ID | Kind | Sev | Finding | Status | Session |
|---|---|---|---|---|---|
| [W5-8](#w5-8) | Minor | Low | GUI-TEXT: General-tab Airways alias tooltip ignores the Airways tab's ROI alias choice | Open | S12 |
| [N1-3](#n1-3) | Minor | Low | GUI-TEXT: the update window doesn't say Windows will ask for administrator permission | Open | S12 |
| [W5-12](#w5-12) | Minor | Low | GUI-TEXT: the ROI picker window's title and the Area card's option name the same ROI differently | Open | S12 |
| [W5-11](#w5-11) | Minor | Low | GUI-TEXT: General tab says "saved to this profile" but never shows which profile | Open | S12 |
| [W5-15](#w5-15) | Minor | Low | GUI-TEXT: explanations missing from the General tab, Settings and Info (User Guide text to move into the GUI) | Open | S12 |
| [W5-14](#w5-14) | Minor | Low | GUI-TEXT: Buffer Airway Waypoints labels don't match the rule or give the defaults | Open | S12 |
| [W5-16](#w5-16) | Minor | Low | GUI-TEXT: explanations missing from sub-service tabs and conversions (User Guide text to move into the GUI) | Open | S12 |
| [W5-13](#w5-13) | Minor | Low | GUI-TEXT: Procedure Charts uses "Cancel" and "Clear" for the same action | Open | S12 |
| [W5-6](#w5-6) | Minor | Low | GUI-TEXT: Chart Recall Help Command card doesn't say the note covers approach charts only | Open | S12 |
| [W5-7](#w5-7) | Minor | Low | GUI-TEXT: Procedure Charts says `.openurl`, but the file is written with `.OPENURL` | Open | S12 |
| [W5-9](#w5-9) | Minor | Low | GUI-TEXT: Settings says everything goes to the profile, and the shared-settings list leaves out Main Window | Open | S12 |
| [W5-5](#w5-5) | Minor | Low | GUI-TEXT: the status bar always says "Ready" | Open | S12 |
| [W5-10](#w5-10) | Minor | Low | GUI-TEXT: Telephony's Outputs card doesn't say where outputs are turned on and off | Open | S12 |
| [W5-2](#w5-2) | Minor | Med | GUI-TEXT: the CRC ERAM Defaults note names a settings file that no longer exists | Agreed | S6 |
| [C6-35](#c6-35) | Wrong doc | Low | User docs call a log file "that day"/"today's", but files are named by the UTC date | Open | S12 |
| [C4-26](#c4-26) | Wrong doc | Low | User-Guide: External Procedures in Our Airspace is described as covering only airports outside your facility | Open | S12 |

## FeBuddy.Wpf/ViewModels

<a id="w5-8"></a>

### W5-8 · Minor · Low · GUI-TEXT: General-tab Airways alias tooltip ignores the Airways tab's ROI alias choice

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs:94-95`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/AiracSubServices.cs#L94-L95)

**Says:** `Alias: "Airways.txt: ... Every airway, or, with ROI as the area, only those that cross it."`

**Problem:** With ROI as the area, the alias file gets every airway or only the ones that cross the ROI, depending on the Airways tab's "Which Airways the Alias File Gets" choice (`AliasRoiAirwaysOnly` → `AliasRoiScope`, AirwaysViewModel.cs:139,284; AirwaysView.xaml:37-47).

**Proposed:** "... Every airway, or, with ROI as the area, every airway or only those that cross it, as you choose on the Airways tab."

<a id="n1-3"></a>

### N1-3 · Minor · Low · GUI-TEXT: the update window doesn't say Windows will ask for administrator permission

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs:142-143`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Wpf/ViewModels/UpdateWindowViewModel.cs#L142-L143)

**Says:** `"Update now downloads the installer and runs it. FE-Buddy closes while it installs and opens again when it finishes; your settings are kept."`

**Problem:** Since [#377](https://github.com/Nikolai558/FE-BUDDY/pull/377), Windows asks for administrator permission inside setup, after FE-Buddy has closed. Before, it asked first, while FE-Buddy was still open, and saying No kept FE-Buddy open with "Update cancelled - installing needs administrator permission". The Uninstall window already warns ([`UninstallViewModel.cs:39-41`](https://github.com/Nikolai558/FE-BUDDY/blob/e15a634aa6915245e558875cd249bccdb8455480/FeBuddy/FeBuddy.Wpf/ViewModels/UninstallViewModel.cs#L39-L41): "FE-Buddy closes, and Windows asks you to confirm and for administrator permission. If you cancel there, nothing is removed - just start FE-Buddy again."). The update window says nothing, so the prompt comes as a surprise after the app has gone, and saying No leads to [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378).

**Proposed:** Update: `"Update now downloads the installer and runs it. FE-Buddy closes, Windows asks for administrator permission, and FE-Buddy opens again when the install finishes; your settings are kept. If you say No there, nothing changes - just start FE-Buddy again."` The same for Go back now. The "If you say No" sentence is true only once [#378](https://github.com/Nikolai558/FE-BUDDY/issues/378) is fixed. No test pins this text.

## FeBuddy.Wpf/ViewModels/ServiceTabs

<a id="w5-12"></a>

### W5-12 · Minor · Low · GUI-TEXT: the ROI picker window's title and the Area card's option name the same ROI differently

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs:975`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/ServiceTabs/GeojsonSubServiceViewModel.cs#L975); [`FeBuddy/FeBuddy.Wpf/Views/Cards/AreaCard.xaml:67`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/AreaCard.xaml#L67)

**Says:** window title `$"{Title}: Its Own ROI"` versus the radio `ContentStringFormat="An ROI specific to {0}"`

**Problem:** The window is titled "Airways: Its Own ROI", but the option that opens it says "An ROI specific to Airways". The validation messages also say "ROI specific to {Title}" (GeojsonSubServiceViewModel.cs:917).

**Proposed:** title the window `$"An ROI Specific to {Title}"`.

## FeBuddy.Wpf/Views

<a id="w5-11"></a>

### W5-11 · Minor · Low · GUI-TEXT: General tab says "saved to this profile" but never shows which profile

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/AiracGeneralTabView.xaml:176-179`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/AiracGeneralTabView.xaml#L176-L179)

**Says:** `Text="All settings/preferences are saved to this profile."`

**Problem:** Nothing on the AIRAC Service screen names the profile in use, so "this profile" refers to nothing the user can see.

**Proposed:** bind the profile name in ("All settings are saved to the profile *ZOB*."), or say "the settings profile in use".

<a id="w5-15"></a>

### W5-15 · Minor · Low · GUI-TEXT: explanations missing from the General tab, Settings and Info (User Guide text to move into the GUI)

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/AiracGeneralTabView.xaml:59-115`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/AiracGeneralTabView.xaml#L59-L115),169-180; [`SettingsView.xaml:96-131`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml#L96-L131),309-362; [`InfoView.xaml`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/InfoView.xaml) (resource cards from [`InfoViewModel.cs:38`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/ViewModels/InfoViewModel.cs#L38))

**Says:** User-Guide.md:70-79, 492-494, 505-507, 38

**Problem:** Things a new user needs that are only in the User Guide:
  - General: what the cycle states mean (only *partial* has a tooltip; *not yet published* and *failed* don't). That Add Custom Aliases has no row and joins the run by itself while any alias file is ticked.
  - Facility: what it is used for (ticked first on ARTCC lists, listed first in reports, needed by External Procedures).
  - Update channel: set at first start, and updating never changes it.
  - Info: the "User Guide" card's blurb "How each tool works, field by field." will be wrong once the guide shrinks.

**Proposed:** add one caption line for each of these. Give the cycle rows a tooltip for every state, not just Partial (`AiracGeneralTabView.xaml:71-80`). Reword the Info blurb when the guide is cut.

<a id="w5-14"></a>

### W5-14 · Minor · Low · GUI-TEXT: Buffer Airway Waypoints labels don't match the rule or give the defaults

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/AirwaysView.xaml:153`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/AirwaysView.xaml#L153),158,166-167

**Says:** `Text="Around fixes"`, `Text="Around NAVAIDs"`

**Problem:** The first radius applies to a 5-character fix identifier. The second applies to every other waypoint, not just NAVAIDs (AirwayWaypointBuffer.cs:14-18,36-38). The defaults (2.5 and 5 NM) are only in the User Guide (line 139).

**Proposed:** labels "Around five-letter fixes" and "Around NAVAIDs and other points". Make note 1 "Allowed range: 0 to 10 NM (2.5 and 5 to start)."

<a id="w5-16"></a>

### W5-16 · Minor · Low · GUI-TEXT: explanations missing from sub-service tabs and conversions (User Guide text to move into the GUI)

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`NavaidsView.xaml:67-68`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/NavaidsView.xaml#L67-L68),97-99; [`TelephonyView.xaml:36-45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/TelephonyView.xaml#L36-L45); [`ConcatenateAliasesView.xaml:61-92`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ConcatenateAliasesView.xaml#L61-L92); [`SctToGeojsonView.xaml:22-23`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/SctToGeojsonView.xaml#L22-L23); [`Cards/SummaryCard.xaml`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/SummaryCard.xaml)

**Says:** User-Guide.md:51-59, 176, 289-291, 319-322, 386-389

**Problem:** Things a new user needs that are only in the User Guide:
  - NAVAIDs: SHUTDOWN NAVAIDs are always left out. In "By NAVAID type", CONSOLAN and unrecognized types get no symbol style.
  - Telephony: where operators come from (FAA JO 7340.2), and which entries are dropped (no 3LD or telephony, expired special call signs).
  - Add Custom Aliases: a `.FeUseOnly` line moves to the top. A file that can't be read is left out with a warning.
  - SCT2: airports, VORs, NDBs and fixes aren't written (their names only locate points), and VRC colours aren't carried over.
  - What You'll Get: how to read the chips ("for", "and only", "but only", "along with").

**Proposed:** one short caption line each, or a tooltip on the What You'll Get header for the chips.

<a id="w5-13"></a>

### W5-13 · Minor · Low · GUI-TEXT: Procedure Charts uses "Cancel" and "Clear" for the same action

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ProceduresView.xaml:376-377`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ProceduresView.xaml#L376-L377) (compare 190-191)

**Says:** `<Button Content="Cancel" ... Command="{Binding CancelProcedureCommand}" />`

**Problem:** `CancelProcedureCommand` only empties the box (`NewProcedureText = string.Empty`, ProceduresViewModel.cs:160). The Airports card's button does the same and is labelled "Clear" (`ClearAirportTextCommand`, line 156). "Cancel" suggests an edit is being abandoned.

**Proposed:** label it "Clear" and rename the command `ClearProcedureTextCommand`.

<a id="w5-6"></a>

### W5-6 · Minor · Low · GUI-TEXT: Chart Recall Help Command card doesn't say the note covers approach charts only

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ProceduresView.xaml:434-435`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ProceduresView.xaml#L434-L435)

**Says:** `This alias is a `.note` command that provides the user a quick reference chart for how to formulate an FAA Chart Recall command.`

**Problem:** The note covers instrument approaches only ("Help: Chart Recall (IAPs)"); for any other chart it points to the command guides (ChartRecallHelpCommand.cs:3-8,22). The User Guide says so (User-Guide.md:235-239).

**Proposed:** "This alias is a `.note` command with a quick reference for writing an instrument approach's (IAP's) chart recall command. For any other chart it points to your facility's command guide."

<a id="w5-7"></a>

### W5-7 · Minor · Low · GUI-TEXT: Procedure Charts says `.openurl`, but the file is written with `.OPENURL`

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ProceduresView.xaml:53-54`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ProceduresView.xaml#L53-L54)

**Says:** `Description="Writes an `.openurl` command for most charts in the d-TPP Metafile.`

**Problem:** The output line is `$"{Command} .OPENURL {Url}  ; {Comment}"` (ChartRecallResults.cs:18), and the User Guide and Architecture both show `.OPENURL`.

**Proposed:** write `.OPENURL`.

<a id="w5-9"></a>

### W5-9 · Minor · Low · GUI-TEXT: Settings says everything goes to the profile, and the shared-settings list leaves out Main Window

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml:50`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/SettingsView.xaml#L50),70-73

**Says:** `Text="Preferences for this installation, saved to the settings profile in use."` and `Text="Update channel&#xA;News posts you have read&#xA;FE-Buddy's GitHub token&#xA;Your saved credentials"`

**Problem:** Several settings are not stored in the profile. The update channel and FE-Buddy's GitHub token go to `Shared.json`, credentials go to Windows Credential Manager, and window size and position go to Shared.json (`UserConfigPortability.LocalKeys`, UserConfigPortability.cs:40-49, includes `RememberWindowPlacement` and `MainWindowPlacement`). The "Shared across profiles" list doesn't mention the window. The User Guide (line 490) does.

**Proposed:** subtitle: "Preferences for this installation. Most are saved to the settings profile in use; the cards that aren't say so." Add "Main window size and position" to the shared list.

<a id="w5-5"></a>

### W5-5 · Minor · Low · GUI-TEXT: the status bar always says "Ready"

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml:523-526`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ShellWindow.xaml#L523-L526)

**Says:** `<TextBlock DockPanel.Dock="Left" Text="Ready" Style="{StaticResource Text.Caption}" .../>`

**Problem:** The text is a constant. It says "Ready" while AIRAC data is still downloading, has failed, or a run is going. That contradicts the AIRAC status in the caption and the Systems box.

**Proposed:** bind it to a real status (e.g. `HealthSummary`, or a run-in-progress line), or delete the TextBlock.

<a id="w5-10"></a>

### W5-10 · Minor · Low · GUI-TEXT: Telephony's Outputs card doesn't say where outputs are turned on and off

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/TelephonyView.xaml:36-45`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/TelephonyView.xaml#L36-L45) (compare [`ProceduresView.xaml:47-48`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/ProceduresView.xaml#L47-L48))

**Says:** Procedures: `<TextBlock ... Text="Turn these on or off in the General tab." />`. Telephony's Outputs card has no such line.

**Problem:** OutputStatusRow's header comment (OutputStatusRow.xaml:5-6) says "the card says where to change it". Telephony's card doesn't, so the On/Off chip looks like a switch.

**Proposed:** add the same caption at the top of Telephony's Outputs card.

## FeBuddy.Wpf/Views/Cards

<a id="w5-2"></a>

### W5-2 · Minor · Med · GUI-TEXT: the CRC ERAM Defaults note names a settings file that no longer exists

**Status:** Agreed ([H1](010_Decisions.md#h1)); pulled forward from S12, to be fixed before the public beta (Kyle, 2026-10-19) · **Session:** [S6](020_Work-Plan.md#s6)

**Where:** [`FeBuddy/FeBuddy.Wpf/Views/Cards/CrcDefaultsCard.xaml.cs:21-25`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Wpf/Views/Cards/CrcDefaultsCard.xaml.cs#L21-L25)

**Says:** `"Note: This should be a one-time setup, as these values are saved to your `UserConfig.json` file."`

**Problem:** Settings are saved to the profile file `User Configurations\UserConfig.<Profile>.json`. `UserConfig.json` is `UserConfigFile.LegacyConfigFileName`, the 3.0.0-beta.3 file (UserConfigFile.cs:59-60). Every AIRAC tab shows this text.

**Proposed:** "Note: this should be a one-time setup: these values are saved in your settings profile."

**Note:** Part of the pattern in [D1b-24](220_Comments-GUI.md#d1b-24) (this is the one users see).

## docs/Users

<a id="c6-35"></a>

### C6-35 · Wrong doc · Low · User docs call a log file "that day"/"today's", but files are named by the UTC date

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`docs/Users/FAQ-and-Troubleshooting.md:100`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/FAQ-and-Troubleshooting.md?plain=1#L100); [`.github/ISSUE_TEMPLATE/1-bug-report.yml:42`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/.github/ISSUE_TEMPLATE/1-bug-report.yml#L42); [`FeBuddy/FeBuddy.Core/Infrastructure/Logging/AppLog.cs:310`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/FeBuddy/FeBuddy.Core/Infrastructure/Logging/AppLog.cs#L310)

**Says:** "`FE-Buddy_<date>.log` - everything FE-Buddy did that day (kept 30 days)"; "today's log is `%APPDATA%\FE-Buddy\Logs\FE-Buddy_<date>.log`"

**Problem:** `ConsumeAsync` names the file from `entry.UtcTimestamp`, and `GetLogFilePath` says "UTC date". For a US user, everything after about 7-8 pm local time goes into the next date's file, so "today's" file misses an evening's run.

**Proposed:** FAQ: "...everything FE-Buddy did that day, by UTC date (kept 30 days)". Bug template: "today's log is ... `FE-Buddy_<UTC date>.log` (in the evening in the Americas, that's tomorrow's date)".

<a id="c4-26"></a>

### C4-26 · Wrong doc · Low · User-Guide: External Procedures in Our Airspace is described as covering only airports outside your facility

**Status:** Open · **Session:** [S12](020_Work-Plan.md#s12)

**Where:** [`docs/Users/User-Guide.md:213-218`](https://github.com/Nikolai558/FE-BUDDY/blob/dc08390d05e3ce3f2be53b3106e9c5854380b236/docs/Users/User-Guide.md?plain=1#L213-L218)

**Says:** `also includes the procedures at airports outside your facility (Settings ▸ Facility) that have a waypoint inside it`

**Problem:** The rule includes a matching departure or arrival at any airport the area leaves out, including your facility's own airports. That happens with Area None or ROI, or when your facility isn't ticked (ProcedureSelection.cs:32-37, 164-173: `included.Add` doesn't depend on `inPrimaryFacility`, which only controls the label).

**Proposed:** `also includes departures and arrivals at any airport the area leaves out whose route has a waypoint inside your facility (Settings ▸ Facility): ...`. The label sentence at 250-252 is already right.

## Tests that pin on-screen wording

From T3-35. About 260 assertions check GUI text word for word. Any text change above needs the matching test updated.

<details><summary>The full list</summary>

`=` exact, `^` starts with, `$` ends with, `in` contains.

**Controls**
- OutputTagTests: each_tag_has_a_short_name = "Alias", "GeoJSON", "Changes", "JSON"; a_tag_says_what_it_affects_and_whether_that_is_off = "Affects the GeoJSON files.", "Affects the alias file, which is off in the General tab.", "Whether each sub-service writes Procedures.json."; with_every_output_off_a_card_says_which_and_where_to_turn_one_on = "GeoJSON is off for Departures. Turn it on in the General tab.", "The alias file and GeoJSON are off for Departures. Turn one on in the General tab.", "The alias file, Procedure_Changes.md and Procedures.json are off for Departures. Turn one on in the General tab."; a_card_greys_out_with_a_note_... = "The alias file and GeoJSON are off for Departures. Turn one on in the General tab."
- OutputSummaryTests: each_line_after_the_first_starts_with_a_chip = "for", "in", "but only", "and only", "with only", "along with".

**AiracGeneralTabViewModel / SubServiceRow**
- an_output_a_sub_service_does_not_offer_is_always_off_and_greyed_out = "Fixes doesn't make an alias file.", = "Fixes doesn't make Procedure_Changes.md; only Procedure Charts does.", $ "Its settings are on the Fixes tab, in the list to the left."
- the_preview_names_each_included_sub_service_with_its_outputs = "Airports (Alias, GeoJSON)\nProcedure Charts (Procedure Changes, Procedures JSON)"
- a_ready_cycle_without_its_d_tpp_metafile_... = "ready", = "partial", = "d-TPP Metafile not available yet", ^ "Cycle 2611's NASR data is ready, but its d-TPP Metafile isn't."
- the_tooltip_gives_the_dates_... in "expect it between {from} and {to}", in "Procedure_Changes.md, Procedures.json and Faa_Chart_Recall.txt", in "you can't add procedures or airport-specific procedures"
- past_the_usual_dates_... in "should be out by now and the download may have failed"

**AiracServiceViewModel**
- a_sub_service_left_out_keeps_its_tab_greyed_out_with_a_tooltip ^ "A symbol and a label for every fix", $ "To include it, tick Fixes under Include in the General tab."
- add_custom_aliases_comes_in_... ^ "Adds your facility's own alias files to Combined_Alias.txt", $ "To make one, tick a sub-service and its Alias box in the General tab."
- AiracSubServicesTests.the_sub_services_are_in_the_rail_order = the 11 display names.

**AirwaysViewModel**
- j_q_start_high_... = "Choose High, Low, or Both.", = "Choose High, Low, or Both for Y, ZK on the High and Low Airway Classification card, or untick them under Airway Types to Include."
- saved_choices_come_back_and_a_new_type_needs_one ^ "Choose High, Low, or Both for ZK "
- the_preview_says_which_types_go_in_each_file = "High: J · Low: T · Both: ZK · not chosen yet: Y"
- WhatYoullGetTests (Airways): airways_names_the_types_... = "Maps of every airway type selected below (J and V)", "Every airway type selected below (J and V)", "but only the portions inside the ROI specific to the Airways sub-service", "but only those that cross the ROI specific to the Airways sub-service"; airways_lists_the_high_and_low_files_... = "The lines of every airway type set as \"High\" or \"Both\" below.", "The waypoint IDs of every airway type set as \"Low\" or \"Both\" below.", "Every J airway, drawn as lines.", "Every J airway waypoint, drawn as symbols.", "One set of files like these for each airway type selected below: 4 in all."; airways_with_no_type_ticked_... = "No airways: tick a type under Airway Types to Include"; airways_before_a_cycle_... = "Maps of every airway type selected below (all but J, Q, and V)", "Maps of every airway type selected below"

**ArrivalsViewModel / DeparturesViewModel**
- AmendmentQuestionTests.the_question_and_the_cycles_caption_name_the_selected_cycle $ "from the selected cycle?", = "(1 = only procedures amended in the selected cycle)", $ "from the 2610 cycle?", = "(1 = only procedures amended in 2610)"
- WhatYoullGetTests: departures_lists_only_the_filters_that_are_set = "Every SID and obstacle departure"; departures_narrows_by_the_area_... = "Every SID (no obstacle departures)", "in ZNY and ZOB", "and only those amended in the last 4 cycles", "but only those from an airport inside the ROI specific to the Departures sub-service", "but only those amended in the last 4 cycles"; departures_name_the_cycles_... = "but only those amended in the selected cycle", "... in cycle 2610", "... in the last 5 cycles (since cycle 2606)", "... (since cycle 2511)", "... in the last x cycles"; departures_with_the_artccs_area_and_none_ticked_... = "No departures yet: tick an ARTCC on the Area card"; departures_with_no_artcc_narrows_with_but_only = "but only those amended in the last day, counting back from today"; departures_lists_each_departures_files_... in "Alias Command Guide", = "Each airport's GeoJSON files go in `Geojson\<ARTCC ID>\<Airport ID>\`."; arrivals_lists_its_artccs_as_inclusive_... = "Every arrival", "for airports in ZAB, ZLA, and ZOA", "and only those amended in cycle 2610"; arrivals_name_the_oldest_cycle_... = "but only those amended in the last 3 cycles (since cycle 2608)"; arrivals_in_the_roi_by_waypoint_... = "but only those with at least one fix inside the default ROI", "and only those amended in the selected cycle"; arrivals_words_each_amendment_filter = 6 sentences ("but only those amended in the last 30 days, counting back from today", "... on or after a date not yet picked", ...); arrivals_with_a_region_by_airport_... = "but only those for an airport inside the default ROI"

**AirportsViewModel / NavaidsViewModel / ArtccBoundariesViewModel / FixesViewModel / WxStationsViewModel (WhatYoullGetTests)**
- airports_gives_each_output_its_block_... = "Every operational airport", "but only airports inside the default ROI"; airports_names_its_own_roi_... = "but only airports inside the ROI specific to the Airports sub-service"
- the_footer_names_the_outputs_... = "Turn GeoJSON and Alias output on or off in the General tab.", "Turn GeoJSON output on or off in the General tab."
- navaids_types_and_roi_narrow_the_geojson_only = "Every operational NAVAID except VOT", "but only those inside the default ROI", "A symbol at each NAVAID.", in "every operational NAVAID in the NAS"; navaids_one_file_per_type_... = "A symbol at each {type}.", "One set of files like these for each NAVAID type ticked below: N in all."
- artcc_boundaries_names_its_artccs_... = "Every ARTCC boundary", "The boundaries of ZLA and ZOA", "but only the parts inside the default ROI"; artcc_boundaries_lists_the_files_... = "Boundaries labeled \"HIGH\"", "Boundaries labeled \"UNLIMITED\" go in both files."; ..._only_for_a_few_ticked_artccs = "ZOB's boundary labeled \"HIGH\""
- fixes_names_what_each_layout_keeps_... = "Fixes used as RPRTNG-PNT or WYPNT", "Fixes on ENROUTE LOW, IAP, or no chart", "Fixes in these chart + fix use combinations: IAP + WYPNT and STAR + RPRTNG-PNT"; fixes_with_none_picked_... = "No fixes: tick a fix use", "No fixes: tick a chart", "No fixes until you add a chart + fix use combination"; fixes_in_the_region_... = "Every fix", "but only those inside the default ROI"; fixes_list_the_files_... = "Fix symbols."
- wx_stations_list_their_files_... = "Every US and US-territory station with an ICAO ID that reports METARs"

**GeojsonSubServiceViewModel (Area card, AreaTests) / CrcDefaultsChoiceTests**
- the_artccs_area_needs_an_artcc_ticked = "Tick at least one ARTCC on the Area card, or pick another area."
- the_roi_area_needs_a_default_roi_or_the_tabs_own = "No default ROI is set. Set one in Settings or on the Map page, or pick \"An ROI specific to Airports\" on the Area card.", ^ "No default ROI is set.", = "SW 32.5, -120 / NE 37, -114. Change it in Settings or on the Map page."
- the_tabs_own_roi_needs_four_valid_corners_... = "Enter the southwest latitude of the ROI specific to Wx Stations.", ^ "The ROI specific to Wx Stations: "
- the_preview_adds_the_roi_corners_... = "No default ROI is set"; the_preview_leaves_out_the_geojson_rows_... = "FE-Buddy helper properties", "CRC ERAM defaults"
- CrcDefaultsChoiceTests.specific_files_need_one_ticked ^ "CRC-ERAM defaults are set to go on specific files, but none is ticked."

**ProceduresViewModel**
- AreaTests: procedures_cards_that_add_to_the_area_say_what_they_add_to = "Even if outside the facilities you selected, the airports you list here will be included in the `Procedure_Changes.md` and `Procedures.json` output." (+ the two "departures and arrivals" / "airport-specific departures and arrivals" variants), ^ "Select the following chart types to be included in the `Procedure_Changes.md` and `Procedures.json` files.\nNote:", $ "outside the facilities you have chosen.", = "Even if outside the default ROI you selected, ...", $ "outside the default ROI you have set.", ^ "Even if outside the ROI specific to Procedure Charts you selected,", $ "outside the ROI specific to Procedure Charts you have set.", = "The airports you list here will be included in ... output."; procedures_warn_while_a_metafile_falls_short ^ "Cycle 2611's metafile isn't out yet", = "Cycle 2610's metafile isn't available, so deleted procedures are listed without a link."; procedures_say_the_previous_cycle_is_still_loading_... = "Cycle 2610 is still loading. Once it has, deleted procedures link to its charts."; procedures_cards_name_the_documents_that_are_on = "The airports you list here will be included in the `Procedures.json` output.", ^ "Select the following chart types to be included in the `Procedures.json` file.", = "... in the `Procedure_Changes.md` output.", = "The airports you list here will be included in the documents."; procedures_with_everything_... = "Select the following chart types to be included in the `Procedure_Changes.md` and `Procedures.json` files."; procedures_with_no_area_needs_something_listed = "With \"None\" picked on the Area card, list at least one airport or procedure for the documents to include, or pick another area."; procedures_count_the_facilities_... / procedures_facilities_area_needs_a_facility_ticked_... = "Tick at least one facility on the Area card, or pick another area."
- ProcedureFixInFacilityCardTests: the_option_is_on_to_start_and_names_the_facility = "Include procedures for non-`ZDC` airports that have a waypoint inside `ZDC`", = the full tooltip "A departure or arrival is included when any point on its route, ... Only the chart types ticked under Chart Types are included."; the_cards_footer_says_what_its_options_apply_to = "only apply to Departures (ODP/SID) and Arrivals (STAR). IAPs may be included in future updates.", "don't apply to `Faa_Chart_Recall.txt`"; with_no_facility_set_... = "Include procedures for airports outside your facility that have a waypoint inside it", in "is a fix or NAVAID that NASR puts in your facility.", = "Set your facility in Settings ▸ Facility to use this card."; with_everything_... = "\"Everything\" already includes every airport, so this card isn't used."; with_the_area_none_the_option_is_enough_to_pick = the "With \"None\" picked ..." sentence; with_only_the_option_no_chart_type_ticked_is_flagged ^ "No chart types are ticked"; what_youll_get_adds_the_neighbors_procedures = "Charts for airports within ZDC", "along with departures and arrivals at other airports whose route has a fix in ZDC", "Departures and arrivals at other airports whose route has a fix in ZDC", "with only these types: IAP, STAR, DP, ODP, DAU, and APD"
- ProcedureHelpCommandCardTests: the_question_names_the_default_command = "The command is `.helpCHARTRECALL` by default. Would you like to rename it?"; a_new_name_must_be_typed_... = "Type the command's new name, or pick No to keep .helpCHARTRECALL.", "Use letters and numbers only: no spaces or symbols."
- ProcedureNameCardsTests: an_approach_can_not_be_added = "Not a departure or arrival in this cycle."; a_procedure_picked_by_its_full_name_... = "Cycle 2611: BRWNZ FIVE (RNAV)"; a_root_typed_in_any_case_... / the_same_airport_specific_procedure_is_listed_once = "Already in the list."; a_listed_name_the_cycle_has_no_... = "Cycle 2611 has no departure or arrival named ZZZZZ, so it's left out." (+ the ILS and "named BRWNZ at PIT" variants); "LAS — BRWNZ", "PIT — GRUUB" labels
- ProceduresViewModelTests: what_cannot_be_added_stays_in_the_box_... = "Not an airport in this cycle: KXYZ\nAlready in the list: KPIT\nAlready included by ZOB, a facility ticked on the Area card: CLE, CAK"; an_airport_inside_the_roi_area_... = "Already included by the ROI specific to the Procedure Charts sub-service: CAK"; the_list_stops_at_its_limit = "Over the {Max}-airport limit: MDW"
- WhatYoullGetTests (Procedures): procedures_adds_what_is_listed_... = "Charts for airports within ZOB", "along with CLE and DTW", "along with BRWNZ, wherever it's published", "`Procedure_Changes.md`: only those that were added, changed, or deleted this cycle", "with only these types: IAP, STAR, DP, ODP, DAU, and APD (the procedures you named are always included)", "Every chart at every airport in the d-TPP metafile, not just the ones you chose for `Procedure_Changes.md` and `Procedures.json`", "along with `.helpCHARTRECALL` at the top: a quick reference for writing a chart recall command"; procedures_with_only_the_airports_listed_... = "Charts for the 13 airports you listed"; procedures_counts_a_long_airport_list_... = "Charts for the 7 airports you listed", "along with BRWNZ and CLVLD, wherever they're published", "with only these types: none yet, so tick one under Chart Types"; procedures_with_nothing_picked_... = "Nothing yet: pick an area, or list an airport or a procedure"; procedures_in_the_roi_... = "Charts for airports within the ROI specific to the Procedure Charts sub-service"; procedures_with_everything_... = "Charts for every airport in the d-TPP metafile"; the_procedures_alias_line_names_only_... (3 sentences); the_procedures_alias_block_names_the_help_command_... ("along with `.helpCR` at the top: ..."); the_star_chart_type_... = "STARs (STAR)"

**TelephonyViewModel / VirtualAirlineItem**
- TelephonyViewModelTests: a_new_tab_has_no_virtual_airlines_... / the_confirm_button_says_add_... = "Add", "Save"; a_virtual_airline_that_cannot_be_written_... = "The 3LD is three letters, A to Z.", "The telephony needs a letter or a digit."; a_duplicate_ignoring_case_... / an_item_edited_into_another_items_values_... = "Already in the list."; the_same_3ld_and_telephony_with_another_organization_... = "Already in the list as DVA · DELTA · Delta Virtual. Only one virtual airline is written for each 3LD and telephony."; a_hand_written_entry_with_a_bad_3ld_... in "Virtual airline 1", in "3LD"; a_bad_entry_is_named_by_its_place_in_the_list in "Virtual airline 2 (DEV)", in "telephony"; a_tab_with_a_bad_entry_cannot_be_saved in "no virtual organization"; the_preview_says_none_... = "None"; the_preview_lists_each_virtual_airline_... = "DVA (DELTA, Delta Virtual), DEV (DEVIL AIR, Rustic Virtual)"; the_run_summary_names_the_virtual_airlines_only_when_there_were_some = "Telephony.txt: 9 command(s) for 3 ICAO operator(s), 2 U.S. special call sign(s) and 4 virtual airline(s)", "Telephony.txt: 9 command(s) for 3 ICAO operator(s) and 2 U.S. special call sign(s)"; the_run_summary_says_nothing_written_... = "Nothing written"; the_run_summary_says_how_many_... = "... and 250 virtual airline(s) (248 from the virtual airline list)"; with_the_list_included_a_virtual_airline_exactly_on_it_... = "Already on the Virtual Airline List (GNG + VATSIM-Radar), which you've chosen to include, as DAL · DELTA · Fly Delta Virtual. It's written from there, so it can't be added again."; with_the_list_included_one_with_its_3ld_... = "The Virtual Airline List (GNG + VATSIM-Radar) also has DAL · DELTA, for Fly Delta Virtual. Only yours is written.", "Same 3LD and telephony as one on the list, so the list's is left out."; one_with_a_real_operators_3ld_... = "AAL · AMERICAN is a real operator (AMERICAN AIRLINES INC.), so yours won't be written: Telephony.txt writes only the real operator's card.", "Same 3LD and telephony as a real operator (AMERICAN AIRLINES INC.), so it's left out of Telephony.txt."; the_lists_status_says_how_old_... = "FE-Buddy's copy is from {date}: 2 virtual airlines.", "FE-Buddy has no copy yet, so a virtual airline you add can't be checked against it.", ^ "FE-Buddy's older copy of the list is from ", = "Using FE-Buddy's older copy of the list until its two parts can be downloaded."; a_part_with_no_copy_is_a_warning = "FE-Buddy has no copy of the GNG fictional airline list yet, so the list is missing its airlines."; ticking_the_box_downloads_... in "Couldn't download the GNG fictional airline list (offline), and FE-Buddy has no copy of it"; a_download_uses_the_fresh_copies_... = "Couldn't download the latest VATSIM-Radar airline list (...), so FE-Buddy's copy from {date} is used."; a_download_that_throws_... = "Couldn't download the latest list (no network stack)."; the_preview_says_whether_the_list_is_included_... = "Not included", ^ "Included. FE-Buddy's copy is from ", $ "FE-Buddy has no copy of the GNG fictional airline list yet, so the list is missing its airlines."
- WhatYoullGetTests.telephony_adds_the_virtual_airline_list_... = "Every operator with a 3LD and telephony in FAA JO 7340.2's `[ICAO register](...)` (Chapter 3, Section 1) and `[U.S. special call signs](...)` (Section 4), except expired special call signs"
- VirtualAirlineItemTests: it_starts_off_the_list_... = "Same 3LD and telephony as one on the list, so the list's is left out."; a_real_operators_note_comes_before_the_lists = "Same 3LD and telephony as a real operator (AMERICAN AIRLINES INC.), so it's left out of Telephony.txt."

**ConcatenateAliasesViewModel / AliasSourceRow**
- the_custom_files_are_added_to_start_... = "Add Custom Aliases", "Added to `Aliases\Combined_Alias.txt`, after every FE-Buddy alias file this run writes", "https://example.com/ZOB-Alias.txt, with no credential"
- with_the_custom_files_not_added_... = "Custom alias file 1: Enter the file's web address.", "Not added: `Aliases\Combined_Alias.txt` holds only FE-Buddy's alias files", "Combined_Alias.txt: 3 command(s) from Airports.txt. Your custom alias files weren't added"
- a_github_file_its_credential_cannot_read_... ^ "GitHub refused the credential 'ZOB GitHub'."
- a_file_on_this_pc_with_no_alias_commands_... ^ "It has no alias commands", ^ "Custom alias file 1: It has no alias commands"
- a_saved_file_is_one_line_... = "This file is not on this PC. It may have been moved or renamed."; a_new_web_address_opens_... = "No address yet", "Enter the file's web address.", "Web address"; the_line_names_the_credential_... = "Web address, read with ZOB GitHub"

**DuplicateAliasReviewViewModel / DuplicateAliasesCardViewModel**
- each_command_starts_with_its_first_line_kept_... = "3 lines", ^ "One alias command is used by more than one line"; a_saved_choice_fills_its_line_... ^ "2 alias commands are used by more than one line"; each_command_is_checked_as_its_choices_change = "Only one line can keep .orfNUTIYf. Leave out or rename the others.", "Type the new command.", ".ZOBATIS is already used in this run. Choose another command."; a_new_command_given_twice_... = ".orfX is already given to another line. Choose another command." (and .ORFX); a_runs_choices_are_saved_and_listed_... = ".orfNUTIYf in Arrivals.txt"/"Rename to .orfNUTIYa", ".dtwI22Lc in Faa_Chart_Recall.txt (line 2 with it)"/"Leave out", ".orfNUTIYf in Departures.txt"/"Leave out"; a_saved_keep_is_listed_as_keep = "Keep"

**EramToGeojsonViewModel / FileConversionsViewModel**
- a_folder_with_two_geomaps_files_blocks_the_run = "1 Geomaps file in this folder.", ^ "The source folder has 2 Geomaps files, and this conversion takes one per run."
- feb_properties_on_with_none_picked_needs_fixing = "FE-Buddy helper properties are on but none are selected. Pick at least one, or switch them off."
- the_run_summary_names_the_console_command_control_rundown = "1 file(s) read, 2 GeoJSON file(s) written, 5 feature(s), plus ConsoleCommandControl.txt"
- FileConversionsViewModelTests: the 3 source names ("FAA Radar Video Map .dat files", "Legacy Sector File (.sct2)", "FAA ERAM Adaptation Files"), "FAA Radar Video Map .dat files → GeoJSON for CRC", "FAA ERAM Adaptation Files ▸ Geomaps.xml → GeoJSON for CRC"

**FileNamesViewModel**
- turning_renaming_on_... = "Type a new name, or untick this file to keep FE-Buddy's name.", ^ "Duplicate_Alias_Commands.txt: Type a new name", $ "2 more file name(s) need attention too - see the marked boxes."
- a_name_that_cannot_be_used_says_why = "Leave off .geojson: FE-Buddy adds the extension itself.", "A file name can't contain \ / : * ? \" &lt; > |.", "Windows keeps CON as a device name, so no file can have it. Choose another name." (Core's OutputFileNames text)
- two_files_may_not_end_up_with_one_name = "Duplicate_Alias_Commands.txt is already the name of another file. Give this file a name of its own.", "... is being renamed ZOB.txt too. Give each file its own name." (x2)
- a_file_another_tab_adds_after_a_save_... = "This file is new here. Type a new name for it, or untick it to keep FE-Buddy's name." (twice, once with the file name)
- the_preview_lists_each_new_name = "No - every file keeps FE-Buddy's name", "File Names", "Rename files"/"Yes - 1 file(s)"

**MapViewModel / MapFeatureCard**
- the_fields_show_a_saved_roi_in_full = "Saved"; typed_corners_move_the_box = "Unsaved", "Warn"; a_box_over_the_meridian_is_refused_on_save in "180° meridian"; cancel_is_only_available_while_editing = "Editing"; clearing_then_saving_... = "Not set"; empty_corners_are_refused_... = "Draw a box on the map (or type its corners) first."
- a_ctrl_click_shows_a_card_for_each_shape = "2 objects", "At 41.40000, -81.85000.", "LineString · 3 points · feature 152 in the file", "Copy the properties as JSON", "Airport · 41.40000, -81.85000", "Copy the properties"; a_shape_with_no_properties_says_so = "Polygon · 2 parts, 6 points"; a_click_on_nothing_says_how_to_use_it = "Nothing here", "At 40.00000, -100.00000. Ctrl + click on a line, a dot or a label to see its properties."; many_shapes_show_the_ones_on_top = "30 objects", "100 or more objects", $ "Showing the 25 drawn on top."

**ShellViewModel (SystemHealthTests)**
- = "AIRAC data", "previous / current / next ready", "All systems nominal", "previous / current ready, next not published yet", "current ready, previous / next loading…", "ready — a best-effort cycle failed, previous loading…", "next: no d-TPP Metafile yet", "Ready except next d-TPP Metafile", "previous and next: no d-TPP Metafiles yet", "Ready except previous and next d-TPP Metafiles", "2 need attention", "1 needs attention", "ready — a best-effort cycle failed", "GitHub token", "'ZOB' expires in 12 days, on {date}", "Paste a new token in Settings ▸ Credentials", "GitHub tokens", "'Old' was rejected by GitHub\n'Odd' expires soon", "Paste new tokens in Settings ▸ Credentials", "not loaded (dev launch) — a cycle loads when something needs it", "current ready, next loading…, previous not loaded (dev launch)", "current cycle failed to download or parse", "downloading and parsing…"

**ServiceTabViewModel / ServicePreviewTabViewModel / ServiceRunReviewTabViewModel**
- ServiceTabValidationTests: "3 things on this page need your attention. Their areas are outlined in red below.", "One thing on this page needs your attention. Its area is outlined in red below.", "Fixes needs fixing before the run.\nIn the list of tabs on the left, click the tab with a red dot. The red box at its top lists what to fix, and each area to fix is outlined in red.", ^ "Fixes and Airways need fixing before the run.", in "click each tab with a red dot", = "NAVAIDs has unsaved changes, marked with an amber dot in the list on the left.\nThey are saved before the run starts."
- ServiceRunReviewTabViewModelTests.a_message_group_copies_... = "Copy these 2 messages", "Copy this message"

**Settings: ImportSettingsViewModel / SettingsProfilesViewModel / ProfileNameViewModel / DevLaunchCardViewModel / ResetViewModel / UninstallViewModel / UpdateWindowViewModel**
- ImportSettingsViewModelTests: a_new_profile_named_after_... = "ZOB gets the file's settings.", "zob.json, exported from FE-Buddy v3.1.0 on 7 Oct 2026.", NewProfileToolTip "This makes a new profile with the imported profile data.\nName it as you see fit.", "Add them to Default", MergeToolTip "Where both the Import and Default have settings, the Import wins.\nOtherwise, Default fills in the empty settings.", "Replace Default's settings with them", ReplaceToolTip "Similar to \"Add them to Default\", except empty settings in the Import are left empty and not filled with those in Default."; the_name_must_be_usable_... = "The new profile gets the file's settings.", "There is already a profile called ZOB."; merge_and_replace_... ^ "1 setting changes in Default.", in "Unsaved changes on Settings will be lost.", $ "Default's settings as they are now are kept in UserConfig-before-import.Default.json in case you want them back.", ^ "2 settings change in Default."; nothing_to_change_... = "Nothing changes: Default already has these settings."; the_summary_names_versions_and_folders in "This PC runs FE-Buddy v3.1.0, the file came from v3.0.0.", "Folders and files:\n  • ", "Not taken, as they do not work on this PC:", "  • …and 2 more"; a_plain_file_... = "UserConfig.json, a settings file from another PC.", "zob.json."
- SettingsProfilesViewModelTests: the_card_lists_the_profiles_... = "Saved in `{path}`."; choosing_a_profile_switches_to_it toast "Settings profile switched"; unsaved_edits_are_asked_about_... = "Switch settings profile|Settings and Airways have unsaved changes, which will be lost. Save them first to keep them.|Switch to ZNY"; a_profile_that_has_gone_... toast "Could not switch settings profile"; new_makes_a_profile_... = "New Settings Profile", "Unsaved changes on Settings will be lost as FE-Buddy switches to it."; delete_switches_away_then_deletes ^ "Delete settings profile|Delete the ZNY profile? FE-Buddy switches to Default"; a_new_name_must_be_usable_and_not_taken = "There is already a profile called ZNY.", "A profile name can't contain \ / : * ? \" &lt; > |.", "Start with a copy of Default's settings"
- DevLaunchCardViewModelTests: a_dev_launch_shows_every_step_ticked_as_saved = "Parse AIRAC data", "Download News"; choices_that_cannot_be_saved_... ^ "Couldn't save these choices:"
- ResetViewModelTests.deleting_the_credentials_is_in_the_request = "Saved Credentials (3)"; every_reset_lists_what_always_goes pins the count 4
- UninstallViewModelTests: in-list "Your settings and their backups", "Your 3 saved credentials, from Windows Credential Manager", "Your saved credential, from Windows Credential Manager"; every_uninstall_lists_what_it_leaves in "output folder", in "other people's Windows accounts"; counts 4 / 2 / 2
- UpdateWindowViewModelTests: "Go back to the latest Stable release", "Go back to", "Go back now", in "may not carry over", "Open release page", "Update available", "Latest", "Update now", in "your settings are kept"

**InfoViewModel / AliasGuideViewModel (Core guide text)**
- InfoViewModelTests.the_first_two_cards_are_the_featured_... = "What's New in v3.0?", "Alias Command Guide" (version-bound)
- AliasGuideViewModelTests: the_page_shows_the_guide_the_export_writes in "every AIRAC cycle"; with_the_help_command_off_... in-list "Your facility may have given it another name, or left it out."

</details>
