[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageFile,

    [ValidateRange(0, 100)]
    [double]$MinimumLineRatePercent = 10,

    # Optional floors: "AssemblyName=Percent" (e.g. App.Data.Netezza=18)
    [string[]]$MinimumAssemblyLineRates = @(),

    # Optional markdown table for PR comments / artifacts
    [string]$MarkdownReportPath = ''
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

$assemblyFloors = @{}
foreach ($entry in $MinimumAssemblyLineRates) {
    if ([string]::IsNullOrWhiteSpace($entry)) {
        continue
    }

    $parts = $entry.Split('=', 2)
    if ($parts.Count -ne 2) {
        throw "Invalid MinimumAssemblyLineRates entry '$entry'. Expected AssemblyName=Percent."
    }

    $name = $parts[0].Trim()
    $percent = [double]$parts[1].Trim()
    if ($percent -lt 0 -or $percent -gt 100) {
        throw "Assembly floor for '$name' must be between 0 and 100."
    }

    $assemblyFloors[$name] = $percent
}

$packageStats = @()
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

    $packageStats += [pscustomobject]@{
        Name = [string]$package.name
        Rate = $packageRate
        Covered = $packageCoveredLines
        Valid = $packageValidLines
    }

    Write-Host ("{0}: {1:N2}% ({2}/{3})" -f `
        [string]$package.name,
        $packageRate,
        $packageCoveredLines,
        $packageValidLines)
}

$coveredLines = 0L
$validLines = 0L
foreach ($stat in $packageStats) {
    $coveredLines += $stat.Covered
    $validLines += $stat.Valid
}

if ($validLines -eq 0) {
    throw 'Coverage report contains no executable core lines.'
}

$lineRatePercent = 100.0 * $coveredLines / $validLines
Write-Host ("Weighted core line coverage: {0:N2}% ({1}/{2})" -f $lineRatePercent, $coveredLines, $validLines)

if (-not [string]::IsNullOrWhiteSpace($MarkdownReportPath)) {
    $directory = Split-Path -Parent $MarkdownReportPath
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $lines = @(
        '## Core coverage',
        '',
        '| Assembly | Line coverage | Covered / Valid |',
        '| --- | ---: | ---: |'
    )
    foreach ($stat in ($packageStats | Sort-Object Name)) {
        $lines += ('| `{0}` | {1:N2}% | {2}/{3} |' -f $stat.Name, $stat.Rate, $stat.Covered, $stat.Valid)
    }
    $lines += ''
    $lines += ('**Weighted core line coverage:** {0:N2}% ({1}/{2})' -f $lineRatePercent, $coveredLines, $validLines)
    $lines += ''
    $lines += ('Gate: weighted >= {0:N0}%' -f $MinimumLineRatePercent)
    if ($assemblyFloors.Count -gt 0) {
        $floorText = ($assemblyFloors.GetEnumerator() | Sort-Object Name | ForEach-Object { '{0} >= {1:N0}%' -f $_.Key, $_.Value }) -join ', '
        $lines += ('Per-assembly floors: {0}' -f $floorText)
    }

    Set-Content -LiteralPath $MarkdownReportPath -Value ($lines -join [Environment]::NewLine) -Encoding utf8
    Write-Host ("Wrote markdown coverage report: {0}" -f $MarkdownReportPath)
}

foreach ($floor in $assemblyFloors.GetEnumerator()) {
    $stat = $packageStats | Where-Object { $_.Name -eq $floor.Key } | Select-Object -First 1
    if ($null -eq $stat) {
        throw ("Coverage report does not contain assembly '{0}' required for a per-assembly floor." -f $floor.Key)
    }

    if ($stat.Rate -lt $floor.Value) {
        throw ("Assembly {0} line coverage {1:N2}% is below the required {2:N2}%." -f `
            $floor.Key, $stat.Rate, $floor.Value)
    }
}

if ($lineRatePercent -lt $MinimumLineRatePercent) {
    throw ("Core line coverage {0:N2}% is below the required {1:N2}%." -f $lineRatePercent, $MinimumLineRatePercent)
}
