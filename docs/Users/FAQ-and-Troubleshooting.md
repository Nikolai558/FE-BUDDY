# FAQ and troubleshooting

## Common questions

### How often do I need to run FE-Buddy?

Once per AIRAC cycle - every 28 days. The Dashboard shows the next cycle's date and how many days
away it is. You can prepare the next cycle's files early: pick **Next** on the General tab once
the FAA has published it (a few weeks before it takes effect).

### Where are my files?

In your output folder: Settings ▸ **Default Output Directory**, inside a `FE-Buddy_Output` folder
if that option is on. After a run, **Open output folder** on the Review tab takes you straight
there. The [user guide](User-Guide.md#output-files) shows the full folder layout.

### Why does Windows show a different version number for FE-Buddy?

In Windows **Settings ▸ Apps ▸ Installed apps**, FE-Buddy shows a number that does not match its
real version. That is expected. Windows can only store plain numbers like `3.0.12`, but FE-Buddy
versions can include a tag like `3.0.0-alpha.1`, so the installer gives Windows a separate
counter. The real version is at the top of FE-Buddy's window, and in `FE-BUDDY.exe` ▸ Properties
▸ Details ▸ **Product version**. (The details, for the curious:
[MSI version numbering](../Developers/MSI-VERSION-NUMBERING.md).)

### Which update channel should I use?

**Stable**, unless a developer asks you to test something. Beta and Alpha get early versions that
may be broken.

### Does FE-Buddy send my data anywhere?

No. It downloads the FAA's public data, checks GitHub for updates and news, and writes files on
your PC. Your settings stay in `%APPDATA%\FE-Buddy\UserConfig.json`.

### Do I still need FE-Buddy 2.x?

Only for the tools 3.0 does not have yet: chart-recall and ISR aliases, SCT2 to DXF and GeoMap
conversions, GeoJSON clean-up and procedure ISRs. (FAA `.dat` video maps and SCT2 sector files
already convert to GeoJSON in 3.0, on the File Conversions screen.) Installing 3.0 replaces 2.x, so if you still need those, hold
off upgrading for now.

## Something is wrong

### The AIRAC Service says it is waiting for AIRAC data

FE-Buddy is still downloading or reading the FAA data - watch the status at the top of the window.
The first launch takes the longest. If it says the current cycle **failed**, check your internet
connection and restart FE-Buddy; it tries the download again at every launch.

### The next cycle says "not yet published"

The FAA has not released it yet. It usually appears a few weeks before its effective date; FE-Buddy
checks again every time it starts.

### I can't run: a tab is red

A red tab has a setting that is missing or invalid. Open it: the field is outlined in red, and
hovering it tells you what is wrong. The most common one is an empty box in **CRC ERAM Defaults**:
fill every box on the panel, or untick **Include** on that panel.

### A file I expected is missing

Look at **Advisories** and **Results** on the Review tab. A file is not written when nothing
matched: for example a Region of Interest that contains no airports, or an ARTCC filter that
excludes everything. An airway is left out entirely when one of its waypoints could not be
located; the Review tab names it.

### The installer says this version cannot replace what's installed

You are installing an **older stable** version over a newer one, which the installer blocks so a
stable install never silently goes backwards. If you are on a pre-release (alpha, beta or rc),
going back to the latest stable **is** allowed: use Settings ▸ Updates ▸
**Get the latest stable installer**.

### FE-Buddy crashed

FE-Buddy writes what happened to two places. Please attach them when you
[report the problem](https://github.com/Nikolai558/FE-BUDDY/issues):

- `%TEMP%\febuddy-wpf-crash.txt` - the crash itself.
- `%APPDATA%\FE-Buddy\Logs\FE-Buddy_<date>.log` - everything FE-Buddy did that day (logs are
  kept for 30 days).

(Paste those paths into the File Explorer address bar to open them.)

### I want to start fresh

Close FE-Buddy and delete (or rename) `%APPDATA%\FE-Buddy\UserConfig.json`. FE-Buddy starts with
default settings next time. The downloaded FAA data (`%APPDATA%\FE-Buddy\AiracCycles`) can be
deleted too; it is downloaded again.

## Still stuck?

Ask on the FE-Buddy Discord (link on the Dashboard) or open an issue on
[GitHub](https://github.com/Nikolai558/FE-BUDDY/issues).
