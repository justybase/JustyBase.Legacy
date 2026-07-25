# ADR-007: Testability Through Ports, Not Global State

**Status:** Accepted (2026)

## Context

Unit coverage numbers alone do not make the legacy WinForms host easier to change. The expensive parts were process-wide static catalogs (`NetezzaHelpers.baseTableDictionary`, `IGeneralDbService.ConnectionSessions`, `DynamicCollectionForNettezaHelpers`) and logic trapped inside `BaseWindow` partials. Clean layers (ADR-001) and engine-first completion (ADR-002) already show the winning pattern: move decisions behind injectable ports and keep WinForms as an adapter. Catalog and session registry ownership have moved to DI; `DynamicCollectionForNettezaHelpers` is the remaining global cache.

## Decision

Prefer, in this order:

1. **Injected schema/session state** — depend on `INetezzaSchemaTableCatalog`, `IDatabaseRuntimeContext` / `INetezzaCompletionContext`, and `IConnectionSessionRegistry` instead of static dictionaries.
2. **Engine-first SQL understanding** — new completion/lint behavior goes through Application + `NzCompletionEngine`; legacy regex helpers are maintenance-only (ADR-002).
3. **Use cases out of `BaseWindow`** — extract Application ports / services (pattern: `LegacySchemaDdlService`, SQL view models). Do not unit-test WinForms menus.
4. **Seams for import/export** — small, named operations with table-driven contract tests; characterize before rewriting large methods.
5. **Architecture fences** — keep Reflection tests that forbid WinForms in clean layers and **new** public static mutable fields in `App.Data.Netezza` / `AppBase.Services` (allowlist-only growth).

Do **not** prioritize first: thin provider stubs, full `BaseWindow` unit suites, or hosted Netezza integration in CI.

## Consequences

+ New services can be tested with NSubstitute and in-memory registries without seeding process globals.
+ Gradual migration: existing static writers keep working while call sites switch to ports.
+ Architecture tests fail PRs that reintroduce global mutable state.
- Session registry is a DI singleton (`ConnectionSessionRegistry`); the old `IGeneralDbService.ConnectionSessions` / `GeneralDic` statics have been removed.
- `DynamicCollectionForNettezaHelpers` remains process-wide until a dedicated completion-cache port exists.
- The Netezza table catalog is owned by `LegacyDatabaseRuntimeContext` / `INetezzaSchemaTableCatalog` — `NetezzaHelpers.baseTableDictionary` has been removed.
- Call-site migrations must update constructors and DI registrations carefully.
- Legacy `DatabaseExplorerControl` was removed after MVVM replacement; do not revive static-coupled WinForms explorers.
