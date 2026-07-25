# Netezza integration tests (local only)

`tests/AppBase.IntegrationTests` runs a single read-only check (`SELECT 1`) against a **real** Netezza instance. There is no GitHub Actions workflow for this: hosted runners cannot reach a typical on-prem or VPN-protected database, and storing production-like credentials in org secrets is usually undesirable.

## When to run

- After driver or `App.Data.Netezza` changes that affect connectivity
- Before a release, if you have access to a dev/test Netezza host

## Configuration

Set these environment variables (never commit values):

| Variable | Description |
|----------|-------------|
| `NZ_DEV_HOST` | Hostname or IP |
| `NZ_DEV_DATABASE` | Database name |
| `NZ_DEV_USER` | User |
| `NZ_DEV_PASSWORD` | Password |
| `NZ_DEV_PORT` | TCP port |

## Run

```powershell
$env:NZ_DEV_HOST = "..."
$env:NZ_DEV_DATABASE = "..."
$env:NZ_DEV_USER = "..."
$env:NZ_DEV_PASSWORD = "..."
$env:NZ_DEV_PORT = "5480"

pwsh ./scripts/test-netezza-integration.ps1
```

Or:

```powershell
dotnet test tests/AppBase.IntegrationTests/AppBase.IntegrationTests.csproj -c Release
```

## Default `dotnet test` on the full solution

Integration tests are tagged `Category=Integration`. CI and the usual contributor command **exclude** them:

```powershell
dotnet test JustData.All.slnx -c Release --filter "Category!=Integration"
```

Without the filter, `dotnet test` on the full solution fails if the `NZ_DEV_*` variables are not set.
