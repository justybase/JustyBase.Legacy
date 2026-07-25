[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageFile,

    [ValidateRange(0, 100)]
    [double]$MinimumLineRatePercent = 10
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $CoverageFile -PathType Leaf)) {
    throw "Coverage report was not found: $CoverageFile"
}

[xml]$report = Get-Content -LiteralPath $CoverageFile -Raw
$includedAssemblies = @(
    'AppBase.Common',
    'AppBase.Services',
    'AppBase.Data.Core',
    'App.Data.Netezza',
    'JustData.Application',
    'JustData.ViewModels'
)
$packages = @($report.coverage.packages.package | Where-Object {
    $includedAssemblies -contains [string]$_.name
})

if ($packages.Count -eq 0) {
    throw 'Coverage report does not contain any of the required core assemblies.'
}

foreach ($package in $packages) {
    $packageLines = @(
        foreach ($class in @($package.classes.class)) {
            @($class.lines.line)
        }
    )
    $packageValidLines = $packageLines.Count
    $packageCoveredLines = @($packageLines | Where-Object { [int]$_.hits -gt 0 }).Count
    $packageRate = if ($packageValidLines -gt 0) {
        100.0 * $packageCoveredLines / $packageValidLines
    }
    else {
        0.0
    }

    Write-Host ("{0}: {1:N2}% ({2}/{3})" -f `
        [string]$package.name,
        $packageRate,
        $packageCoveredLines,
        $packageValidLines)
}

$coveredLines = 0L
$validLines = 0L
foreach ($package in $packages) {
    foreach ($class in @($package.classes.class)) {
        foreach ($line in @($class.lines.line)) {
            $validLines++
            if ([int]$line.hits -gt 0) {
                $coveredLines++
            }
        }
    }
}

if ($validLines -eq 0) {
    throw 'Coverage report contains no executable core lines.'
}

$lineRatePercent = 100.0 * $coveredLines / $validLines
Write-Host ("Weighted core line coverage: {0:N2}% ({1}/{2})" -f $lineRatePercent, $coveredLines, $validLines)

if ($lineRatePercent -lt $MinimumLineRatePercent) {
    throw ("Core line coverage {0:N2}% is below the required {1:N2}%." -f $lineRatePercent, $MinimumLineRatePercent)
}
