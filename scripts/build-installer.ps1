[CmdletBinding()]
param(
    [Parameter()]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string] $Version = '0.0.0.0',

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [switch] $NoRestore,

    # Self-contained (default) bundles the .NET runtime. Framework-dependent
    # builds require the .NET 10 Windows Desktop Runtime (x64) on the target.
    [Parameter()]
    [bool] $SelfContained = $true,

    # Build without the optional AI features and their heavy dependencies.
    [Parameter()]
    [switch] $NoAi,

    # Slim variant: Netezza + DB2 only (no MS SQL Server / PostgreSQL),
    # always without AI. Installs side-by-side with full (separate AppId,
    # install directory, and Start Menu group).
    [Parameter()]
    [switch] $SlimDb2
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$appProject = Join-Path $repoRoot 'JustData\JustData.csproj'
$stagingPath = Join-Path $repoRoot 'JustData\JD_TEMP\SetupFilesTmp'
$installerScript = Join-Path $repoRoot 'JustData\Installers\Offline\OfflineInstaller.iss'
$installerOutput = Join-Path $repoRoot 'JustData\Installers\Offline\Output'

function Assert-PathWithinRoot([string] $path, [string] $root) {
    $resolvedPath = [IO.Path]::GetFullPath($path).TrimEnd('\')
    $resolvedRoot = [IO.Path]::GetFullPath($root).TrimEnd('\')
    if (-not $resolvedPath.StartsWith($resolvedRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside repository root: $resolvedPath"
    }
}

foreach ($requiredPath in @($appProject, $installerScript)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required file does not exist: $requiredPath"
    }
}

Assert-PathWithinRoot $stagingPath $repoRoot
Assert-PathWithinRoot $installerOutput $repoRoot

Write-Host "Cleaning publish staging directory: $stagingPath"
if (Test-Path -LiteralPath $stagingPath) {
    Remove-Item -LiteralPath $stagingPath -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null

$isSlim = $SlimDb2.IsPresent
$enableAi = if ($isSlim) { 'false' } elseif ($NoAi) { 'false' } else { 'true' }
$includeMsSql = if ($isSlim) { 'false' } else { 'true' }
$includePostgres = if ($isSlim) { 'false' } else { 'true' }
# Both variants ship Netezza + DB2. Oracle stays opt-in (default off).
$slimLabel = if ($isSlim) { ' slim (Netezza+DB2, no-AI)' } else { '' }
Write-Host "Publishing $Configuration application (EnableAi=$enableAi, IncludeMsSql=$includeMsSql, IncludePostgres=$includePostgres)$slimLabel"
# Slim uses different MSBuild flags than every preceding build in the
# session, but MSBuild treats global-property changes as up-to-date and skips
# recompilation. A plain `dotnet publish` would then reuse the previous
# variant's reference closure (SqlClient/Npgsql/AI assemblies leaking into
# slim, verified locally). Force a full rebuild with the slim flags first;
# the full variants keep the standard single-step publish, which already
# produces correct self-contained output (publish --no-build broke the
# self-contained apphost smoke test in CI).
if ($isSlim) {
    $slimBuildArguments = @(
        'build',
        $appProject,
        '-c', $Configuration,
        '-r', 'win-x64',
        '-p:Platform=x64',
        '-p:IncludeDB2=true',
        "-p:IncludeMsSql=$includeMsSql",
        "-p:IncludePostgres=$includePostgres",
        "-p:EnableAi=$enableAi",
        '-p:UseAOT=false',
        '-p:PublishAot=false',
        '-p:PublishTrimmed=false',
        '-p:PublishReadyToRun=true',
        "-p:Version=$Version",
        '--no-incremental'
    )

    & dotnet @slimBuildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
}

$publishArguments = @(
    'publish',
    $appProject,
    '-c', $Configuration,
    '-r', 'win-x64',
    '-p:Platform=x64',
    '-p:IncludeDB2=true',
    "-p:IncludeMsSql=$includeMsSql",
    "-p:IncludePostgres=$includePostgres",
    "-p:EnableAi=$enableAi",
    # Windows Forms does not support Native AOT/trimming; use the supported
    # self-contained ReadyToRun configuration instead.
    '-p:UseAOT=false',
    '-p:PublishAot=false',
    '-p:PublishTrimmed=false',
    '-p:PublishReadyToRun=true',
    '--self-contained', $SelfContained.ToString().ToLowerInvariant(),
    '-o', $stagingPath,
    "-p:Version=$Version"
)
if ($NoRestore) {
    $publishArguments += '--no-restore'
}

& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

# Some transitive native packages copy every supported RID even when the
# application is published for one RID. The installer is Windows x64-only, so
# retain only the native assets that can be loaded by this application.
$runtimeRoot = Join-Path $stagingPath 'runtimes'
if (Test-Path -LiteralPath $runtimeRoot -PathType Container) {
    Get-ChildItem -LiteralPath $runtimeRoot -Directory |
        Where-Object { $_.Name -ne 'win-x64' } |
        Remove-Item -Recurse -Force
}

# PDB/XML files are useful for development but are not runtime files and can
# unnecessarily inflate both the installer and the portable ZIP package.
Get-ChildItem -LiteralPath $stagingPath -Recurse -File |
    Where-Object { $_.Extension -in @('.pdb', '.xml') } |
    Remove-Item -Force

$executable = Join-Path $stagingPath 'JustyBaseLegacy.exe'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Published executable was not produced: $executable"
}

$requiredRuntimeAssemblies = @(
    'DatabaseDataGridView.WinForms.dll'
)
$missingRuntimeAssemblies = $requiredRuntimeAssemblies |
    Where-Object { -not (Test-Path -LiteralPath (Join-Path $stagingPath $_) -PathType Leaf) }
if ($missingRuntimeAssemblies) {
    throw "Required runtime assemblies were not included in publish staging: $($missingRuntimeAssemblies -join ', ')"
}

# DB2 is implemented by the lightweight managed JustyBase driver. The
# IBM.Data.Db2 namespace is provided only for source compatibility; the IBM
# client assembly and native clidriver are no longer part of the application.
foreach ($db2Assembly in @('JustyBase.Db2.dll', 'JustyBase.Db2.IBMCompat.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $stagingPath $db2Assembly) -PathType Leaf)) {
        throw "DB2 JustyBase managed driver assembly was not included in publish staging: $db2Assembly"
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $stagingPath 'Microsoft.Data.SqlClient.dll') -PathType Leaf)) {
    if (-not $isSlim) {
        throw "MS SQL Server managed provider was not included in publish staging: $stagingPath"
    }
} elseif ($isSlim) {
    throw "Slim (Netezza+DB2) publish must not contain MS SQL Server provider: $stagingPath"
}

if ($enableAi -eq 'false') {
    $unexpectedAiFiles = Get-ChildItem -LiteralPath $stagingPath -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in @('JustyBase.Ai.dll', 'JustyBase.Ai.Embedded.dll', 'Markdig.dll', 'Microsoft.Web.WebView2.Core.dll', 'GitHub.Copilot.SDK.dll', 'Microsoft.Extensions.AI.dll') }
    if ($unexpectedAiFiles) {
        throw "No-AI/slim publish must not contain AI assemblies: $($unexpectedAiFiles.FullName -join ', ')"
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $stagingPath 'Npgsql.dll') -PathType Leaf)) {
    if (-not $isSlim) {
        throw "PostgreSQL managed provider was not included in publish staging: $stagingPath"
    }
} elseif ($isSlim) {
    throw "Slim (Netezza+DB2) publish must not contain PostgreSQL provider: $stagingPath"
}

$unexpectedFiles = Get-ChildItem -LiteralPath $stagingPath -Recurse -File |
    Where-Object { $_.Extension -in @('.pdb', '.xml', '.7z') }
if ($unexpectedFiles) {
    throw "Unexpected development/archive files remain in publish staging: $($unexpectedFiles.FullName -join ', ')"
}

$unexpectedRuntimeDirectories = @()
if (Test-Path -LiteralPath $runtimeRoot -PathType Container) {
    $unexpectedRuntimeDirectories = @(
        Get-ChildItem -LiteralPath $runtimeRoot -Directory |
            Where-Object { $_.Name -ne 'win-x64' }
    )
}
if ($unexpectedRuntimeDirectories.Count -gt 0) {
    throw "Unexpected runtime identifiers remain in publish staging: $($unexpectedRuntimeDirectories.Name -join ', ')"
}

if (-not (Test-Path -LiteralPath $installerOutput)) {
    New-Item -ItemType Directory -Path $installerOutput -Force | Out-Null
}
Write-Host "Cleaning installer output directory: $installerOutput"
Get-ChildItem -LiteralPath $installerOutput -Force | Remove-Item -Recurse -Force

$isccCommand = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
$iscc = if ($isccCommand) { $isccCommand.Source } else { $null }
if (-not $iscc) {
    $isccCandidates = @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    )
    $iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $iscc) {
    throw 'ISCC.exe was not found. Install Inno Setup 6 or add ISCC.exe to PATH.'
}

Write-Host "Building Inno Setup installer"
if ($isSlim -and -not $NoAi) {
    Write-Warning 'Slim variant implies no-AI; ignoring missing -NoAi switch.'
}
$variantSuffix = if ($isSlim) { '-slim' } elseif ($NoAi) { '-noai' } else { '' }
if (-not $SelfContained) {
    $variantSuffix += '-framework-dependent'
}
# Slim installs side-by-side with full: separate display name (install
# directory, Start Menu group, Add/Remove Programs) and separate AppId.
# The full variant keeps the historical AppId and directory.
$slimAppName = 'JustyBaseLegacy Slim'
$slimAppId = '{{5F718B72-A986-407C-9E00-C27B20438619}'
$isccArguments = @($installerScript, "/DMyAppVersion=$Version", "/DSourcePath=$stagingPath")
if ($isSlim) {
    # No embedded quotes: PowerShell quotes the whole argument for the native
    # command line when it contains a space, so ISCC receives a single
    # /DMyAppName=... argument. (Embedded value-only quotes split it into two
    # arguments and ISCC fails with "more than one script filename".)
    $isccArguments += "/DMyAppName=$slimAppName"
    $isccArguments += "/DMyAppId=$slimAppId"
}
if ($variantSuffix) {
    $isccArguments += "/DVariant=$variantSuffix"
}

& $iscc @isccArguments
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
}

$installers = @(Get-ChildItem -LiteralPath $installerOutput -Filter '*.exe' -File)
if ($installers.Count -ne 1) {
    throw "Expected exactly one installer in $installerOutput, found $($installers.Count)."
}

Write-Host "Installer created: $($installers[0].FullName) ($($installers[0].Length) bytes)"
