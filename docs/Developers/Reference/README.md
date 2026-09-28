# Reference material

Source files kept for reading, not for building. Nothing in this folder belongs to FE-Buddy, is
compiled into it, or is referenced by any project in `FeBuddy/FeBuddy.sln` - the folder sits
under `docs/` precisely so it can never be mistaken for ours or pulled into the build.

## Contents

| File | What it is | Why it is here |
|---|---|---|
| `AliasParser.cs` | CRC's alias-command parser, namespace `Vatsim.Nas.Crc.CommandProcessing`. | Ground truth for how CRC actually parses alias files and dynamic variables. FE-Buddy generates alias commands (today `Airways.txt`, more later), and they have to be right at the consuming end, not merely plausible. |

## Rules for using it

- **Read it, do not copy it.** It is someone else's code, under whatever terms CRC ships. Treat it
  as documentation of behaviour, not as a source of code to paste into FE-Buddy.
- **It is a snapshot.** It reflects CRC as of whenever it was captured. Before relying on a detail,
  check it still holds in the current CRC build rather than assuming this file is current.
- **Keep it out of any project.** Do not add it to a `.csproj`, do not move it next to source, and
  do not let a wildcard `Compile Include` reach it.

Anything else added here follows the same rules: a row in the table above saying what it is, where
it came from, and why we keep it.
