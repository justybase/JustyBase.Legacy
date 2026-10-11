# Repository Guidelines

## Project Structure & Architecture

- `JustData/` contains the Windows Forms desktop app and startup composition root. `JustData.Application/` and `JustData.ViewModels/` hold platform-independent use cases and view models.
- `App.Data.*` contains database providers; `AppBase.*` contains shared services and data code.
- `tests/` holds unit, view-model, preferences, login, UI, and integration test projects. `docs/ADR/` records architecture decisions; `scripts/` contains build and maintenance scripts.
- Keep WinForms and Windows-only APIs at the UI edge. Inject services through constructors and follow the relevant ADR when changing layer boundaries.

## Build, Test, and Development

Use the .NET SDK pinned in `global.json` (10.0.301) on Windows. Local restore and build require the sibling checkout `../JustyBase.NetezzaSql`; shared JustyBase libraries are project references, with no NuGet fallback:

```powershell
dotnet restore JustData.All.slnx
dotnet build JustData.All.slnx -c Release
dotnet test JustData.All.slnx -c Release --filter "Category!=Integration"
dotnet run --project JustData/JustData.csproj
```

The integration tests require a real Netezza environment; see `docs/INTEGRATION_TESTS.md`. Optional provider and installer commands are documented in `README.md`.

## Coding Style

Use the surrounding C# style: four-space indentation, nullable annotations, PascalCase for public types and members, and descriptive test names ending in `Tests.cs`. No root `.editorconfig` or dedicated formatter is configured, so match adjacent files. Preserve clean-layer boundaries, constructor injection, and stable `AutomationId` values for interactive controls.

## Testing Guidelines

Add tests for new view-model and service logic. Place them in the matching project under `tests/`; use table-driven cases for SQL parser behavior where useful. Run the solution test command above before opening a PR. Architecture tests in `tests/JustData.ViewModels.Tests` guard layer boundaries.

## Commits and Pull Requests

Recent commits use short, imperative subject lines (for example, “Refactor resource files…”); no strict prefix convention is evident. PRs should state the change, include build and test results, link a relevant ADR for architecture changes, and complete the checklist in `CONTRIBUTING.md`. Run `pwsh ./scripts/verify-no-tracked-secrets.ps1` before submitting.

## Security and Configuration

Never commit credentials, profile exports, or production logs. Report vulnerabilities using `SECURITY.md`; follow its reporting instructions.
