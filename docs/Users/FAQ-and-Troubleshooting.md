# FAQ and troubleshooting

## Questions

### How often do I run FE-Buddy?

Once per AIRAC cycle - every 28 days. The Dashboard shows when the next one starts. Once the FAA
publishes it, a few weeks early, you can pick **Next** on the General tab and get ahead.

### Where are my files?

In an `AIRAC_<cycle>` folder (such as `AIRAC_2610`) in your output folder - Settings ▸ Default
Output Directory. **Open output folder** on the Review tab goes straight there. See
[Output files](User-Guide.md#output-files).

### Why does Windows show a different version number?

Windows can only store plain numbers like `3.0.12`, not `3.0.0-alpha.1`, so the installer gives it a
separate counter. The real version is at the top of FE-Buddy's window.

### Does FE-Buddy send my data anywhere?

No. It downloads public data (the FAA's, aviationweather.gov's and, if you choose it, VATSIM-Radar's
virtual airline list), asks a public time service for the current UTC time, checks GitHub for updates
and News, and downloads any custom alias file you point it at. Your settings stay in
`%APPDATA%\FE-Buddy\UserConfig.json`. A saved token or password lives in Windows Credential Manager
and is only sent to the websites you allow.

### I set FEBUDDY_GITHUB_TOKEN for FE-Buddy 2.x

FE-Buddy 3 never uses it, and Windows keeps it as plain text any program can read, so delete it: the
first time FE-Buddy finds it, it offers to open Environment Variables. If you no longer need the
token, delete it on GitHub too. To give FE-Buddy 3 a token, use Settings ▸ Credentials.

### What happens if Wx Stations or Telephony can't download their data?

Every run downloads the latest. If that fails it uses FE-Buddy's last copy, and the Review tab says
how old it is. With no copy at all (a first run offline) that sub-service writes nothing; the rest of
the run goes on.

### Why is the same command in Departures.txt and Arrivals.txt?

Since cycle 2609 the FAA's data lists ORF's NUTIY and SWOPE departures as STARs too, so
`.orfNUTIYf` and `.orfSWOPEf` are in both files. `Duplicate_Alias_Commands.txt` lists them with any
other duplicates. To pick which line keeps a command once and for all, see Duplicate Alias Commands
on the [Preview Settings tab](User-Guide.md#preview-settings-tab).

### Do I still need FE-Buddy 2.x?

Only for what 3.0 doesn't do yet:

- the VRC, vSTARS and vERAM files 2.x's AIRAC data made (3.0 makes CRC files only);
- DAT to SCT2;
- SCT2 to and from DXF, KML and FAA GeoMap XML;
- vSTARS and vERAM video maps to GeoJSON.

Installing 3.0 replaces 2.x, so hold off if you need those.

## Something is wrong

### The AIRAC Service is waiting for AIRAC data

It's still downloading - the status at the top says what it's doing. If the current cycle
**failed**, check your internet connection and restart FE-Buddy.

### The next cycle says "not yet published"

The FAA hasn't released it. It usually appears a few weeks early; FE-Buddy checks at every launch.

### A tab is red and I can't run

Click each tab with a red dot: the red box at its top lists what to fix, and the cards to fix are
outlined in red.

### The cycle says "partial"

The FAA hasn't posted the cycle's d-TPP Metafile yet, so Procedures writes nothing for it;
everything else runs. Hover the cycle for when to expect it - FE-Buddy looks for it at every launch.

### My GeoJSON files have no CRC ERAM defaults

In the AIRAC Service, none get them to start with: on the tab's **CRC ERAM Defaults** card, choose
every GeoJSON file or specific ones, and fill in the boxes. In DAT and SCT2 to GeoJSON, tick
**Include** on the card. In ERAM to GeoJSON, choose a **CRC ERAM Defaults Source**.

### A file I expected is missing

Look at **Advisories** on the Review tab. A file isn't written when nothing matched - a region with
no airports, say. An airway with a waypoint that can't be located is left out of everything; its
warning is under **Results** ▸ Airways.

### My custom alias file can't be read

The run leaves it out of `Combined_Alias.txt` and the Review tab says why - don't upload until it's
fixed. **Check** on the Concatenate Aliases tab says what's wrong and, for GitHub, what to do next.
GitHub's messages are explained in [If something goes wrong](GitHub-Token-Guide.md#if-something-goes-wrong).

### The installer won't replace what's installed

It won't put an older stable version over a newer one. To leave a pre-release (alpha, beta or rc) for
the latest stable, choose **Stable** in Settings ▸ Updates, **Save**, then **Go back now**.

### FE-Buddy crashed

Please attach these when you [report it](https://github.com/Nikolai558/FE-BUDDY/issues):

- `%APPDATA%\FE-Buddy\Logs\febuddy-wpf-crash.txt` - the crash.
- `%APPDATA%\FE-Buddy\Logs\FE-Buddy_<date>.log` - everything FE-Buddy did that day (kept 30 days).

### I want to start fresh

Use Settings ▸ **Reset FE-Buddy…**, or close FE-Buddy and delete
`%APPDATA%\FE-Buddy\UserConfig.json`.

## Still stuck?

Ask on the FE-Buddy Discord (the link is on the Dashboard) or open an
[issue](https://github.com/Nikolai558/FE-BUDDY/issues).
