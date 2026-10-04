# Reference material

Other people's source, kept for reading. Nothing here is FE-Buddy's, compiled into it, or referenced
by any project - which is why it sits under `docs/`.

| File | What it is | Why it is here |
|---|---|---|
| `AliasParser.cs` | CRC's alias-command parser (`Vatsim.Nas.Crc.CommandProcessing`). | The ground truth for how CRC reads alias files and dynamic variables, so FE-Buddy's alias commands work in CRC, not just look right. |

- **Read it, don't copy it.** It is someone else's code; treat it as documentation of behaviour.
- **It is a snapshot.** Check a detail still holds in the current CRC before relying on it.
- **Keep it out of every project:** no `.csproj` entry, and no wildcard `Compile Include` that
  reaches it.

Anything added here gets a row in the table saying what it is and why it's kept.
