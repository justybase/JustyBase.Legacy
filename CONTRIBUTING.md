# Contributing to JustyBase.Legacy

## Quick Start

1. Read the [Architecture Decision Records](docs/ADR/) — 7 ADRs covering key design decisions.
2. Restore packages from NuGet.org (`dotnet restore JustData.All.slnx`).
3. Run `dotnet test` with `--filter "Category!=Integration"` before opening a PR (see [docs/INTEGRATION_TESTS.md](docs/INTEGRATION_TESTS.md) for optional real Netezza checks).
4. Check an existing test in `tests/` for style conventions.

---

## Rules (11)

### 1. Clean Layer Boundaries
`JustData.Application` and `JustData.ViewModels` must **not** reference `System.Windows.Forms`, `System.Drawing`, or any `-windows`-only API. Architecture tests enforce this in CI.

### 2. Stable AutomationIds on Every Interactive Control
Every WinForms control that the user interacts with must have a unique, stable `AutomationId` (set via `Name` or `AccessibleName`). UI tests depend on this — no coordinate-based automation.

### 3. No Static Service Locators
Do not call `Program.GetRequiredService<T>()` or similar. Inject dependencies through constructor parameters. The DI container is composed once at startup (composition root in `Program.Main`).

### 4. No WeakReferenceMessenger.Default
Use injected `IMessenger` from constructor injection. Static messengers break test isolation and create hidden coupling.

### 5. DataGridView Bindings → `BindingList<T>`, Hierarchies → `ObservableCollection<T>`
These types enable WinForms/VM change notification without coupling to WinForms-specific `IBindingListView` or custom invalidation.

### 6. Extend `ObservableValidator` for Data Validation
When a view model needs validation (`INotifyDataErrorInfo`), derive from `ObservableValidator` (CommunityToolkit.Mvvm). Do not hand-roll `INotifyDataErrorInfo`.

### 7. Write Tests Before New VM Logic
New view models and services require tests. For SQL parsing logic (`SqlTextCursorParser` functions), prefer table-driven unit tests; property-based testing (e.g. FsCheck) is optional when it adds meaningful coverage.

### 8. Architecture Tests Must Pass — No New WinForms References in Clean Layers
Run `dotnet test tests/JustData.ViewModels.Tests` locally before push. The architecture test gate checks against `System.Windows.Forms` in clean layers.

### 9. New Public Interfaces in `JustData.Application` Require Implementation
Every new `I*` in `JustData.Application` must have a concrete implementation, registered in the DI composition root (`Program.cs`). No orphan interfaces.

### 10. Link the Relevant ADR in PR Description
Every PR that touches architecture should reference the relevant ADR (`docs/ADR/ADR-xxx-title.md`). If no ADR covers the decision, start a new one (template below).

### 11. Prefer Injected Ports Over Global Mutable State
Do not add new `public static` mutable fields in `App.Data.Netezza` or `AppBase.Services`. Use `INetezzaSchemaTableCatalog`, `IConnectionSessionRegistry`, and runtime/completion contexts instead. Prefer extracting use cases from `BaseWindow` over unit-testing WinForms. See [ADR-007](docs/ADR/ADR-007-testability-ports-over-global-state.md). `StaticStateFenceTests` enforces the allowlist in CI.

---

## ADR Template

```markdown
# ADR-XXX: Title

**Status:** Proposed / Accepted / Deprecated (YYYY)

**Context:** What problem are we solving?

**Decision:** What did we choose and why?

**Consequences:**
+ Pros
- Cons / trade-offs
```

---

## Architecture Governance

| ADR | Topic |
|-----|-------|
| [ADR-001](docs/ADR/ADR-001-clean-layers.md) | Clean layers: `net10.0` without `-windows` |
| [ADR-002](docs/ADR/ADR-002-engine-first-completion.md) | Engine-first SQL completion (NzCompletionEngine) |
| [ADR-003](docs/ADR/ADR-003-no-command-messages.md) | No command-style messages — explicit interfaces |
| [ADR-004](docs/ADR/ADR-004-docksuite-over-tabcontrol.md) | DockSuite over embedded TabControl |
| [ADR-005](docs/ADR/ADR-005-credential-encryption.md) | Credential encryption: AES-GCM + DPAPI |
| [ADR-006](docs/ADR/ADR-006-flaui-automationids.md) | FlaUI UI tests with stable AutomationIds |
| [ADR-007](docs/ADR/ADR-007-testability-ports-over-global-state.md) | Testability through ports, not global state |

---

## PR Checklist

- [ ] `pwsh ./scripts/verify-no-tracked-secrets.ps1` passes
- [ ] `dotnet build JustData.All.slnx -c Release` succeeds
- [ ] `dotnet test` passes (exclude Netezza integration: `--filter "Category!=Integration"`)
- [ ] Architecture tests pass (no WinForms in clean layers; no new static mutables outside allowlist)
- [ ] `AutomationId` set on new interactive controls
- [ ] ADR linked in PR description (or new ADR proposed)

---

## License

By contributing, you agree that your contributions will be licensed under the same [LGPL-3.0-or-later](LICENSE.md) license as the project.
