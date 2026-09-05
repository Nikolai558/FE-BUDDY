# FE-Buddy AWY GeoJSON Generator
# Stage 0 Regression Test Plan

## Purpose

This document defines the Stage 0 regression-test plan for the existing `AwyGeojsonGenerator` before Stage 1 implementation begins.

The objectives of Stage 0 are to:

1. Protect the current AWY GeoJSON behavior that is already working.
2. Establish reusable synthetic test data for later stages.
3. Record the expected behavior for the planned grouping, antimeridian, and waypoint-clipping features.
4. Keep the test suite green before Stage 1 begins.
5. Avoid using large real FAA CSV files for routine unit tests.
6. Make failures specific enough that it is clear which behavior changed.

Stage 0 should **not** implement any of the three new production features.

---

## Important Test-Suite Rule

The tests are divided into two categories.

### Category A - Baseline regression tests

These tests protect behavior that exists **before Stage 1**.

They should be written and passing before Stage 1 begins.

### Category B - Future feature contract tests

These tests document the required behavior for Stages 1 through 3.

They should be added to the test plan now, but they should **not be enabled as failing tests before the corresponding feature exists**.

When a feature stage begins, its contract tests should be added first or enabled first, then production code should be changed until those tests pass.

This keeps the Stage 0 test project green rather than intentionally filling it with failures for functionality that has not been implemented yet.

---

# Existing Unit Test Project

Current structure:

```text
UnitTests
│   UnitTests.csproj
│   Usings.cs
│
├───Handlers
│       CoordinateHandlerTests.cs
│
└───Library
    └───Models
            LocationTests.cs
```

The test framework was not included in the provided information, so this document intentionally uses framework-neutral test names and assertions.

The names are compatible with the general style used by xUnit, NUnit, or MSTest.

---

# Proposed Test Folder Structure

Do not create these files yet unless we agree to proceed with the test implementation.

```text
UnitTests
│   UnitTests.csproj
│   Usings.cs
│
├───Generators
│   └───NASR
│       └───AwyGeojson
│           │   AwyGeojsonGeneratorBaselineTests.cs
│           │   AwyGeojsonGeneratorGroupingTests.cs
│           │   AwyGeojsonGeneratorAntimeridianTests.cs
│           │   AwyGeojsonGeneratorClippingTests.cs
│           │
│           ├───Fixtures
│           │       AwyGeojsonTestDataBuilder.cs
│           │
│           └───Helpers
│                   GeoJsonTestReader.cs
│                   GeoJsonAssertions.cs
│
├───Handlers
│       CoordinateHandlerTests.cs
│
└───Library
    └───Models
            LocationTests.cs
```

## File responsibilities

### `AwyGeojsonGeneratorBaselineTests.cs`

Protects the existing generator behavior before Stage 1.

This is the most important Stage 0 file.

### `AwyGeojsonGeneratorGroupingTests.cs`

Stage 1 contract tests for:

- `OutputBy = HighLow`
- `OutputBy = Type`
- multiple output files
- `MaxAuthAlt` classification
- empty output groups

This file should not contain active failing tests until Stage 1 begins.

### `AwyGeojsonGeneratorAntimeridianTests.cs`

Stage 2 contract tests for antimeridian detection and cutting.

### `AwyGeojsonGeneratorClippingTests.cs`

Stage 3 contract tests for waypoint clipping.

### `Fixtures/AwyGeojsonTestDataBuilder.cs`

Creates very small in-memory `NasrCsvDataCollection` objects.

The builder should allow tests to create only the records required for that scenario rather than parsing actual FAA files.

Likely builder operations will eventually include concepts such as:

```text
AddAirwayBase(...)
AddAirwaySegment(...)
AddFix(...)
AddNavaid(...)
AddAirport(...)
Build()
```

The exact method signatures should match the actual NASR model constructors/properties when test implementation begins.

### `Helpers/GeoJsonTestReader.cs`

Reads generated GeoJSON into a structure suitable for assertions.

The tests should avoid comparing the entire serialized JSON string because:

- formatting can change,
- property ordering can change,
- indentation can change,
- geometry can remain correct even when serialization details change.

Tests should inspect the JSON structure and coordinate arrays.

### `Helpers/GeoJsonAssertions.cs`

Optional reusable assertions for:

- FeatureCollection type
- feature count
- geometry type
- coordinate count
- coordinate comparison with floating-point tolerance
- no zero-length line pieces
- no longitude jumps greater than 180 degrees when antimeridian splitting is enabled

---

# Test Isolation and Temporary Files

Every generator test that writes output should use a test-specific temporary directory.

Do not write tests to:

```text
C:\Users\...
```

or another developer-specific path.

Each test should:

1. Create a unique temporary directory.
2. Pass it as `OutputDirectory`.
3. Run the generator.
4. Inspect the generated file or files.
5. Delete the temporary directory during cleanup.

This prevents tests from:

- overwriting another test's files,
- depending on one developer's computer,
- requiring manual cleanup,
- interfering when tests run in parallel.

---

# Synthetic Test Data

The baseline suite should use small, obvious coordinate values.

Example waypoint set:

```text
AAA = latitude 40.0, longitude -100.0
BBB = latitude 41.0, longitude  -99.0
CCC = latitude 42.0, longitude  -98.0
DDD = latitude 43.0, longitude  -97.0
```

For antimeridian tests later:

```text
AMW1 = latitude 30.0, longitude -179.0
AMW2 = latitude 50.0, longitude  178.0

AME1 = latitude 30.0, longitude  179.0
AME2 = latitude 50.0, longitude -178.0
```

Identifiers should be chosen so the lookup rules in `FindWaypointCoordinates.GetCoordinates(...)` resolve them from the intended test data source.

If a five-character identifier would automatically cause a FIX lookup, the fixture should deliberately use that when a FIX result is desired.

When testing NAVAID or airport resolution, the fixture should use identifiers and/or explicit waypoint-type information that exercise the intended lookup path.

---

# Floating-Point Assertions

Latitude and longitude results should not normally be compared using exact floating-point equality after interpolation or great-circle calculations.

Tests should use an explicit numeric tolerance.

For ordinary unmodified coordinates, exact values may be appropriate if they are simply copied from the fixture.

For calculated coordinates, use an agreed tolerance.

The exact tolerance for Stage 2 and Stage 3 should be selected when those calculations are implemented.

---

# CATEGORY A
# Baseline Regression Tests Required Before Stage 1

These tests protect the existing generator behavior.

All tests in this section should pass before Stage 1 begins.

---

## B01 - Null NASR Collection Is Rejected

### Proposed test name

```text
Generate_WhenNasrDataIsNull_ThrowsArgumentNullException
```

### Protects

The current public argument validation.

### Setup

- `allNasrCsvData = null`
- valid settings dictionary

### Expected result

`ArgumentNullException` is thrown.

---

## B02 - Null Settings Dictionary Is Rejected

### Proposed test name

```text
Generate_WhenAirwaySettingsIsNull_ThrowsArgumentNullException
```

### Protects

`ParseSettings(...)` null validation.

### Setup

- valid `NasrCsvDataCollection`
- `airwaySettings = null`

### Expected result

`ArgumentNullException` is thrown.

---

## B03 - Missing Output Directory Setting Is Rejected

### Proposed test name

```text
Generate_WhenOutputDirectorySettingIsMissing_ThrowsArgumentException
```

### Protects

Required `OutputDirectory` behavior.

### Setup

Settings contain all currently required settings except:

```text
OutputDirectory
```

### Expected result

`ArgumentException` is thrown.

---

## B04 - Blank Output Directory Is Rejected

### Proposed test name

```text
Generate_WhenOutputDirectoryIsBlank_ThrowsArgumentException
```

### Setup

```text
OutputDirectory = "   "
```

### Expected result

`ArgumentException` is thrown.

---

## B05 - Missing OutputBy Setting Is Rejected

### Proposed test name

```text
Generate_WhenOutputBySettingIsMissing_ThrowsArgumentException
```

### Expected result

`ArgumentException` is thrown.

---

## B06 - Invalid OutputBy Setting Is Rejected

### Proposed test name

```text
Generate_WhenOutputByIsInvalid_ThrowsArgumentException
```

### Setup

```text
OutputBy = "SomethingElse"
```

### Expected result

`ArgumentException` is thrown.

---

## B07 - OutputBy Parsing Is Case Insensitive

### Proposed test name

```text
Generate_WhenOutputByUsesDifferentCase_AcceptsSetting
```

### Setup

Use a currently valid value such as:

```text
highlow
```

### Expected result

Settings parsing succeeds.

### Note

This protects the existing `OrdinalIgnoreCase` behavior.

---

## B08 - Missing Yes/No Settings Are Rejected

### Proposed test names

```text
Generate_WhenSplitAtAntimeridianIsMissing_ThrowsArgumentException
Generate_WhenWaypointBufferIsMissing_ThrowsArgumentException
```

### Expected result

Each missing required setting throws `ArgumentException`.

---

## B09 - Invalid Yes/No Settings Are Rejected

### Proposed test names

```text
Generate_WhenSplitAtAntimeridianIsNotYesOrNo_ThrowsArgumentException
Generate_WhenWaypointBufferIsNotYesOrNo_ThrowsArgumentException
```

### Example inputs

```text
SplitAtAntimeridian = "True"
WaypointBuffer = "1"
```

### Expected result

`ArgumentException` is thrown.

---

## B10 - Yes/No Settings Are Case Insensitive

### Proposed test name

```text
Generate_WhenYesNoSettingsUseLowerCase_AcceptsSettings
```

### Setup

```text
SplitAtAntimeridian = "y"
WaypointBuffer = "n"
```

### Expected result

Settings parsing succeeds.

---

## B11 - Missing AWY Parsed Data Is Rejected

### Proposed test name

```text
Generate_WhenAwyDataHasNotBeenParsed_ThrowsInvalidOperationException
```

### Setup

Use a `NasrCsvDataCollection` whose `Awy` value is `null`.

### Expected result

`InvalidOperationException` is thrown.

---

## B12 - Output Directory Is Created When It Does Not Exist

### Proposed test name

```text
Generate_WhenOutputDirectoryDoesNotExist_CreatesDirectory
```

### Setup

- generate a unique temporary path
- ensure the directory does not exist before calling the generator

### Expected result

The generator creates the output directory.

---

## B13 - One Valid Segment Produces One LineString

### Proposed test name

```text
Generate_WithOneValidAirwaySegment_WritesLineString
```

### Setup

Airway:

```text
AAA -> BBB
```

Coordinates:

```text
AAA = [-100.0, 40.0]
BBB = [ -99.0, 41.0]
```

One `AwyBase` record and one corresponding `AwySegAlt` record.

### Expected result

One GeoJSON feature exists for the airway.

Geometry:

```text
LineString
```

Coordinates:

```text
[-100.0, 40.0]
[ -99.0, 41.0]
```

---

## B14 - GeoJSON Uses Longitude Then Latitude

### Proposed test name

```text
Generate_WritesCoordinatesInLongitudeLatitudeOrder
```

### Protects

RFC 7946 coordinate ordering and the current NTS mapping:

```text
X = longitude
Y = latitude
```

### Setup

Use deliberately different latitude and longitude values so a reversal is obvious.

Example:

```text
latitude  = 41.25
longitude = -82.75
```

### Expected result

GeoJSON contains:

```text
[-82.75, 41.25]
```

not:

```text
[41.25, -82.75]
```

---

## B15 - Two Continuous Segments Become One LineString

### Proposed test name

```text
Generate_WithContinuousSegments_MergesThemIntoOneLineString
```

### Setup

```text
AAA -> BBB
BBB -> CCC
```

No gap flags.

### Expected result

One `LineString` with exactly:

```text
AAA
BBB
CCC
```

The shared point `BBB` appears once in the coordinate sequence.

---

## B16 - Explicit Airway Gap Starts a New Line Piece

### Proposed test name

```text
Generate_WhenSegmentIsMarkedAsGap_StartsNewLineString
```

### Setup

Two sequential segments:

```text
AAA -> BBB
BBB -> CCC
```

The second normalized segment is marked as an airway gap.

### Expected result

The airway is represented by multiple line pieces rather than one continuous coordinate chain.

Under the current implementation, this should result in a `MultiLineString`.

### Protects

Existing `AwySegGapFlag` behavior.

---

## B17 - Discontinuous Waypoint IDs Start a New Line Piece

### Proposed test name

```text
Generate_WhenAdjacentSegmentIdsAreDiscontinuous_StartsNewLineString
```

### Setup

```text
AAA -> BBB
CCC -> DDD
```

No explicit airway gap flag.

### Expected result

The two pieces are not joined.

Geometry should contain separate line pieces.

---

## B18 - Reference-Only Point Is Collapsed

### Proposed test name

```text
Generate_WhenIntermediateFromPointIsReferenceOnly_CollapsesReferencePoint
```

### Setup

Equivalent to:

```text
TIJ -> BORDER
BORDER -> TEYON
```

where the second record has an empty/null `FromPtType`.

`BORDER` should not be sent to normal coordinate resolution as a real airway waypoint.

### Expected result

Normalized geometry behaves as:

```text
TIJ -> TEYON
```

The artificial/reference-only `BORDER` point is not present in the generated coordinate list.

---

## B19 - Multiple Consecutive Reference-Only Points Are Collapsed

### Proposed test name

```text
Generate_WhenMultipleReferenceOnlyPointsAreConsecutive_CollapsesEntireChain
```

### Setup

Conceptually:

```text
AAA -> REF1
REF1 -> REF2
REF2 -> BBB
```

where the records beginning with `REF1` and `REF2` have empty/null `FromPtType`.

### Expected result

The normalized segment becomes:

```text
AAA -> BBB
```

---

## B20 - Gap Flag Is Preserved While Collapsing Reference-Only Records

### Proposed test name

```text
Generate_WhenCollapsedReferenceChainContainsGap_PreservesGap
```

### Setup

A reference-only chain where one of the collapsed `AwySegAlt` records has:

```text
AwySegGapFlag = "Y"
```

### Expected result

The resulting normalized segment retains gap behavior.

### Protects

The current normalization rule that a gap anywhere in the collapsed chain must not be lost.

---

## B21 - Record Missing FromPoint Is Ignored

### Proposed test name

```text
Generate_WhenSegmentFromPointIsBlank_DoesNotCreateGeometryForThatRecord
```

### Setup

Include an `AwySegAlt` record with blank/null `FromPoint`.

### Expected result

That record does not produce invalid geometry or crash normalization.

Valid other records continue to process.

---

## B22 - Record Missing ToPoint Is Ignored

### Proposed test name

```text
Generate_WhenSegmentToPointIsBlank_DoesNotCreateGeometryForThatRecord
```

### Expected result

That record does not create invalid geometry.

Valid other records continue to process.

---

## B23 - Unresolvable Trailing End Point Truncates Airway

### Proposed test name

```text
Generate_WhenTrailingEndWaypointCannotBeResolved_KeepsPreviouslyBuiltGeometry
```

### Setup

Conceptually:

```text
AAA -> BBB
BBB -> UNKNOWN_TRAILING
```

`AAA` and `BBB` resolve.

`UNKNOWN_TRAILING` does not resolve.

There are no later fully resolvable segments.

### Expected result

Previously valid geometry is retained.

The unresolved trailing portion is not emitted.

The entire airway does not fail.

---

## B24 - Unresolvable Trailing Start Point Truncates Airway

### Proposed test name

```text
Generate_WhenTrailingStartWaypointCannotBeResolved_KeepsPreviouslyBuiltGeometry
```

### Setup

A valid first portion followed by a trailing normalized segment whose starting waypoint cannot be resolved.

No later segment has both endpoints resolvable.

### Expected result

Previously built geometry remains.

Processing stops at the unresolved trailing portion.

---

## B25 - Unresolvable Internal Start Point Throws

### Proposed test name

```text
Generate_WhenInternalStartWaypointCannotBeResolved_ThrowsInvalidOperationException
```

### Setup

An unresolved segment appears inside the airway, and a later segment has resolvable start and end coordinates.

### Expected result

`InvalidOperationException` is thrown.

The exception should identify:

- airway ID
- unresolved start waypoint ID

---

## B26 - Unresolvable Internal End Point Throws

### Proposed test name

```text
Generate_WhenInternalEndWaypointCannotBeResolved_ThrowsInvalidOperationException
```

### Setup

An unresolved end waypoint occurs before later fully resolvable geometry.

### Expected result

`InvalidOperationException` is thrown.

The exception should identify:

- airway ID
- unresolved end waypoint ID

---

## B27 - Duplicate AWY_BASE IDs Do Not Cause Dictionary Failure

### Proposed test name

```text
Generate_WhenAwyBaseContainsDuplicateAirwayIds_DoesNotThrowDuringAirwayLookupCreation
```

### Setup

Two `AwyBase` records share the same `AwyId`.

### Expected result

The generator does not fail with a duplicate-key exception.

### Protects

The current:

```text
GroupBy(...).ToDictionary(...)
```

behavior.

---

## B28 - Airway IDs Are Matched Case Insensitively

### Proposed test name

```text
Generate_WhenAirwayIdCaseDiffersBetweenBaseAndSegments_MatchesAirway
```

### Setup

Example:

```text
AwyBase.AwyId    = "A216"
AwySegAlt.AwyId  = "a216"
```

### Expected result

The segment records are associated with the airway.

---

## B29 - Airway IDs Are Trimmed

### Proposed test name

```text
Generate_WhenAirwayIdContainsOuterWhitespace_MatchesTrimmedAirwayId
```

### Setup

Whitespace differences in `AwyId`.

### Expected result

Records still group under the same airway ID.

---

## B30 - Feature Contains feb_AWY-ID

### Proposed test name

```text
Generate_WritesFebAirwayIdProperty
```

### Expected result

Generated feature properties include:

```text
feb_AWY-ID
```

with the airway ID as its value.

---

## B31 - Airway With No Usable Geometry Produces No Feature

### Proposed test name

```text
Generate_WhenAirwayHasNoUsableGeometry_DoesNotWriteFeature
```

### Setup

An airway exists in `AwyBase`, but its segments normalize to nothing or no usable line geometry can be built.

### Expected result

No feature is written for that airway.

The generator still produces a valid GeoJSON FeatureCollection.

---

## B32 - Generated Document Is Valid GeoJSON FeatureCollection Structure

### Proposed test name

```text
Generate_WritesValidFeatureCollectionStructure
```

### Expected result

The output can be parsed as JSON and has:

```json
{
  "type": "FeatureCollection",
  "features": [...]
}
```

The test should inspect structure rather than compare the entire serialized string.

---

# Stage 0 Baseline Exit Criteria

Stage 1 should not begin until:

- all applicable B01-B32 tests are implemented,
- all applicable tests pass,
- test fixture data is independent of a developer's local FAA download folder,
- test output uses temporary directories,
- failures clearly identify the behavior that changed.

A specific baseline test may be omitted only if the actual current model/API makes the scenario impossible or redundant. If that happens, the reason should be documented in the test code or review notes.

---

# CATEGORY B
# Future Feature Contract Tests

These tests belong in the Stage 0 plan now but should be activated during their corresponding implementation stage.

---

# Stage 1 Contract Tests - Output Grouping

## Proposed production behavior used by these tests

When:

```text
OutputBy = HighLow
```

the generator creates:

```text
AIRWAYS_HIGH.geojson
AIRWAYS_LOW.geojson
AIRWAYS_OTHER.geojson
```

All three files are created even if one or more contain zero features.

The return value is expected to be:

```csharp
IReadOnlyDictionary<string, string>
```

or a closely related read-only dictionary type.

Suggested dictionary keys:

```text
HIGH
LOW
OTHER
```

with values containing the full generated file paths.

When:

```text
OutputBy = Type
```

the generator creates output groups dynamically from `AwyBase.AwyDesignation`.

Examples:

```text
J  -> AIRWAYS_J.geojson
V  -> AIRWAYS_V.geojson
AB -> AIRWAYS_AB.geojson
```

No hard-coded switch should be required for every possible valid designation.

---

## Important High/Low classification interpretation

The new `MaxAuthAlt` requirement should be treated as the altitude-stratum classification rule.

Proposed precedence for Stage 1 tests:

1. If the airway designation is not recognized as an allowed/known airway designation for High/Low classification, classify it as `OTHER`.
2. Otherwise examine **all** `AwySegAlt.MaxAuthAlt` values belonging to the airway ID.
3. If every value is `>= 18_000`, classify `HIGH`.
4. If every value is `> 0` and `< 18_000`, classify `LOW`.
5. If the values contain a mixture of the two ranges, classify `OTHER`.
6. If any value is `null`, classify `OTHER`.
7. If any value is `<= 0`, classify `OTHER`.

Before Stage 1 implementation, confirm whether this new altitude-based rule completely replaces the earlier designation-specific `J/V/RN/etc.` High/Low mapping for recognized designations, with the designation table serving only as the known-designation check.

This point does not block Stage 0 regression planning, but it should be locked before Stage 1 code is written.

---

## G01 - High When Every MaxAuthAlt Is At Least 18000

### Proposed test name

```text
Generate_HighLow_WhenAllMaximumAuthorizedAltitudesAreAtLeast18000_WritesAirwayToHigh
```

### Setup

One airway with several segments:

```text
18000
24000
45000
60000
```

### Expected result

Airway appears only in:

```text
AIRWAYS_HIGH.geojson
```

---

## G02 - Exactly 18000 Is High

### Proposed test name

```text
Generate_HighLow_WhenMaximumAuthorizedAltitudeIsExactly18000_ClassifiesAsHigh
```

### Expected result

`18_000` belongs to HIGH.

This explicitly protects the inclusive boundary.

---

## G03 - Low When Every MaxAuthAlt Is Positive and Below 18000

### Proposed test name

```text
Generate_HighLow_WhenAllMaximumAuthorizedAltitudesArePositiveAndBelow18000_WritesAirwayToLow
```

### Example values

```text
1
5000
17999
```

### Expected result

Airway appears only in:

```text
AIRWAYS_LOW.geojson
```

---

## G04 - Exactly 17999 Is Low

### Proposed test name

```text
Generate_HighLow_WhenMaximumAuthorizedAltitudeIs17999_ClassifiesAsLow
```

---

## G05 - Mixed High and Low Values Are Other

### Proposed test name

```text
Generate_HighLow_WhenMaximumAuthorizedAltitudesSpanHighAndLow_ClassifiesAsOther
```

### Example values

```text
17000
18000
45000
```

### Expected result

Airway appears only in:

```text
AIRWAYS_OTHER.geojson
```

---

## G06 - Any Null MaxAuthAlt Makes Airway Other

### Proposed test name

```text
Generate_HighLow_WhenAnyMaximumAuthorizedAltitudeIsNull_ClassifiesAsOther
```

### Example values

```text
18000
null
45000
```

---

## G07 - Zero MaxAuthAlt Makes Airway Other

### Proposed test name

```text
Generate_HighLow_WhenAnyMaximumAuthorizedAltitudeIsZero_ClassifiesAsOther
```

---

## G08 - Negative MaxAuthAlt Makes Airway Other

### Proposed test name

```text
Generate_HighLow_WhenAnyMaximumAuthorizedAltitudeIsNegative_ClassifiesAsOther
```

---

## G09 - Unknown Designation Is Other

### Proposed test name

```text
Generate_HighLow_WhenAirwayDesignationIsUnknown_ClassifiesAsOther
```

### Setup

Use a designation not present in the agreed known designation set.

Even if every `MaxAuthAlt` would otherwise qualify as High or Low.

### Expected result

Airway appears only in:

```text
AIRWAYS_OTHER.geojson
```

---

## G10 - Every Airway Appears in Exactly One HighLow File

### Proposed test name

```text
Generate_HighLow_WritesEachGeneratedAirwayToExactlyOneOutputFile
```

### Setup

Several airways covering High, Low, and Other.

### Expected result

No airway feature is duplicated between files.

No successfully generated airway is omitted.

---

## G11 - All Three HighLow Files Are Always Created

### Proposed test name

```text
Generate_HighLow_WhenAGroupHasNoFeatures_StillCreatesAllThreeFiles
```

### Setup

Data containing only High airways.

### Expected result

All exist:

```text
AIRWAYS_HIGH.geojson
AIRWAYS_LOW.geojson
AIRWAYS_OTHER.geojson
```

LOW and OTHER contain valid empty FeatureCollections.

---

## G12 - Empty Output Is Valid GeoJSON

### Proposed test name

```text
Generate_HighLow_WhenGroupIsEmpty_WritesEmptyFeatureCollection
```

### Expected result

The empty file parses as:

```json
{
  "type": "FeatureCollection",
  "features": []
}
```

---

## G13 - Return Dictionary Contains Every Generated HighLow File

### Proposed test name

```text
Generate_HighLow_ReturnsPathsForAllGeneratedFiles
```

### Expected result

The returned dictionary contains entries for:

```text
HIGH
LOW
OTHER
```

and every path exists.

---

## G14 - Type Output Groups Dynamically By Designation

### Proposed test name

```text
Generate_Type_GroupsAirwaysByDesignation
```

### Setup

Designations:

```text
J
V
AB
```

### Expected result

Files:

```text
AIRWAYS_J.geojson
AIRWAYS_V.geojson
AIRWAYS_AB.geojson
```

Each file contains only its corresponding designation.

---

## G15 - New Designation Does Not Require Hard-Coded Handling

### Proposed test name

```text
Generate_Type_WhenPreviouslyUnseenDesignationExists_CreatesDesignationFile
```

### Setup

Use a synthetic designation not otherwise represented by a specific production branch.

### Expected result

A corresponding output group/file is created dynamically.

### Note

Before Stage 1, decide how blank designations and characters invalid in Windows filenames should be handled.

---

## G16 - Type Grouping Normalizes Outer Whitespace

### Proposed test name

```text
Generate_Type_WhenDesignationContainsOuterWhitespace_UsesTrimmedDesignation
```

### Expected result

A value equivalent to:

```text
" J "
```

is grouped as:

```text
J
```

---

## G17 - Type Grouping Handles Case Consistently

### Proposed test name

```text
Generate_Type_WhenDesignationCaseDiffers_DoesNotCreateDuplicateLogicalGroups
```

### Setup

Equivalent logical designation values with case differences.

### Expected result

They belong to one logical output group.

The exact filename casing should be standardized during Stage 1.

---

# Stage 2 Contract Tests - Antimeridian

## Agreed mathematical model

For GeoJSON antimeridian cutting:

- detect a short-path longitude crossing when the absolute raw longitude difference is greater than 180 degrees,
- unwrap the end longitude to the equivalent longitude on the adjacent world copy,
- calculate the interpolation parameter at `+180` or `-180`,
- interpolate latitude using the same Cartesian segment parameter,
- terminate one line piece at one antimeridian representation,
- begin the next line piece at the equivalent longitude on the opposite side.

Do not use a simple average of waypoint latitudes unless the crossing occurs exactly halfway along the segment.

---

## A01 - Ordinary Segment Is Not Split

### Proposed test name

```text
Generate_Antimeridian_WhenLongitudeDifferenceIsBelow180_DoesNotSplitSegment
```

### Setup

```text
170 -> 175
```

### Expected result

One normal line piece.

---

## A02 - Exactly 180 Degrees Is Not Automatically Treated As Crossing

### Proposed test name

```text
Generate_Antimeridian_WhenLongitudeDifferenceIsExactly180_DoesNotAutomaticallySplit
```

### Protects

The agreed strict comparison:

```text
abs(deltaLongitude) > 180
```

rather than:

```text
>= 180
```

---

## A03 - West-Side to East-Side Crossing Is Split

### Proposed test name

```text
Generate_Antimeridian_WhenSegmentCrossesFromNegativeToPositiveLongitude_SplitsAtBoundary
```

### Example

```text
[-179, 30] -> [178, 50]
```

For short-path interpolation, `178` is unwrapped to `-182`.

Intersection occurs one third of the way from `-179` to `-182`.

Expected crossing latitude:

```text
30 + (50 - 30) * (1 / 3)
= 36.666666...
```

Expected conceptual pieces:

```text
[-179, 30]
[-180, 36.666666...]

[180, 36.666666...]
[178, 50]
```

This test deliberately uses an asymmetric crossing so an incorrect average-latitude implementation would fail.

---

## A04 - East-Side to West-Side Crossing Is Split

### Proposed test name

```text
Generate_Antimeridian_WhenSegmentCrossesFromPositiveToNegativeLongitude_SplitsAtBoundary
```

### Example

```text
[179, 30] -> [-178, 50]
```

Expected conceptual pieces:

```text
[179, 30]
[180, 36.666666...]

[-180, 36.666666...]
[-178, 50]
```

---

## A05 - Symmetric Example Produces Expected Midpoint Latitude

### Proposed test name

```text
Generate_Antimeridian_WhenCrossingOccursHalfway_ProducesHalfwayLatitude
```

### Example

```text
[-179, 30] -> [179, 50]
```

Expected crossing latitude:

```text
40
```

This is the case where averaging latitudes happens to be mathematically equivalent to the interpolation result.

---

## A06 - Endpoint Already At Positive 180 Does Not Create Zero-Length Piece

### Proposed test name

```text
Generate_Antimeridian_WhenEndpointIsPositive180_DoesNotEmitZeroLengthBoundaryPiece
```

### Expected behavior

If an endpoint is already physically on the antimeridian, the implementation should use the equivalent `+180` or `-180` representation that keeps the adjacent line local.

It should not manufacture an extra zero-length line piece solely to switch signs.

---

## A07 - Endpoint Already At Negative 180 Does Not Create Zero-Length Piece

### Proposed test name

```text
Generate_Antimeridian_WhenEndpointIsNegative180_DoesNotEmitZeroLengthBoundaryPiece
```

Same rule as A06.

---

## A08 - Segment Starting At Boundary Uses Local Equivalent Longitude

### Proposed test name

```text
Generate_Antimeridian_WhenSegmentStartsAtBoundary_UsesBoundarySignThatAvoidsWorldWrap
```

### Example concept

A physical start at the antimeridian followed immediately by a point near `-179`.

Expected representation should use:

```text
-180 -> -179
```

rather than:

```text
180 -> -179
```

which many viewers would draw around the world.

---

## A09 - Segment Ending At Boundary Uses Local Equivalent Longitude

### Proposed test name

```text
Generate_Antimeridian_WhenSegmentEndsAtBoundary_UsesBoundarySignThatAvoidsWorldWrap
```

Equivalent endpoint case.

---

## A10 - Multiple Crossings Produce Multiple Line Pieces

### Proposed test name

```text
Generate_Antimeridian_WhenAirwayCrossesBoundaryMultipleTimes_PreservesAllPieces
```

### Setup

An ordered airway that crosses the antimeridian, returns across it, and continues.

### Expected result

Every crossing is cut.

No valid downstream geometry is lost.

---

## A11 - Existing FAA Gap Plus Antimeridian Crossing Both Survive

### Proposed test name

```text
Generate_Antimeridian_WhenAirwayContainsGapAndBoundaryCrossing_PreservesBothBreakTypes
```

### Expected result

- FAA gap remains a route break.
- Antimeridian crossing creates serialization line pieces.
- Neither behavior overwrites the other.

---

## A12 - Splitting Disabled Preserves Uncut Segment

### Proposed test name

```text
Generate_Antimeridian_WhenSplittingIsDisabled_DoesNotCutCrossingSegment
```

### Setup

```text
SplitAtAntimeridian = "N"
```

### Expected result

The original endpoint coordinates are emitted without the new cutting transformation.

---

## A13 - Splitting Enabled Prevents Greater-Than-180 Longitude Jumps

### Proposed test name

```text
Generate_Antimeridian_WhenSplittingIsEnabled_NoEmittedLinePieceContainsLongitudeJumpGreaterThan180
```

### Expected result

For every pair of adjacent coordinates within each emitted line piece:

```text
abs(deltaLongitude) <= 180
```

This is a useful general invariant test.

---

## A14 - Boundary Cut Does Not Produce Duplicate Consecutive Coordinates

### Proposed test name

```text
Generate_Antimeridian_WhenCutOccurs_DoesNotEmitDuplicateConsecutiveCoordinatesWithinLinePiece
```

---

# Stage 3 Contract Tests - Waypoint Clipping

## Agreed behavior

- clipping distance units are nautical miles,
- clipping uses spherical great-circle calculations,
- clipping distance at each end is based on the waypoint data source/type at that end,
- `FindWaypointCoordinates.GetCoordinates(...)` returns:
  - latitude,
  - longitude,
  - source string (`fix`, `navaid`, or `airport`),
- `FromPtType` remains useful for distinguishing actual airway points from reference-only points,
- an antimeridian-crossing segment is not waypoint-clipped,
- clipping may naturally turn one airway into a `MultiLineString`,
- there is no one-nautical-mile minimum visible segment,
- if requested clipping leaves any real non-zero segment, keep the clipped result,
- if clipping would eliminate the segment entirely, leave that pair of points unmodified,
- a zero-length original segment should be omitted without preventing valid downstream geometry from being processed.

---

## Great-circle implementation detail to lock before Stage 3

A spherical model requires a documented Earth radius.

Before Stage 3 implementation begins, select one constant and use it consistently in both production code and tests.

A reasonable candidate is the IUGG mean Earth radius:

```text
6,371,008.8 meters
```

with:

```text
1 nautical mile = 1,852 meters
```

The Stage 0 plan does not require this choice to be finalized yet.

---

## C01 - Buffer Disabled Leaves Ordinary Segment Unchanged

### Proposed test name

```text
Generate_Clipping_WhenWaypointBufferIsDisabled_LeavesSegmentEndpointsAtWaypointCenters
```

---

## C02 - Fix To Fix Uses Fix Distance At Both Ends

### Proposed test name

```text
Generate_Clipping_WhenBothEndpointsAreFixes_UsesFixBufferAtBothEnds
```

### Setup

Choose an easily testable segment long enough that both buffers fit.

Use a known test configuration, for example:

```text
Fix buffer = 2.5 NM
```

### Expected result

Visible geometry starts 2.5 NM after the first FIX and ends 2.5 NM before the second FIX along the great-circle path.

---

## C03 - Fix To Navaid Uses Different Endpoint Distances

### Proposed test name

```text
Generate_Clipping_WhenSegmentConnectsFixAndNavaid_UsesEndpointSpecificBuffers
```

### Example test configuration

```text
FIX buffer    = 2.5 NM
NAVAID buffer = 3.0 NM
```

### Expected result

Start is moved 2.5 NM from the FIX.

End is moved 3.0 NM from the NAVAID.

---

## C04 - Navaid To Fix Applies Distances To Correct Ends

### Proposed test name

```text
Generate_Clipping_WhenSegmentConnectsNavaidAndFix_AppliesBuffersToCorrectEndpoints
```

This guards against accidentally applying a single segment-wide waypoint type.

---

## C05 - Airport Endpoint Uses Its Configured Category

### Proposed test name

```text
Generate_Clipping_WhenEndpointResolvesFromAirportData_UsesAirportOrOtherConfiguredBuffer
```

The exact configuration schema should be established before Stage 3.

---

## C06 - Very Short But Non-Zero Clipped Segment Is Retained

### Proposed test name

```text
Generate_Clipping_WhenRequestedBuffersLeaveVeryShortPositiveLength_KeepsClippedSegment
```

### Setup

Choose a segment whose length is only slightly greater than:

```text
startBuffer + endBuffer
```

Use a safe margin such as:

```text
0.01 NM
```

rather than an extremely tiny floating-point epsilon.

### Expected result

The remaining short line segment is emitted.

The implementation does not impose a one-mile minimum.

---

## C07 - Clipping That Exactly Eliminates Segment Preserves Original

### Proposed test name

```text
Generate_Clipping_WhenBuffersConsumeEntireSegment_PreservesOriginalSegment
```

### Setup

Segment length is effectively equal to:

```text
startBuffer + endBuffer
```

within the agreed geometric tolerance.

### Expected result

The pair is not clipped.

Original waypoint-to-waypoint segment is retained.

---

## C08 - Clipping Distances Greater Than Segment Preserve Original

### Proposed test name

```text
Generate_Clipping_WhenBuffersWouldOverlap_PreservesOriginalSegment
```

### Setup

```text
segmentLength < startBuffer + endBuffer
```

### Expected result

Original segment is retained.

No inverted or invalid line geometry is generated.

---

## C09 - Zero-Length Original Pair Is Omitted

### Proposed test name

```text
Generate_Clipping_WhenSegmentEndpointsHaveSameCoordinate_OmitsZeroLengthSegment
```

### Setup

```text
A -> B
coordinates(A) == coordinates(B)
```

### Expected result

No line piece is emitted for `A -> B`.

---

## C10 - Duplicate Coordinate Does Not Stop Downstream Geometry

### Proposed test name

```text
Generate_Clipping_WhenOnePairHasDuplicateCoordinates_ContinuesWithValidDownstreamSegment
```

### Setup

```text
A -> B
B -> C

coordinates(A) == coordinates(B)
coordinates(C) is different
```

### Expected result

- zero-length `A -> B` is omitted,
- valid `B -> C` geometry is emitted,
- the whole airway does not fail.

---

## C11 - Antimeridian-Crossing Segment Is Not Clipped

### Proposed test name

```text
Generate_Clipping_WhenSegmentCrossesAntimeridian_DoesNotApplyWaypointBufferToThatSegment
```

### Expected result

The original waypoint endpoints participate in antimeridian cutting.

No waypoint buffer is applied to that crossing pair.

---

## C12 - Noncrossing Segments In Same Airway Can Still Be Clipped

### Proposed test name

```text
Generate_Clipping_WhenOnlyOneSegmentCrossesAntimeridian_StillClipsEligibleNoncrossingSegments
```

### Setup

One airway containing:

```text
ordinary segment
antimeridian crossing segment
ordinary segment
```

### Expected result

Only the crossing pair bypasses clipping.

The ordinary pairs are clipped normally.

---

## C13 - Buffering Around Shared Waypoint Creates Independent Line Pieces

### Proposed test name

```text
Generate_Clipping_WhenContinuousAirwayIsBuffered_CreatesIndependentPiecesAroundSharedWaypoint
```

### Setup

```text
A -> B -> C
```

### Expected result

Conceptually:

```text
after A -> before B

after B -> before C
```

The gap around B is not accidentally reconnected.

A `MultiLineString` is acceptable and expected.

---

## C14 - Great-Circle Clipping Distance Is Correct Within Tolerance

### Proposed test name

```text
Generate_Clipping_MovesEndpointByConfiguredGreatCircleDistance
```

### Expected result

The surface distance from the original waypoint to the clipped point equals the configured nautical-mile buffer within the agreed tolerance.

This test should use an independent assertion formula/helper rather than checking only that the coordinate changed.

---

## C15 - Clipping Maintains Travel Direction

### Proposed test name

```text
Generate_Clipping_MovesStartTowardEndAndEndTowardStart
```

### Protects

Incorrect bearing or reverse-bearing calculations.

---

## C16 - Clipping Plus Existing FAA Gap Does Not Reconnect Gap

### Proposed test name

```text
Generate_Clipping_WhenAirwayContainsExistingGap_PreservesGap
```

---

# Combined Integration Tests After Stage 3

These are not Stage 0 baseline tests, but they should remain in the overall plan.

---

## I01 - HighLow Grouping With FAA Gap

Airway is classified correctly and its geometry retains the gap.

---

## I02 - HighLow Grouping With Antimeridian Crossing

The feature goes to the correct output file and is cut correctly.

---

## I03 - Type Grouping With Clipped Geometry

Designation output grouping does not alter clipping results.

---

## I04 - Airway With Gap, Clipping, and Antimeridian Crossing

One airway contains all three geometry conditions.

Expected result:

- ordinary eligible segments are clipped,
- antimeridian pair is not clipped,
- crossing is cut,
- FAA gap remains a true route break,
- feature geometry remains valid,
- feature appears in exactly one output group.

---

## I05 - Multiple Gaps and Multiple Antimeridian Crossings

Protects the implementation goal of supporting repeated transformations in one airway.

---

# Suggested Test Fixture Strategy

The most important Stage 0 supporting work is a small builder for `NasrCsvDataCollection`.

The tests should not manually initialize dozens of irrelevant NASR properties in every method.

A fixture should provide defaults for fields that are irrelevant to a test while allowing the important fields to be overridden.

Conceptual usage:

```text
builder
    .AddAirwayBase(
        awyId: "V1",
        designation: "V")
    .AddAirwaySegment(
        awyId: "V1",
        pointSeq: 10,
        from: "AAAAA",
        to: "BBBBB",
        fromPtType: "WP",
        gap: "N",
        maxAuthAlt: 17000)
    .AddFix("AAAAA", latitude: 40, longitude: -100)
    .AddFix("BBBBB", latitude: 41, longitude: -99)
```

This is conceptual only.

The real builder should follow the actual NASR model property names and object structure.

---

# Suggested GeoJSON Assertion Strategy

Tests should parse generated GeoJSON and inspect only the structure that matters.

Examples:

```text
Feature count == 1

feature.properties["feb_AWY-ID"] == "V1"

feature.geometry.type == "LineString"

coordinates.Count == 3
```

For `MultiLineString`:

```text
lineParts.Count == expectedCount
```

For calculated points:

```text
abs(actualLongitude - expectedLongitude) <= tolerance
abs(actualLatitude  - expectedLatitude)  <= tolerance
```

Avoid assertions based on indentation or JSON property order.

---

# Tests That Should Be Written Before Stage 1 Begins

The complete baseline set is B01-B32.

If implementation time must be prioritized, these are the highest-value minimum subset that should exist before production code is changed:

```text
B13 - one valid segment produces LineString
B14 - longitude/latitude order
B15 - continuous segments merge
B16 - explicit gap splits line pieces
B18 - reference-only point collapses
B20 - gap survives reference collapse
B23 - unresolved trailing endpoint retains valid geometry
B25 - unresolved internal start throws
B26 - unresolved internal end throws
B27 - duplicate AWY_BASE IDs do not fail
B30 - feb_AWY-ID property
B31 - no usable geometry produces no feature
B32 - valid FeatureCollection structure
```

The settings/error tests B01-B12 should also be added during Stage 0 if practical because Stage 1 will modify generator settings and output behavior.

The goal should still be to complete B01-B32 before Stage 1, not only the minimum subset.

---

# Stage Activation Order

## Stage 0

Write and pass:

```text
B01-B32
```

Do not modify generator production behavior except for a testability change that is separately reviewed and proven not to alter output behavior.

## Stage 1

Add/enable:

```text
G01-G17
```

Then implement output grouping.

## Stage 2

Add/enable:

```text
A01-A14
```

Then implement antimeridian handling.

## Stage 3

Add/enable:

```text
C01-C16
```

Then implement waypoint clipping.

## Final integration

Add/enable:

```text
I01-I05
```

---

# Items To Lock Before Their Implementation Stage

These do not prevent Stage 0 from beginning.

## Before Stage 1

Confirm:

1. Whether `MaxAuthAlt` completely replaces the earlier designation-specific High/Low mapping for recognized airway designations.
2. What should happen for a blank/null `AwyDesignation` under `OutputBy = Type`.
3. How a designation containing a character invalid in a Windows filename should be handled.
4. Final dictionary key casing for returned output paths.

## Before Stage 2

Confirm the floating-point tolerance for calculated antimeridian intersection coordinates.

The agreed interpolation approach itself is already sufficient for the architecture.

## Before Stage 3

Confirm:

1. exact clipping-setting keys/configuration shape,
2. buffer values for FIX/NAVAID/Airport or other categories,
3. spherical Earth-radius constant,
4. distance tolerance for clipping assertions,
5. numerical tolerance used to determine when a clipped line has effectively zero length.

---

# Stage 0 Completion Checklist

- [ ] Add proposed AWY GeoJSON generator test folder.
- [ ] Add baseline test file.
- [ ] Add synthetic NASR fixture/builder.
- [ ] Add GeoJSON reader/assertion helper if useful.
- [ ] Implement B01-B32.
- [ ] Use temporary output directories.
- [ ] Do not depend on a local FAA CSV download.
- [ ] Confirm all baseline tests pass.
- [ ] Review baseline results with project partner.
- [ ] Lock Stage 1 classification precedence questions.
- [ ] Do not begin Stage 1 production changes until Stage 0 is accepted.

---

# Recommended Stage 0 Outcome

At the end of Stage 0, the existing AWY generator should behave exactly as it did before the test work began, but its important behavior will be protected by automated regression tests.

The later feature contracts will also be documented so that output grouping, antimeridian cutting, and waypoint clipping can be implemented one stage at a time without redesigning the test strategy during each stage.
