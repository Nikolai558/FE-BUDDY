# Archive

Finished planning documents from the FE-Buddy 3.0 rewrite, kept as a record of **why** things
were built the way they were. None of them describes the code as it is today: they were written
as instructions for work that has since been done, and the code has moved on since (the folder
layout, class names and some behaviour all changed after they were written).

For how FE-Buddy works now, start at [docs/README.md](../../README.md).

| Document | What it was |
|---|---|
| [FE-Buddy_3.0_Structure_And_Build_Plan.md](FE-Buddy_3.0_Structure_And_Build_Plan.md) | The first build spec: project structure, the settings contract, the Airways domain model and the CRC property rules, phase by phase. |
| [FE-Buddy_3.0_Feedback_Remediation_Plan.md](FE-Buddy_3.0_Feedback_Remediation_Plan.md) | The second round of work after owner feedback: the AIRAC Service screen, the data pipeline, Airways corrections, the GUI phases. Its "[OWNER]" notes record decisions still worth knowing. |
| [AWY_GEOJSON_RegressionTests.md](AWY_GEOJSON_RegressionTests.md) | The regression-test plan written before the Airways code was ported. The cases live on in `FeBuddy.UnitTests`. |
| [Project-Structure.md](Project-Structure.md) | A proposed multi-project Clean Architecture layout. Superseded: `FeBuddy.Core` keeps one project with Domain / Application / Infrastructure folders instead - see [FeBuddy.Core-Structure.md](../FeBuddy.Core-Structure.md). |
| [FE-Buddy_3.0_Session_Handoff.md](FE-Buddy_3.0_Session_Handoff.md) | A hand-off note between two AI coding sessions (2026-09-23). Its "design decisions" section is carried into [Architecture.md](../Architecture.md). |

Open work is tracked in [TODO.md](../TODO.md), not here.
