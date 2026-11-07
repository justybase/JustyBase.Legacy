# AGENTS.md

## Setup

- Windows x64 only. SDK pinned in `global.json` (10.0.301). `JustData/JustData.csproj` is `net10.0-windows` WinExe, `Platforms=x64`; clean layers (`JustData.Application`, `JustData.ViewModels`) are plain `net10.0` with no `-windows` TFM.
- Local build requires sibling checkout `../JustyBase.NetezzaSql` (Netezza, parser, DDL, catalog, core, import/export, AI projects are direct `ProjectReference`s, no NuGet fallback). Never add `JustyBase.*` NuGet packages — `scripts/verify-no-internal-justybase-nugets.ps1` fails CI if you do.
- Two solution files (`.slnx`): `JustData.All.slnx` (default) and `JustData.NoAi.slnx` (excludes `JustyBase.Ai` / `JustyBase.Ai.Embedded`). Pair with `-p:EnableAi=false` for the no-AI build; `AI` compiler constant is defined only when `EnableAi=true` (`Directory.Build.targets`).
- Providers: DB2 / MsSql / Postgres default on, Oracle default off. Opt in/out per build, e.g. `-p:IncludeOracle=true`, `-p:IncludeDB2=false -p:IncludeMsSql=false -p:IncludePostgres=false`. Never set `PublishAot`/`UseAOT=true` with DB2 (blocked by `ValidateDb2Publishing`).
- Versions centralized in `Directory.Packages.props`. `TreatWarningsAsErrors` is on when `CI=true`, plus `NuGetAudit`; warnings that pass locally can fail CI.

## Build and verify (CI parity)

```powershell
dotnet restore JustData.All.slnx
dotnet build JustData.All.slnx -c Release -p:Platform=x64
dotnet run --project JustData/JustData.csproj -c Release -p:Platform=x64 --no-build --no-launch-profile -- --smoke-test
dotnet test JustData.All.slnx -c Release --filter "Category!=Integration"
pwsh ./scripts/verify-no-tracked-secrets.ps1
```

- Single test: `dotnet test tests/<Project>/<Project>.csproj -c Release --filter "FullyQualifiedName~<ClassOrMethod>"` (e.g. `tests/AppBase.Tests`). Architecture gate only: `dotnet test tests/JustData.ViewModels.Tests -c Release`.
- CI (`ci.yml`) runs per-project tests with coverage (AppBase, ViewModels, Preferences, Login), merges with `scripts/merge-cobertura-coverage.ps1`, and enforces **46% merged line rate** plus floors `App.Data.Netezza=18`, `AppBase.Services=35` (`scripts/check-core-coverage.ps1`).
- `tests/AppBase.IntegrationTests` is local-only, `Category=Integration`, needs `NZ_DEV_HOST/DATABASE/USER/PASSWORD/PORT` (`pwsh ./scripts/test-netezza-integration.ps1`). Full-solution `dotnet test` without the `Category!=Integration` filter fails without those vars.
- `tests/JustData.UiTests` (FlaUI) runs only via manual `ui-tests.yml` workflow, not CI. Do not run headless/unstable UI tests as a default check.
- No-AI check: `dotnet build JustData.NoAi.slnx -c Release -p:EnableAi=false -p:Platform=x64`; only `JustyBase.Ai.Contracts.dll` may remain in output (see `docs/NO_AI_BUILD.md`).

## Architecture constraints (enforced by tests, see `CONTRIBUTING.md` rules 1–11)

- Dependency direction: `JustData` (WinForms edge, DI composition root in `Program.Main`) → `JustData.ViewModels` → `JustData.Application`. Providers (`App.Data.*`) and shared code (`AppBase.*`) plug into the edge.
- `JustData.Application` / `JustData.ViewModels` must not reference `System.Windows.Forms`, `System.Drawing`, or `-windows`-only APIs — architecture tests fail the build.
- Constructor injection only: no `Program.GetRequiredService<T>()` static locators; inject `IMessenger`, never use `WeakReferenceMessenger.Default`.
- No new `public static` mutable state in `App.Data.Netezza` / `AppBase.Services` (`StaticStateFenceTests` allowlist); prefer injected ports (`INetezzaSchemaTableCatalog`, `IConnectionSessionRegistry`) per ADR-007.
- New `I*` in `JustData.Application` needs a concrete implementation registered in `Program.cs` — no orphan interfaces.
- WinForms specifics: every interactive control needs a stable `AutomationId` (`Name`/`AccessibleName`, no coordinate automation per ADR-006); `DataGridView` bindings use `BindingList<T>`, hierarchies use `ObservableCollection<T>`; validation via `ObservableValidator`, not hand-rolled `INotifyDataErrorInfo`.

## PR and security

- Before opening a PR: `pwsh ./scripts/verify-no-tracked-secrets.ps1`, `git diff --check`, Release build + filtered tests + architecture tests pass.
- PRs touching architecture must link the relevant `docs/ADR/ADR-xxx-*.md` (or propose a new one); keep `AutomationId`s stable.
- Never commit credentials, `*.env`, `*.pfx/.p12/.pem/.key`, `connectionprofile*.json`, `appsettings.Production.json`, profile exports, or production logs. Report vulnerabilities per `SECURITY.md`.
- Details live in `README.md` (providers, publish, installer via `scripts/build-installer.ps1` + Inno Setup 6), `docs/INTEGRATION_TESTS.md`, `docs/NO_AI_BUILD.md`, `CONTRIBUTING.md` (full rule list + checklist).
