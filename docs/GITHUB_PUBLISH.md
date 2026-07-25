# First-time GitHub publish (maintainers)

Use this checklist once when creating `justybase/JustyBase.Legacy` on GitHub.

## Local prerequisites (before push)

1. **Secrets scan** — `pwsh ./scripts/verify-no-tracked-secrets.ps1` from the repo root (also in [SMOKE_CHECKLIST.md](SMOKE_CHECKLIST.md)).
2. **Local CI parity**:

```powershell
dotnet restore JustData.All.slnx
dotnet build JustData.All.slnx -c Release
dotnet test tests/AppBase.Tests/AppBase.Tests.csproj -c Release --no-build
# … remaining core test projects per .github/workflows/ci.yml
dotnet run --project JustData/JustData.csproj -c Release --no-build --no-launch-profile -- --smoke-test
```

3. **Screenshots** — login images under `docs/images/` must not show private LAN IPs or lab hostnames (use a fictional host such as `netezza.example.local`).

## GitHub steps (org `justybase`)

1. Create public repository **`JustyBase.Legacy`** under the org (or transfer from private `JustData.All`).
2. Set **default branch** to `main`.
3. Point local `origin` at `https://github.com/justybase/JustyBase.Legacy.git` and push `main`.
4. Confirm [CI](https://github.com/justybase/JustyBase.Legacy/actions/workflows/ci.yml) is green.
5. Repository **About**: short product description, topics (`netezza`, `sql`, `winforms`, `dotnet`, `desktop`, `puredata`), homepage `https://justybase.github.io/`.
6. Enable private security advisories (see [SECURITY.md](../SECURITY.md)).
7. Optionally pin the repo on the org profile; add a product card on [justybase.github.io](https://justybase.github.io/).
8. After [SMOKE_CHECKLIST.md](SMOKE_CHECKLIST.md), tag `vMAJOR.MINOR.PATCH.REVISION` to run [create-release.yml](../.github/workflows/create-release.yml).

## Coverage reference (local run, core CI projects)

Weighted merged line coverage across core assemblies enforced at **≥46%** in CI, with per-assembly floors for **App.Data.Netezza (≥18%)** and **AppBase.Services (≥35%)**. A recent local merge reported **~48%** overall, with **JustData.Application** ~90%+ and **JustData.ViewModels** ~65%+ (Netezza adapters and large service helpers still pull the blended number down). The coverage job writes `artifacts/coverage/coverage-summary.md` for PR review.
