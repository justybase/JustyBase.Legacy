# ADR-002: Engine-First SQL Completion (NzCompletionEngine)

**Status:** Accepted (2026)

## Context
The SQL editor needs autocomplete, signature help, hover info, semantic tokens, and linting for Netezza SQL. Earlier versions used regex-based heuristics inside a WinForms helper class (`AutocompleteClass`, 1186 lines, 0 tests), which was untestable and duplicated parsing logic.

## Decision
- Integrate **JustyBase.NetezzaSqlParser** (`NzCompletionEngine`) as the single source of truth for SQL understanding.
- `NzCompletionEngine` provides: completion items, semantic tokens, hover, signature help, and diagnostics (lint).
- ViewModel (`SqlAuthoringViewModel`) calls the engine via `INetezzaCompletionService` (interface in `JustData.Application/Sql/`).
- WinForms editor (`FastColoredTextBox` adapter) only renders what the engine returns — no SQL parsing in UI layer.
- Legacy regex helpers (`AutocompleteClass.LastSelect`, `FirstFrom`, etc.) are extracted to `SqlTextCursorParser` (pure static functions, `ReadOnlySpan<char>`, fully testable) and will be removed once `NzCompletionEngine` covers all cases.

## Consequences
+ Single parser = no drift between editor highlights and actual execution semantics.
+ Engine is a class library — unit-testable with 1000s of cases, no UI.
+ `SqlAuthoringViewModel` stays thin (~200 lines), delegates to injected service.
- External dependency (`JustyBase.NetezzaSql` repo, including `JustyBase.NetezzaSqlParser`) must be checked out as sibling for local builds.
- Legacy autocomplete path still exists for non-Netezza engines (General SQL) — tracked for future removal.