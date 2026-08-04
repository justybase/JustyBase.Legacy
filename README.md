<p align="center">
  <img src="Icons/icon2.png" alt="JustyBase.Legacy" width="88"/>
</p>

<h1 align="center">JustyBase.Legacy</h1>

<p align="center">
  <strong>Windows SQL client for IBM Netezza</strong><br/>
  Author, explore, and ship queries with a modern desktop IDE — built on .NET&nbsp;10.
</p>

<p align="center">
  <a href="https://github.com/justybase/JustyBase.Legacy/actions/workflows/ci.yml"><img src="https://github.com/justybase/JustyBase.Legacy/actions/workflows/ci.yml/badge.svg" alt="CI"/></a>
  <a href="https://github.com/justybase/JustyBase.Legacy/releases"><img src="https://img.shields.io/github/v/release/justybase/JustyBase.Legacy?include_prereleases&label=release" alt="Release"/></a>
  <a href="LICENSE.md"><img src="https://img.shields.io/badge/License-LGPL--3.0-blue.svg" alt="License: LGPL-3.0"/></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10-512BD4" alt=".NET 10"/></a>
</p>

<p align="center">
  <a href="#download">Download</a> ·
  <a href="#features">Features</a> ·
  <a href="#architecture">Architecture</a> ·
  <a href="#build-from-source">Build</a>
</p>

---

<p align="center">
  <img src="docs/images/explorer-dark.png" alt="SQL editor with results — dark theme" width="920"/>
</p>
<p align="center"><sub>SQL editor, result grid, and database explorer — dark theme</sub></p>

**JustyBase.Legacy** is a solo-maintained desktop IDE focused on **IBM Netezza / PureData**.  
Write and run SQL, browse catalog objects, tune preferences in a docked settings tab, and keep connection profiles encrypted on disk.

Netezza and DB2 are included in the default shipped provider set. The SQL editor selects the matching dialect for the active connection; Netezza remains the fallback for unknown connections. MS SQL, Oracle, and PostgreSQL remain optional compile-time providers.

---

## Download

Get the latest **Windows x64** build from [GitHub Releases](https://github.com/justybase/JustyBase.Legacy/releases/latest):

- Inno Setup installer, or
- Portable ZIP (self-contained)

No separate .NET runtime install required for release packages.

---

## Screenshots

| Object explorer | Preferences (docked tab) | Login |
|:---:|:---:|:---:|
| <img src="docs/images/explorer.png" alt="Object explorer" width="280"/> | <img src="docs/images/preferences.png" alt="Preferences as a document tab" width="280"/> | <img src="docs/images/login.png" alt="Login" width="280"/> |
| Catalog tree & DIMDATE selection | Settings live beside SQL tabs | Encrypted connection profiles |

Light and dark themes are both first-class (`UseSpecialColoring`).

---

## Features

- **Dialect-aware SQL editor** — Netezza and DB2 syntax highlighting, completion, hover, signature help, and linting selected per active connection
- **Quick Open (`Ctrl+P`)** — VS Code–style search across `.sql` file names and contents in the Files panel roots, the selected Git repo (when set), and open SQL tabs; Enter opens/focuses the file and jumps to a content match line when applicable
- **Virtual-mode result grid** — large result sets, filter/export to CSV / Excel
- **Object explorer** — connections, databases, tables; open DDL and navigate into objects
- **Docked Preferences** — General, Colors & Editor, Snippets, Execution, Results — as a document tab, not a modal
- **Encrypted profiles** — AES-GCM + DPAPI for the current Windows user ([ADR-005](docs/ADR/ADR-005-credential-encryption.md))
- **Release pipeline** — smoke test, coverage gate, NuGet audit, Inno installer + ZIP on tagged releases

---

## Architecture

Dependency direction: **UI → ViewModels → Application**.  
WinForms stays at the edge; business logic is testable without `-windows` TFMs.

```mermaid
flowchart TB
  subgraph clean [Clean layers]
    JA[JustData.Application]
    JVM[JustData.ViewModels]
  end

  subgraph ui [WinForms]
    JD[JustData]
  end

  subgraph providers [Providers]
    NZ[App.Data.Netezza]
    OPT[Optional MsSql / Oracle / Postgres]
  end

  JVM --> JA
  JD --> JVM
  JD --> JA
  JD --> NZ
  JD -.-> OPT
```

| Choice | Why |
|--------|-----|
| Clean layers | Architecture tests block WinForms leaks into Application / ViewModels |
| MVVM (`CommunityToolkit.Mvvm`) | Commands, messengers, validators under unit test |
| Provider engines | `ISqlExecutionEngine` selected per connection type |
| Central Package Management | Pinned versions in `Directory.Packages.props` |

Design notes: [docs/ADR/](docs/ADR/) (six ADRs).

---

## Testing & quality

| Layer | What runs |
|-------|-----------|
| Unit / ViewModel | Services, VMs, architecture governance (~1,100+ cases in CI projects) |
| Preferences / Login | Settings and profile flows |
| UI (FlaUI) | Stable `AutomationId`s; documentation screenshots via real user paths |
| Integration | Real Netezza — **local only** ([docs/INTEGRATION_TESTS.md](docs/INTEGRATION_TESTS.md)) |

CI on `main`: restore → NuGet audit → Release build → smoke test → core tests with **≥46%** merged line coverage (plus floors for Netezza ≥18% and Services ≥35%; Application typically ~90%+, ViewModels ~65%+).

---

## Build from source

**Requirements:** Windows x64, [.NET SDK 10.0.301](https://dotnet.microsoft.com/download/dotnet/10.0) (`global.json`), NuGet.org.

```powershell
dotnet restore JustData.All.slnx
dotnet build JustData.All.slnx -c Release
dotnet test JustData.All.slnx -c Release --filter "Category!=Integration"
```

### Local JustyBase.Netezza* libraries

When sibling folder `../JustyBase.NetezzaSql` exists (e.g. a `##justybase` multi-repo checkout), MSBuild automatically uses `ProjectReference` instead of NuGet for `JustyBase.Netezza`, `JustyBase.NetezzaDdl`, `JustyBase.NetezzaCatalogSql`, and `JustyBase.NetezzaSqlParser`. External clones without that sibling keep using NuGet (`*-*` = latest published, including previews). Force either mode with `-p:UseLocalJustyBaseLibraries=true|false`. Pin a NuGet version with `-p:JustyBaseNetezzaLibsPackageVersion=0.2.0-preview.6`.

Optional provider (example):

```powershell
dotnet build JustData/JustData.csproj -c Release -p:IncludeMsSql=true
```

Self-contained publish:

```powershell
dotnet publish JustData/JustData.csproj -c Release -r win-x64 --self-contained true
```

Build the local self-contained `win-x64` installer (requires Inno Setup 6):

```powershell
./scripts/build-installer.ps1 -Version 1.0.0.0
```

The script recreates the publish staging directory on every run, removes development symbols/documentation files, and leaves exactly one installer in `JustData/Installers/Offline/Output`.

---

## Security

- Profiles stored encrypted; keys protected with DPAPI for the current user
- Diagnostic logs under `%LOCALAPPDATA%\JustyBaseLegacy` with secret redaction
- Report vulnerabilities via [SECURITY.md](SECURITY.md)

Do not commit credentials, profile exports, or production logs.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for layer rules, AutomationIds, and the PR checklist.  
Changelog: [CHANGELOG.md](CHANGELOG.md).  
Maintainer publish steps: [docs/GITHUB_PUBLISH.md](docs/GITHUB_PUBLISH.md).

---

## License

Copyright (C) 2021–2026 Krzysztof Duśko.  
Released under [LGPL-3.0-or-later](LICENSE.md).
