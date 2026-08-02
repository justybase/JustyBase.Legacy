# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html) for release tags (`vMAJOR.MINOR.PATCH.REVISION`).

## [Unreleased]

## [1.1.0.2] - 2026-08-02

### Fixed
- Inline FIM ghost text now follows the selected SQL autocomplete item, matching VS Code-style Tab acceptance.

## [1.1.0.1] - 2026-08-01

### Added
- Hierarchical AST-backed SQL Outline with CTE, table, JOIN, subquery, view, procedure, and procedural block nodes.
- Source navigation, icons, parser fallback, and cached Outline refreshes.

### Fixed
- CTE Outline navigation now selects the definition name instead of a later query reference.

## [1.0.0.3] - 2026-07-30

### Added
- Online Netezza DDL loading path with coverage.
- SQL execution risk gate with host confirmation port and streamlined execution UI.
- SQL authoring performance improvements for large scripts.

### Changed
- Removed legacy `ObjectExplorerControl` in favor of MVVM database explorer.
- Schema refresh refactored through coordinator, repository modes, and editor catalog projection.
- Netezza schema refresh flags moved to `INetezzaHelperService` and gated downloads.
- Connection credentials injected via profile catalog instead of `LoginDataDic`.
- Editor workspace document logic centralized for tabs and docking.
- Autocomplete suggestion handling refactored across database classes.
- Netezza dynamic collection helpers renamed to `NetezzaLegacyCompletionHelpers`.
- FlaUI tests aligned with explorer and SQL execution refactors.
- Registered new application ports in DI and wired `BaseWindow` shell dependencies.

### Fixed
- SQL result grid second-run behavior and virtual row metrics policy.

### Internal
- Updated `.gitignore` to exclude local live-test gallery run logs.

## [1.0.0.2] - 2026-07-26

### Added
- Pre-publish helpers: `scripts/verify-no-tracked-secrets.ps1`, [docs/GITHUB_PUBLISH.md](docs/GITHUB_PUBLISH.md).
- File diagnostic logging under `%LOCALAPPDATA%\JustyBaseLegacy\logs` (separate from user MessageBox notifications).
- Portfolio documentation: screenshots, smoke checklist, and clearer install vs contributor build paths.
- Optional local `JustyBase.ImportExport` project reference when sibling `JustyBase.NetezzaSql` checkout is present.

### Changed
- Netezza CSV fast import and related tabular export paths use the shared **JustyBase.ImportExport** NuGet package (legacy `FastNetezzaCsvImport` and `CsvFastImport` UI removed).
- Netezza integration tests are local-only; removed the GitHub Actions workflow and badge (hosted runners cannot reach a real database). See [docs/INTEGRATION_TESTS.md](docs/INTEGRATION_TESTS.md).
- GitHub URLs and workflow checkouts aligned with the `justybase` organization (including winget manifest template and installer metadata).
- README testing section documents core coverage figures; CI runs the secrets verification script.
- Release workflow quality gates aligned with CI (full core test matrix and coverage threshold).
- Local JustyBase library wiring and CI coverage thresholds (≥46% blended, assembly floors for Netezza adapters and services).
- Netezza catalog/schema code refactored toward injected dependencies and shared package helpers (connection session registry, schema table catalog).

## [1.0.0.1] - 2026-07-25

### Fixed
- After editing and saving a connection (including the first-run placeholder), schema refresh rebuilds the live session so a restart is no longer required.
- Failed Netezza schema download drops the stale session so the next connect/edit can retry with fresh credentials.

## [1.0.0.0] - 2026-01-01

### Added
- Initial public release packaging (self-contained Windows x64, Inno Setup installer, ZIP).
- CI pipeline with NuGet audit, smoke test, coverage gate, and architecture tests.
