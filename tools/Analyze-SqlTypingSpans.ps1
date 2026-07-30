# Analyze SqlTypingPerf NDJSON spans.
# Usage:
#   .\Analyze-SqlTypingSpans.ps1
#   .\Analyze-SqlTypingSpans.ps1 -Path "$env:LOCALAPPDATA\JustyBase\perf\sql-typing-spans-....ndjson"

param(
    [string]$Path = ""
)

$ErrorActionPreference = "Stop"
$perfDir = Join-Path $env:LOCALAPPDATA "JustyBase\perf"

if ([string]::IsNullOrWhiteSpace($Path)) {
    if (-not (Test-Path $perfDir)) {
        Write-Error "Perf folder not found: $perfDir. Run JustData with JUSTYBASE_SQL_TYPING_PERF=1 first."
    }
    $Path = Get-ChildItem $perfDir -Filter "sql-typing-spans-*.ndjson" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not $Path) {
        Write-Error "No sql-typing-spans-*.ndjson in $perfDir"
    }
}

Write-Host "Analyzing: $Path"

$lines = Get-Content -LiteralPath $Path
$summaries = @()
$ends = @{}

foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    try {
        $o = $line | ConvertFrom-Json
    } catch {
        continue
    }

    if ($o.op -eq "session_summary") {
        $summaries += $o
        continue
    }

    if ($o.phase -ne "end") { continue }
    if ($o.op -eq "session" -or $o.op -eq "session_summary") { continue }

    $name = [string]$o.op
    if (-not $ends.ContainsKey($name)) {
        $ends[$name] = New-Object System.Collections.Generic.List[double]
    }
    $ends[$name].Add([double]$o.durationMs)
}

function Get-Percentile([double[]]$values, [double]$p) {
    if ($null -eq $values -or $values.Length -eq 0) { return 0 }
    $sorted = $values | Sort-Object
    $idx = ($sorted.Count - 1) * $p
    $lo = [int][Math]::Floor($idx)
    $hi = [int][Math]::Ceiling($idx)
    if ($lo -eq $hi) { return $sorted[$lo] }
    $w = $idx - $lo
    return ($sorted[$lo] * (1 - $w)) + ($sorted[$hi] * $w)
}

$rows = foreach ($key in $ends.Keys) {
    $vals = $ends[$key].ToArray()
    [pscustomobject]@{
        op       = $key
        count    = $vals.Length
        maxMs    = ($vals | Measure-Object -Maximum).Maximum
        medianMs = [math]::Round((Get-Percentile $vals 0.50), 2)
        p95Ms    = [math]::Round((Get-Percentile $vals 0.95), 2)
        sumMs    = [math]::Round(($vals | Measure-Object -Sum).Sum, 1)
        slow16   = @($vals | Where-Object { $_ -ge 16 }).Count
    }
}

Write-Host ""
Write-Host "=== Ranking by maxMs (from end events) ==="
$rows | Sort-Object maxMs -Descending | Format-Table -AutoSize

Write-Host "=== Ranking by p95Ms ==="
$rows | Sort-Object p95Ms -Descending | Format-Table -AutoSize

if ($summaries.Count -gt 0) {
    Write-Host "=== Embedded session_summary lines ==="
    $summaries |
        Sort-Object maxMs -Descending |
        Select-Object opName, count, maxMs, medianMs, p95Ms, sumMs, slowCount |
        Format-Table -AutoSize
}

$top = $rows | Sort-Object maxMs -Descending | Select-Object -First 1
if ($top) {
    $hyp = "unknown - inspect Call Tree for this op"
    if ($top.op -eq "fctb.subscribers") { $hyp = "B/H (TextChanged subscribers wall time)" }
    elseif ($top.op -like "fctb.*") { $hyp = "A (FCTB core path)" }
    elseif ($top.op -like "editor.handle*") { $hyp = "B (Legacy UpdateAdditionStyles)" }
    elseif ($top.op -eq "host.fctb_text_changed") { $hyp = "B (host HandleTextChanged)" }
    elseif ($top.op -like "host.fctb_text_changed_delayed*") { $hyp = "C (delayed path)" }
    elseif ($top.op -eq "host.selection_delayed") { $hyp = "D (same-words / selection)" }
    elseif ($top.op -eq "host.semantic") { $hyp = "E (semantic classification)" }
    elseif ($top.op -like "autocomplete.*") { $hyp = "C (NZ autocomplete)" }

    Write-Host ("Dominant op: {0}  maxMs={1}  p95Ms={2}  => hypothesis {3}" -f $top.op, $top.maxMs, $top.p95Ms, $hyp)
    Write-Host "If all maxMs under 20ms but UI still freezes => hypothesis F (paint/GDI). See JustData/Diagnostics/vs-confirm.md"
}
