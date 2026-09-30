# Preparing for FE-Buddy v3.0

FE-Buddy v3.0 is a ground-up rewrite with a new interface, far more control over output files, and more complete data for ISR commands and GeoJSON properties. The goal is simple: FEs should never need an external script or program to prepare files for CRC.

Though, we may never fully achieve that goal, it is worth chasing.

Below are the biggest changes, including the new chart recall syntax controllers will use.

## No Manual Required

A user guide, FAQ, and troubleshooting docs are available, but most users won't need them. Options include short descriptions and tooltips where helpful.

## Region of Interest

FEs can now define a Region of Interest (ROI), a geographic box that excludes most data outside it. This keeps vNAS storage focused on what your facility actually uses. Some data, such as the `.apt` ISR command, intentionally still covers the entire FAA database.

## AIRAC Service

You now control which files are produced, what they contain, and how they're formatted:

- Choose from a wide range of data types, each with its own options.
- Rename any output file.
- Mark any file for `Upload_to_vNas` to preload CRC ERAM default properties and merge your custom alias file(s) with FE-Buddy's.

FE-Buddy will even download your files from a place like GitHub using your personal tokens, if the repo is private and you allow.

If a custom alias command conflicts with an FE-Buddy command, CRC uses yours.

## Chart Recall Changes

**Multi-page charts:** The page number (2 or higher) now goes after the trailing "C".

| Old | New |
|---|---|
| `.dtwCLVIN2c` | `.dtwCLVINc2` |

**Procedures without a computer code** (visual approaches, departures, arrivals): Commands now use the full base name, dropping "Visual," "Rwy,", "(OBSTACLE)", spaces, special characters, etc...

| Procedure | Old | New |
|---|---|---|
| SOUTH RIVER VISUAL RWY 19 (fictional AAA airport) | `.aaavSOUTH19c` | `.aaavSOUTHRIVER19c` |
| TURNAGAIN EIGHT (ANC) | `.ancTURNAc` | `.ancTURNAGAINc` |

**Instrument approaches:** Runway numbers are no longer shortened.

| Procedure | Old | New |
|---|---|---|
| ILS RWY 16R | `I6R` | `I16R` |

It's slightly more typing, but commands are easier for new controllers to learn, less mental parsing for you, duplicates are gone, and more procedures are now covered.

## IAP Type Codes

We trimmed the list of IAP types to those in common use throughout the FAA:

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
| LOC/DME | LD |
| VOR/DME | OD |
| NDB/DME | ND |
| LDA/DME | DD |
| Back course | Type code + BC (e.g., LOC BC = `LBC`) |

- Back course commands are only created for supported types.
- Multi-character codes are used only for DME and back course variants of supported types.
- All RNAV approaches use "R", regardless of what's in parentheses.