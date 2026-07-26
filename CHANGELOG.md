# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html) for release tags (`vMAJOR.MINOR.PATCH.REVISION`).

## [Unreleased]

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
