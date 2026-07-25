param(
    [string]$SourcePath = "$PSScriptRoot\..\JustData\BaseWindow.SqlExecution.cs",
    [string]$TargetPath = "$PSScriptRoot\..\JustData\BaseWindow.NetzezzaSqlEngine.cs"
)

$ErrorActionPreference = "Stop"
$lines = Get-Content -LiteralPath $SourcePath -Encoding UTF8

# Find RunNzSQLCore method
$start = -1
$end = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'private async Task RunNzSQLCore\(') { $start = $i; break }
}
if ($start -lt 0) { throw "RunNzSQLCore not found" }

$braceDepth = 0
$started = $false
for ($i = $start; $i -lt $lines.Count; $i++) {
    $open = ([regex]::Matches($lines[$i], '\{')).Count
    $close = ([regex]::Matches($lines[$i], '\}')).Count
    if ($lines[$i] -match '\{') { $started = $true }
    if ($started) {
        $braceDepth += $open - $close
        if ($braceDepth -le 0 -and $i -gt $start) { $end = $i; break }
    }
}
if ($end -lt 0) { throw "RunNzSQLCore end not found" }

$methodLines = $lines[$start..$end]
$header = @(
    '// Netezza SQL execution core extracted from BaseWindow.SqlExecution.cs.',
    'using AppBase.Common;',
    'using AppBase.Common.Enums;',
    'using AppBase.Common.Interfaces;',
    'using AppBase.Data;',
    'using AppBase.Data.Core.Interfaces;',
    'using AppBase.Services.Sql;',
    'using DatabaseDataGridView.WinForms;',
    'using DatabaseDataGridView.WinForms.Coloring;',
    'using FastColoredTextBoxNS;',
    'using JustyBase.NetezzaDriver;',
    'using JustyBaseLegacy.Services;',
    'using JustyBaseLegacy.UI.Controls;',
    'using JustyBaseLegacy.UI.Helpers;',
    'using JustyBaseLegacy.UI.Models;',
    'using SpreadSheetTasks;',
    'using System.Data;',
    'using System.Data.Common;',
    'using System.Diagnostics;',
    'using System.Text;',
    'using System.Text.RegularExpressions;',
    'using System.Windows.Forms;',
    '',
    'namespace JustyBaseLegacy.UI;',
    '',
    'public partial class BaseWindow',
    '{'
)
$footer = @('}', '')

$newTarget = ($header + $methodLines + $footer) -join "`n"
[System.IO.File]::WriteAllText($TargetPath, $newTarget, [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote RunNzSQLCore ($($methodLines.Count) lines) to $TargetPath"

$newSource = $lines[0..($start - 1)] + $lines[($end + 1)..($lines.Count - 1)]
[System.IO.File]::WriteAllLines($SourcePath, $newSource, [System.Text.UTF8Encoding]::new($false))
Write-Host "SqlExecution partial now $($newSource.Count) lines"
