# FE-Buddy 3.0: What's New

FE-Buddy 3.0 is a ground-up rewrite.

Same job: turning each AIRAC cycle into CRC video maps and alias files. Now with a modern interface, far more control, and cleaner data.

The goal: no outside scripts needed to get your files CRC-ready.

It would take too long to explain every difference, but we can give you a quick look at the important stuff.

## At a Glance

| Subject | 2.x | 3.0 |
|---|---|---|
| Graphical User Interface (GUI) | Looks like the 1900s | Major modern facelift |
| Output files include... | Everything; the FE has no control | Only what **you** pick, with more control over format and filters |
| Output files include... | VRC, vSTARS and vERAM files | Only CRC files, or those that may actually be useful |
| Map coverage | Whole country | **Your** Region of Interest |
| vNAS upload preparation | Assembled by hand or with external scripts | Output to an `Upload_to_vNAS` folder with **your** own CRC ERAM defaults, file names, etc. |
| Visualizing maps | Outside tools, and only after the output is complete | Built-in map that can visualize **before** you run the AIRAC Service |
| Sharing your preferences | Uh... what preferences?! | Export/import your preferences and settings |
| Merging FEB alias commands with your custom commands | Not available | Not only available, but it can reach out to your GitHub repo for the current file(s), even a private repo (using your GitHub token) |
| Duplicate commands | Many | Almost none |
| IAP type syntax | Many variables to mentally parse | 8 types, each with a `*/DME` and a `* BC` (back course) variant |
| DP/STAR/IAP syntax with no computer code | A mix of the first letter of each word in the base name, or its first 5 characters | Simply writes out the full base name |
| d-TPP procedures (changes) | Limited output options; not context-aware | Monitor changes for chosen facilities, approaches, airports, or any combination of those |
| d-TPP procedures (file types) | Simple `.txt` files | Procedures: `.json`; changes: `.md` |
| ISR alias commands | Limited data; no handling for duplicates | Significantly more information, handles duplicates in the data, and makes better use of CRC's `\s` and `\t` formatting |

## For Facility Engineers

- **Pick what you need.** 11 sub-services, each with its own options.
- **Region of Interest.** Draw one box and your maps stay in your area. Commands like `.apt`, `.nav` and chart recalls still cover the whole FAA.
- **vNAS-ready.** Mark files for upload. GeoJSON can carry your CRC ERAM defaults; alias files merge with your facility's own (local or private GitHub) into one `vNAS_Alias.txt`. Your commands win.
- **Procedure changes.** `Procedure_Changes.md` lists New, Changed and Deleted charts with links.
- **Set once.** Settings are remembered. Preview shows what will happen; Review shows what did. Share your settings with others.

## Better Maps

- **Airways:** one symbol and label per point, split High/Low or by designation, adjustable waypoint buffer.
- **SIDs/STARs:** lines, fix symbols and labels per airport. Shared segments drawn once.
- **Runways:** more of them, water runways included.
- **NAVAIDs:** every FAA type, one file or one per type.
- **Fixes and ARTCC Boundaries:** split by use, chart, altitude or ARTCC.

## Better Aliases

- `.apt`, `.nav` and `.id` show clean, labeled cards. Airports add longest runway, pattern altitude, airspace and its hours, FSS, CTAF, attended hours and more.
- A NAVAID or airline identifier shared by several is one command listing every match.
- Telephony adds U.S. special call signs (`.idNASA`).
- FE-Buddy can pick up your own custom alias file(s) stored on your PC and combine them with its own into a single, ready-for-vNAS-upload file. It can even reach out to your private GitHub repo for the most current version.

## Chart Recall Changes

Reworked so commands are, on average, one character longer, but they are easier for new controllers to learn, take less mental parsing for seasoned controllers, have no duplicates, and cover more relevant procedures.

**Multi-page charts:** The page number (2 or higher) now goes after the trailing `c`.

| Old | New |
|---|---|
| `.dtwCLVIN2c` | `.dtwCLVINc2` |

**Procedures without a computer code** (visual approaches, departures, arrivals): Commands now use the full base name, dropping "VISUAL", "RWY", "(OBSTACLE)", spaces, special characters, etc.

| Procedure | Old | New |
|---|---|---|
| SOUTH RIVER VISUAL RWY 19 (fictional AAA airport) | `.aaavSOUTH19c` | `.aaavSOUTHRIVER19c` |
| TURNAGAIN EIGHT (ANC) | `.ancTURNAc` | `.ancTURNAGAINc` |

**Instrument approaches:** Runway numbers are no longer shortened.

| Procedure | Old | New |
|---|---|---|
| ILS RWY 16R | `I6R` | `I16R` |

### IAP Type Codes

We trimmed the list of IAP types to those in common use throughout the FAA.

**Base types:**

| Type | Code |
|---|---|
| RNAV | R |
| ILS | I |
| LOC | L |
| VOR | O |
| NDB | N |
| LDA | D |
| GPS | G |
| TACAN | T |

**`*/DME` variants:** Add `D` to the base IAP type code.

| Type | Code |
|---|---|
| LOC/DME | LD |
| VOR/DME | OD |
| NDB/DME | ND |
| LDA/DME | DD |

**Back course variants:** Add `BC` to the base IAP type code.

| Type | Code |
|---|---|
| Back course | Type code + BC (e.g., LOC BC = `LBC`) |

**Same-runway approach variants (X, Y, Z...):** Handled the same as in 2.x.

### New Command Examples

| Airport | Procedure | Chart Recall Command | Note |
|---|---|---|---|
| DTW | ILS Z OR LOC RWY 04L | `.dtwIZ04Lc` or `.dtwLZ04Lc` | The `Z` variant carries over to the LOC approach, even when the FAA doesn't label it that way. |
| LAX | RNAV (RNP) Z RWY 07R | `.laxRZ07Rc` | Whether it says (RNP) or (GPS) doesn't matter; it is still an RNAV approach, so its IAP type code is `R`. |
| LAX | RNAV (RNP) Z RWY 24L | `.laxRZ24Lc` | - |
| LAX | RNAV (GPS) Y RWY 07R | `.laxRY07Rc` | - |
| LAX | RNAV (GPS) Y RWY 24L | `.laxRY24Lc` | - |
| MRY | RACEWAY *charted* VISUAL RWY 28L | `.mryvRACEWAY28Lc` | `RACEWAY` is spelled out in full. |
| LGB | LA RIVER *charted* VISUAL RWY 12 | `.lgbvLARIVER12c` | `LA RIVER` is spelled out in full, with spaces (and any special characters) removed. |
| SFO | QUIET BRIDGE *charted* VISUAL RWY 28R | `.sfovQUIETBRIDGE28Rc` | - |
