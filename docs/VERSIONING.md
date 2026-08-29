# Versioning Policy

FE-BUDDY follows [Semantic Versioning 2.0.0](https://semver.org/). This document
explains what that means in practice, so everyone bumps version numbers the
same way.

Every release version looks like this:

```
MAJOR.MINOR.PATCH[-PRERELEASE]
```

Example: `2.8.3`, or `2.9.0-beta.2`.

---

## First: a version number is not a decimal number

This is the single most common mistake, so it goes first.

**`2.8.10` is greater than `2.8.9`.**

It is tempting to read `2.8.9` and `2.8.10` the way you'd read decimals —
like `2.89` vs `2.810` — and conclude `.10` is "smaller" than `.9`. That
intuition is wrong here, for two reasons:

1. **Each segment (`MAJOR`, `MINOR`, `PATCH`) is its own independent integer
   counter, not a digit after a decimal point.** The `PATCH` field just
   counts "how many patch releases have happened since the last minor bump."
   After 9 patch releases comes the 10th. It has nothing to do with place
   value.
2. **Per the SemVer spec, numeric identifiers are compared numerically, not
   as text.** `10 > 9` is just normal integer comparison. (For what it's
   worth, this also isn't a "string sorting" thing — even sorted as plain
   text, `"10"` does *not* universally mean "less than `"9"`"; SemVer avoids
   that ambiguity entirely by mandating numeric comparison.)

So the sequence `2.8.8 → 2.8.9 → 2.8.10 → 2.8.11 → ...` is completely normal
and expected. Nothing "resets" or "goes backward" at the `9 → 10` boundary.

---

## MAJOR.MINOR.PATCH — when to bump each

Given a released version `MAJOR.MINOR.PATCH`, increment:

### `MAJOR` — e.g. `2.8.3 → 3.0.0`
Bump this for a breaking or fundamentally incompatible change — something
that changes behavior in a way existing users/workflows can't assume still
works the same. In FE-BUDDY terms: a rewrite of a core system (e.g. the
planned WPF UI rewrite), a change to output file formats that breaks
compatibility with VRC/vSTARS/vERAM consumers, or removing/replacing a major
feature.

**When MAJOR is bumped, reset MINOR and PATCH to `0`.**

### `MINOR` — e.g. `2.8.3 → 2.9.0`
Bump this when you add new functionality in a backwards-compatible way —
new features, new conversion types, new supported data sources — that don't
break existing behavior.

**When MINOR is bumped, reset PATCH to `0`.**

### `PATCH` — e.g. `2.8.3 → 2.8.4`
Bump this for backwards-compatible bug fixes only — no new functionality,
just a fix. This is the counter from the example above: `2.8.9 → 2.8.10` is
just the 10th patch since `2.8.0`.

If you're unsure whether something is MINOR or PATCH, ask: "did this add a
capability that wasn't there before, or just fix something that was
supposed to already work?" New capability → MINOR. Fix → PATCH.

---

## Pre-release tags: `-alpha`, `-beta`, `-rc`

Sometimes we want to ship a version to testers *before* it's the official
stable release for that number. That's what the pre-release suffix is for:

```
MAJOR.MINOR.PATCH-alpha.N
MAJOR.MINOR.PATCH-beta.N
MAJOR.MINOR.PATCH-rc.N
```

`N` is a counter starting at `1` (`-alpha.1`, `-alpha.2`, ...).

A pre-release tag means: **"this build is on its way to becoming
`MAJOR.MINOR.PATCH`, but is not that release yet."** For example, everything
below is a step on the road to the stable release `2.9.0`:

```
2.9.0-alpha.1
2.9.0-alpha.2
2.9.0-beta.1
2.9.0-beta.2
2.9.0-rc.1
2.9.0            <- the actual stable release
```

### Which tag to use, and when

| Tag | Meaning | Use when |
|---|---|---|
| `-alpha.N` | Early, unstable, in-progress | Feature is incomplete or actively being built; internal/developer testing only; things may be broken or half-finished. |
| `-beta.N` | Feature-complete, being tested | The feature set for this version is done; you're now testing for bugs/polish, not building new pieces. Safe for willing external testers. |
| `-rc.N` ("release candidate") | Believed ready to ship | No known bugs; this build is a candidate to become the stable release as-is. If no issues turn up, the *next* release is the stable version with the tag simply dropped. |

If a bug is found in `2.9.0-rc.1`, you fix it and cut `2.9.0-rc.2` — you do
**not** bump the core version number for that. The `MAJOR.MINOR.PATCH` part
stays fixed at the target version (`2.9.0`) throughout the whole
alpha → beta → rc chain; only the pre-release counter moves, until the tag
is finally dropped for the real release.

### Ordering / precedence

SemVer defines a strict precedence order, low to high:

```
2.9.0-alpha.1  <  2.9.0-alpha.2  <  2.9.0-beta.1  <  2.9.0-rc.1  <  2.9.0
```

Two things fall out of this that matter for FE-BUDDY specifically:

- **Any pre-release is considered "less than" its final stable release.**
  `2.9.0-rc.1 < 2.9.0`, always — even though the RC might contain literally
  the same code that ships as stable.
- **A pre-release of a future version is still "greater than" the current
  stable version**, because comparison looks at `MAJOR.MINOR.PATCH` first:
  `2.9.0-alpha.1 > 2.8.3`, because `2.9.0 > 2.8.3` regardless of the
  pre-release tag.

This is directly relevant to the update/rollback behavior we're building
into the installer: `2.8.3 → 2.8.4-alpha.1 → 2.8.3` is a legitimate
"try a prerelease, then fall back to stable" path, not a version going
backward-then-forward-then-backward in a way that should be blocked. Only
stable-to-older-stable (`2.8.4 → 2.8.3`) is a true downgrade we want to
prevent.

The precedence and channel rules on this page are encoded in
`FeBuddy.Versioning` (`ProductVersion`, `ReleaseChannel`) and the
downgrade rule in `UpdatePolicy`, with unit tests in
[`FeBuddy.Versioning.Tests`](../FeBuddy.Versioning.Tests). If you change a
rule here, update those tests too.

---

## Quick reference

- Bug fix, no new feature → bump **PATCH** (`2.8.9 → 2.8.10`, not `2.9.0`).
- New feature, nothing breaks → bump **MINOR**, reset PATCH (`2.8.9 → 2.9.0`).
- Breaking/incompatible change → bump **MAJOR**, reset MINOR and PATCH
  (`2.9.4 → 3.0.0`).
- Testing something before it's official → append `-alpha.N`, `-beta.N`, or
  `-rc.N` to the **version you're aiming to release**, not the last stable
  version.
- The number after `MAJOR.MINOR.PATCH` (and after `-alpha`/`-beta`/`-rc`)
  is a counter, not a decimal digit. `9 → 10` is forward, always.

Full spec: <https://semver.org/>
