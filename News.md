# FE-Buddy News
<!--
PostId format =  yyyy-mm-dd.#
  - Date/Time is always Zulu (GMT) time.
  - # = sequential number for the number of posts this day. The First post of the day is 1, while the third post is 3.
  - A new post's PostId must be later than every PostId already here. FE-Buddy marks a post as new
    only when its PostId is later than the newest one the user has already seen.
  - A post without a valid PostId (wrong format, or # of 0) is not shown in FE-Buddy.
  - Links can be relative to this file, which sits at the repository root: [User Guide](docs/Users/User-Guide.md).
-->

News concerning all things FE-Buddy will be posted here with the most recent post at the top.

---

## 2026-10-09
<!--
PostId: 2026-10-09.1
-->

**Version 3.0.0-beta.6 Compiled!**

Clearer filters on the AIRAC Service tabs

- **New Area card** on each tab: pick **ARTCCs**, **ROI**, or **Everything** (Procedures adds **None**). Only the one you pick applies
- For the ROI, use **the default ROI** or give the tab **its own**
- **What You'll Get with the Current Settings** now reads like a sentence: "Every arrival · **for** airports in ZOB · **and only** those amended this cycle"
- **Procedures**: the airports and procedures you list are added even outside the area you pick
- Your settings carry over. A tab that used its ARTCCs and an ROI together starts on its ARTCCs

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-beta.6)!!!

---

## 2026-10-08
<!--
PostId: 2026-10-08.1
-->

**Version 3.0.0-beta.5 Compiled!**

A hotfix for Telephony's virtual airlines

- **Windows 10**: the virtual airline list downloads again
- The list now comes straight from its two sources: **GNG** and **VATSIM-Radar's GitHub**. Each is saved and falls back on its own copy, and runs download each one at most once a day (**Download the latest list** gets them now)
- **One card for each 3LD and telephony**: a virtual airline with the same 3LD and telephony as a real operator (`AAL AMERICAN`) is left out. The Review tab names them, and the Telephony tab flags yours
- If part of the list can't be downloaded, the tab says so in amber, with the reason
- When a download fails, FE-Buddy now gives the underlying cause (a TLS error, say), not just "see inner exception"

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-beta.5)!!!

---

## 2026-10-07
<!--
PostId: 2026-10-07.1
-->

**Version 3.0.0-beta.4 Compiled!**

More from your beta feedback

- **No more uninstalling between versions**: your settings now carry over
- **Settings profiles**: keep a set of settings for each facility (or for testing) and switch at the top of Settings
- Every AIRAC Service card is tagged with the files it changes, and each tab starts with a **What You'll Get** summary
- **Duplicate alias commands**: have the run stop and let you pick which line keeps each one. Your choices are remembered
- **Ctrl + click** anything on the map to see its properties
- Fixes: the full VATSIM-Radar virtual airline list (about 700), `ALL` hours for full-time airspace, and a Systems box that flags a missing d-TPP Metafile

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-beta.4)!!!

---

## 2026-10-05
<!--
PostId: 2026-10-05.2
-->

**Version 3.0.0-beta.3 Compiled!**

More from your beta feedback

- **File Conversions** starts by asking what you have: pick the source and output, then **Continue** to its settings. Results show right under **Convert**
- **General** tab: untick a sub-service's last file and it's left out; tick any file and it's back in
- **Procedures ▸ Airports** takes a list: paste up to 100 airport IDs and press **Enter**

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-beta.3)!!!

---

## 2026-10-05
<!--
PostId: 2026-10-05.1
-->

**Version 3.0.0-beta.2 Compiled!**

Built from your beta.1 feedback

- **Uninstall FE-Buddy completely (removing its settings) before installing** - beta.1's settings aren't read
- The **General** tab is now one table: pick the sub-services and the files each one makes
- **Upload to vNAS is gone**: every file is ready for vNAS
- **Concatenate Aliases** builds the one `Combined_Alias.txt` to upload to vNAS
- Each tab lists its problems at the top and outlines the cards to fix in red
- Much smaller Symbols files, a new ERAM **Raw Plus** layout, and zoom shown as a percentage on the map
- Text is generally 10% larger and more readable

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-beta.2)!!!

---

## 2026-10-04
<!--
PostId: 2026-10-04.1
-->

**Version 3.0.0-beta.1**

- Small beta release
- Special thanks to Matthew Kramer & Dave Wagner for initial feedback!

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-beta.1)!!!

---

## 2026-10-03
<!--
PostId: 2026-10-03.1
-->

**Version 3.0.0-alpha.4 Compiled!**

- New **What's New in v3.0?** page under Info
- Export an **Alias Command Guide** for your controllers (web page or Markdown)
- Add your facility's **Virtual Airlines** to Telephony
- New look: FEB logo and a Natural Earth base map

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-alpha.4)!!!

---

## 2026-09-30
<!--
PostId: 2026-09-30.1
-->

**Version 3.0.0-alpha.3 Compiled!**

**On alpha.2?** It won't offer you alpha.3 until you open Settings ▸ Updates, choose **Alpha** and press **Save**.

- Choose how far airway lines stop short of fixes and NAVAIDs
- ERAM to GeoJSON writes the original ERAM_2_GEOJSON tool's layouts
- Your custom aliases now replace FE-BUDDY's in `vNAS_Alias.txt`
- **Update available!** badge, plus Reset and Uninstall in Settings
- Lots of smaller fixes and polish

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-alpha.3)!!!

---

## 2026-09-29
<!--
PostId: 2026-09-29.1
-->

**Version 3.0.0.alpha.2 Compiled!**

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-alpha.2)!!!

---

## 2026-09-28
<!--
PostId: 2026-09-28.1
-->

**Version 3.0.0.alpha.1 Compiled!**

Check it out [HERE](https://github.com/Nikolai558/FE-BUDDY/releases/tag/3.0.0-alpha.1)!!!

---

## 2026-09-26
<!--
PostId: 2026-09-26.1
-->

**News Page Created!**

Nothing to really see here, move along...
