# TODO

Open work only. When an item is done, delete it - the commit is the record.

## After 3.0

- **The Harness.** Replace its commented-out runs with a switch that picks which runs to do. Give it
  XML docs on every member, remove the machine-specific paths and the planning-phase comments
  ("Phase 3.3-3.7", "(3.3)"). It should compile cleanly and run on any developer's machine.
- **Share repeated code.** Departures and Arrivals are near copies of each other; share what they
  have in common, and look for other copies at the same time.
- **One home for values used in several places.** Start with the d-TPP Metafile timing
  ("approximately 20 days prior to" a cycle), now written out in Core, the General tab tooltip,
  the run advisory and the docs: make it one Core constant. Then look for other values hard-coded
  in more than one place.
- **A new AIRAC cycle during a session.** When a cycle takes effect while FE-Buddy is open, pick
  it up without a restart. Agree the approach first.
- **A wrong DAT or SCT file should fail**, as a wrong ERAM file does, instead of only giving an
  advisory. Try a few real DAT and SCT files first, so valid ones don't fail.
- **Non-web custom alias sources.** If an FTP or other non-http(s) address is ever allowed, keep a
  secret in the address out of the error message.
- **GUI framework simplifications.** `TabbedServiceViewModel`, `ConversionTabViewModel` and the
  card interfaces each have a single user; fold them in.
- **Delete the alpha.1 News stub** (`FeBuddy/FeBuddy.Core/News.md` and its comment) in the first
  release after 3.0.0.
- **Decide whether to offer a per-user install.**

## Testing

- **Test more of `FeBuddy.Wpf`.** Not tested on their own yet: the Credentials page and editor,
  the Dashboard, the DAT and SCT2 to GeoJSON pages, and the Airports, Departures, Arrivals,
  NAVAIDs, ARTCC Boundaries, Fixes and Wx Stations tabs (settings blocks, dirty tracking,
  validation). They can be tested like the others, without a window.
