# Pre-release smoke checklist

Run these checks manually before pushing a release tag (`vMAJOR.MINOR.PATCH.REVISION`).

## Automated (release workflow)

- [ ] `pwsh ./scripts/verify-no-tracked-secrets.ps1` passes
- [ ] NuGet vulnerability audit passes
- [ ] `dotnet test` on core projects passes with merged coverage ≥ 35%
- [ ] `--smoke-test` passes on built and published binaries
- [ ] `git diff --check` is clean

## Manual (desktop)

- [ ] Install from the produced Inno Setup `.exe` on a clean Windows x64 machine (or VM)
- [ ] Launch app, open login, connect with a test profile (or cancel without crash)
- [ ] Open SQL editor, run a simple `SELECT` against a test database
- [ ] Confirm results grid renders and export to CSV works on a small result set
- [ ] Open Preferences, change a setting, save and cancel paths behave correctly
- [ ] Close app and reopen — dock layout and recent files behave as expected

## Artifacts

- [ ] GitHub Release contains installer and ZIP
- [ ] `CHANGELOG.md` updated for the version being tagged

See also [CONTRIBUTING.md](CONTRIBUTING.md) and [docs/ADR/](ADR/).
