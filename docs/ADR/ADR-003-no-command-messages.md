# ADR-003: No Command-Style Messages — Explicit Interfaces

**Status:** Accepted (2026)

## Context
`CommunityToolkit.Mvvm` provides `IMessenger` with `Send<TMessage>()`. Early MVVM migration used "command-style" messages like `ExecuteSqlCommandMessage`, `RefreshSchemaMessage` — essentially fire-and-forget commands with payloads. This couples sender to implicit handler behavior and makes flow hard to trace.

## Decision
- **No command-style messages.** Every cross-VM communication uses explicit interfaces:
  - `ISqlExecutionUseCase.ExecuteAsync(request, ct)` — direct call, awaitable, cancellable.
  - `ISchemaRefreshService.RefreshAsync(profileId, ct)` — explicit contract.
  - `IImportExportOrchestrator.ImportAsync(...)` — explicit orchestration.
- `IMessenger` used **only** for UI-level notifications:
  - `SqlExecutionCompletedMessage` (result payload)
  - `SchemaRefreshedMessage` (profileId, success)
  - `ThemeChangedMessage` (theme name)
  - `StatusMessage` (user-facing toast)
- No `WeakReferenceMessenger.Default` — `IMessenger` injected via DI everywhere.

## Consequences
+ Call graph is visible in IDE (Go to Implementation on interface).
+ Unit tests mock interfaces, not message handlers.
+ No "who handles this message?" debugging.
- More boilerplate (interface + implementation + DI registration) vs. `Messenger.Send()`.
- New developers must learn the interface catalog (`JustData.Application/UseCases/`).