[CmdletBinding()]
param(
    [Parameter()]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string] $Version = '0.0.0.0',

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [switch] $NoRestore
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

Write-Host "Publishing $Configuration application"
$publishArguments = @(
    'publish',
    $appProject,
    '-c', $Configuration,
    '-r', 'win-x64',
    '-p:Platform=x64',
    '-p:UseAOT=true',
    '--self-contained', 'true',
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
& $iscc $installerScript "/DMyAppVersion=$Version" "/DSourcePath=$stagingPath"
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
}

$installers = @(Get-ChildItem -LiteralPath $installerOutput -Filter '*.exe' -File)
if ($installers.Count -ne 1) {
    throw "Expected exactly one installer in $installerOutput, found $($installers.Count)."
}

Write-Host "Installer created: $($installers[0].FullName) ($($installers[0].Length) bytes)"
