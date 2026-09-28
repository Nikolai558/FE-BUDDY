# FAQ and troubleshooting

## Common questions

### How often do I need to run FE-Buddy?

Once per AIRAC cycle - every 28 days. The Dashboard shows the next cycle's date and how many days
away it is. You can prepare the next cycle's files early: pick **Next** on the General tab once
the FAA has published it (a few weeks before it takes effect).

### Where are my files?

In an `AIRAC_<cycle>` folder (for example `AIRAC_2610`) in your output folder: Settings ▸
**Default Output Directory**, inside a `FE-Buddy_Output` folder if that option is on. GeoJSON
files are in its `Geojson` folder and alias files are in its `Aliases` folder. GeoJSON you marked
for vNAS is in its `Upload_to_vNAS` folder instead, and the alias files you marked for vNAS are
merged into `Upload_to_vNAS\vNAS_Alias.txt` - the one alias file to upload. After a run, **Open output folder** on
the Review tab takes you straight there. The [user guide](User-Guide.md#output-files) shows the
full folder layout.

### FE-Buddy says the cycle has already been run

The cycle's `AIRAC_<cycle>` folder already has files from an earlier run. Pick **Overwrite files**
(the default) to write over them - any old file this run does not write stays - or **Delete all
files** to empty the folder first so it holds only this run's files. Deleted files do not go to
the Recycle Bin. **Cancel** stops the run.

### Why does Windows show a different version number for FE-Buddy?

In Windows **Settings ▸ Apps ▸ Installed apps**, FE-Buddy shows a number that does not match its
real version. That is expected. Windows can only store plain numbers like `3.0.12`, but FE-Buddy
versions can include a tag like `3.0.0-alpha.1`, so the installer gives Windows a separate
counter. The real version is at the top of FE-Buddy's window, and in `FE-BUDDY.exe` ▸ Properties
▸ Details ▸ **Product version**. (The details, for the curious:
[MSI version numbering](../Developers/MSI-VERSION-NUMBERING.md).)

### Which update channel should I use?

**Stable**, unless a developer asks you to test something. Release Candidate adds versions that
are in a final round of testing; Beta and Alpha get earlier versions that may be broken.

### Does FE-Buddy send my data anywhere?

No. It downloads the FAA's public data, checks GitHub for updates and news, downloads any custom
alias file you gave a web address for, and writes files on your PC. Your settings stay in
`%APPDATA%\FE-Buddy\UserConfig.json`. A token or password you save in Settings ▸ Credentials is
kept in Windows Credential Manager, and only ever sent to the websites you allow it for.

### I set FEBUDDY_GITHUB_TOKEN for FE-Buddy 2.x

FE-Buddy 3 doesn't use it. It keeps tokens in Windows Credential Manager, encrypted with your
Windows sign-in, while an environment variable is plain text that any program you run can read.
Delete it:

1. Search the Start menu for **environment variables** and open *Edit environment variables for
   your account*.
2. Select `FEBUDDY_GITHUB_TOKEN` under *User variables*, press **Delete**, then **OK**. If it was set
   for everyone on the PC, it is under *System variables* instead, which needs an administrator:
   open *Edit the system environment variables*.
3. If you no longer need the token, delete it on GitHub too (Settings ▸ Developer settings ▸
   Personal access tokens).

To have FE-Buddy use a GitHub token, add it in Settings ▸ Credentials and choose it under
FE-Buddy's GitHub Requests. When FE-Buddy starts, it tells you once if the variable is still set -
it looks for the name only and never reads the token.

### What happens if Wx Stations or Telephony can't download their data?

Wx Stations' station list and Telephony's FAA pages aren't part of the AIRAC cycle, so every AIRAC
Service run that includes either downloads the latest copy first, whichever cycle you run. If that
download fails, FE-Buddy falls back to its last kept copy, and the Review tab shows an advisory
warning naming its date and how old it is. If FE-Buddy has no copy at all yet - your very first run,
or no internet connection - that sub-service writes nothing and the Review tab shows an error, but
the rest of the run still completes. Telephony's U.S. special call signs page is optional: without
a copy of it, Telephony still writes `Telephony.txt` from the ICAO register alone, with a warning.

### Why does the same alias command show up in both Departures.txt and Arrivals.txt?

For the cycle effective 2026-09-03, the FAA's data lists ORF's NUTIY and SWOPE departures as STARs
too, so FE-Buddy writes `.orfNUTIYf` and `.orfSWOPEf` into both alias files. Their GeoJSON files
do not clash - the Arrivals ones carry `STAR` in the name - but if you load both alias files, each
of those two commands is defined twice. This comes from the FAA data, and FE-Buddy leaves it as it
is for now - you will see the same pair flagged in `Duplicate_Alias_Commands.txt` after the run,
alongside any other duplicate alias commands that cycle happens to have.

### Do I still need FE-Buddy 2.x?

Only for the tools 3.0 does not have yet: ISR aliases, SCT2 to DXF, vSTARS and FAA GeoMap
conversions, GeoJSON clean-up and procedure ISRs. (FAA `.dat` video maps, SCT2 sector files and
ERAM Geomaps already convert to GeoJSON in 3.0, on the File Conversions screen, and FAA Chart
Recall aliases are part of the Procedures sub-service.) Installing 3.0 replaces 2.x, so if you
still need those, hold off upgrading for now.

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
fill every box shown, or take CRC-ERAM defaults off those files on the **Upload to vNAS** card.

### My GeoJSON files have no CRC ERAM defaults

CRC-ERAM defaults are only written into files marked for vNAS - CRC reads its maps from vNAS, so
anywhere else they would never be used. Tick the file on the **Upload to vNAS** card, then choose
which of those files get defaults.

### A file I expected is missing

Look at **Advisories** and **Results** on the Review tab. A file is not written when nothing
matched: for example a Region of Interest that contains no airports, or an ARTCC filter that
excludes everything. An airway is left out entirely when one of its waypoints could not be
located; the Review tab names it.

### My custom alias file on GitHub can't be read

The run leaves that file out of `vNAS_Alias.txt` and says why on the Review tab - don't upload the
file until it's fixed, or that file's aliases disappear from vNAS. Press **Check** on the vNAS Alias
Upload tab to try again straight away. [Creating a GitHub token for FE-Buddy](GitHub-Token-Guide.md)
walks through the token settings step by step. The usual reasons:

- **GitHub could not find it (404).** Check the address is the file's own page (with `/blob/` in
  it) or its Raw link. A private repository looks missing without a token: choose a GitHub token
  for the file in its **Credential** box (add one in Settings ▸ Credentials, or with **New
  credential…**).
- **GitHub refused the credential (401).** The token is mistyped, expired or revoked. Make a new
  one on GitHub and edit the credential in Settings ▸ Credentials.
- **GitHub does not let the credential read it (403).** A fine-grained token must include this
  repository, with *Contents: Read-only*. With no token at all, a 403 can also mean GitHub's limit
  on downloads without a token was reached - choose a token, or try again in an hour.
- **GitHub needs the credential authorized for single sign-on (403).** The repository's
  organization uses SAML single sign-on: on GitHub, authorize the token for the organization.
- **GitHub is limiting how often it can be asked.** Too many requests in a short time - wait a few
  minutes and try again.
- **This is a GitHub page, not a file.** The address is a repository or folder page on GitHub.
  Open the alias file on GitHub and copy that page's address (with `/blob/` in it).
- **… sent a web page, not an alias file.** The website sent a page for people to read - a
  sign-in page, say, or a file's page on a website other than GitHub - instead of the file itself.
  Use the address of the file itself (on most websites, its "raw" or download link).

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
