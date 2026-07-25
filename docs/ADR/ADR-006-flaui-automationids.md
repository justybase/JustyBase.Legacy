# ADR-006: FlaUI UI Tests with Stable AutomationIds

**Status:** Accepted (2026)

## Context
WinForms provides no built-in UI testing framework. Manual testing is slow and non-reproducible. WinForms controls expose `AutomationId` through UI Automation, but most WinForms projects omit `Name`/`AutomationId`, making automation brittle (tests break on layout change).

## Decision
- **Framework:** **FlaUI** (managed UI Automation library, MIT) for all UI-level tests.
- **Stable AutomationIds:** Every interactive control in `JustData` defines a unique string `AutomationId` as the `Name` property (or `AccessibleName` where `Name` is data-driven).
- **Serialized execution:** All UI tests are marked `[Collection("UI")]` with `CollectionDefinition("UI", DisableParallelization = true)`.
- **AssemblyInfo:** `tests/JustData.UiTests/Properties/AssemblyInfo.cs` sets `[assembly: CollectionBehavior(DisableTestParallelization = true)]`.
- **Test targets:** Connection dialog (login, profile CRUD), SQL editor execution (F5), result grid rendering, preferences dialog, object explorer tree expand.
- **Test count:** 18+ tests, all serialized, targeting stable AutomationIds only (no coordinate-based clicks).

## Consequences
+ Full desktop automation — tests exercise real UI, not mock views.
+ Stable across layout changes — tests find controls by `AutomationId`, not position.
+ Rare in WinForms ecosystem — differentiator in portfolio (documented as "killer-feature").
- Tests require a Windows desktop with UI (CI runs on `windows-latest` with `--headless` caveats).
- Serialized execution is slow (~5-10 min for 18 tests).
- Every new interactive control must define `AutomationId` (enforced in code review, documented in `CONTRIBUTING.md`).