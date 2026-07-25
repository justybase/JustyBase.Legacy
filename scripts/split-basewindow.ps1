param(
    [string]$SourcePath = "$PSScriptRoot\..\JustData\BaseWindow.cs"
)

$ErrorActionPreference = "Stop"
$lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $SourcePath -Encoding UTF8)

function Get-Range([int]$start, [int]$end) {
    $result = New-Object System.Collections.Generic.List[string]
    for ($i = $start - 1; $i -le $end - 1; $i++) {
        if ($i -ge 0 -and $i -lt $lines.Count) {
            [void]$result.Add($lines[$i])
        }
    }
    return ,$result
}

function Remove-Ranges([array]$ranges) {
    $toRemove = New-Object System.Collections.Generic.HashSet[int]
    foreach ($r in $ranges) {
        $start = [Math]::Min($r[0], $r[1])
        $end = [Math]::Max($r[0], $r[1])
        for ($i = $start; $i -le $end; $i++) {
            [void]$toRemove.Add($i - 1)
        }
    }
    foreach ($i in ($toRemove | Sort-Object -Descending)) {
        if ($i -ge 0 -and $i -lt $lines.Count) {
            $lines.RemoveAt($i)
        }
    }
}

$usingsEnd = 0
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^namespace ') {
        $usingsEnd = $i
        break
    }
}
$usingBlock = Get-Range 1 $usingsEnd

function Write-Partial([string]$path, [string[]]$bodyLines, [string]$comment) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine($comment)
    foreach ($u in $usingBlock) { [void]$sb.AppendLine($u) }
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('namespace JustyBaseLegacy.UI')
    [void]$sb.AppendLine('{')
    [void]$sb.AppendLine('    public partial class BaseWindow')
    [void]$sb.AppendLine('    {')
    foreach ($b in $bodyLines) { [void]$sb.AppendLine($b) }
    [void]$sb.AppendLine('    }')
    [void]$sb.AppendLine('}')
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($path, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
}

# --- Extract ---
$themeBody = New-Object System.Collections.Generic.List[string]
foreach ($r in @(@(471,527), @(539,1014), @(1274,1516), @(1712,1757))) {
    foreach ($line in (Get-Range $r[0] $r[1])) { [void]$themeBody.Add($line) }
}
Write-Partial "$PSScriptRoot\..\JustData\BaseWindow.Theme.cs" $themeBody.ToArray() '// BaseWindow chrome, DPI, and theme partial.'

$tabsBody = New-Object System.Collections.Generic.List[string]
foreach ($r in @(@(1559,1710), @(1932,1989), @(2210,2460), @(9348,9396))) {
    foreach ($line in (Get-Range $r[0] $r[1])) { [void]$tabsBody.Add($line) }
}
Write-Partial "$PSScriptRoot\..\JustData\BaseWindow.Tabs.cs" $tabsBody.ToArray() '// BaseWindow tab lifecycle partial.'

Write-Partial "$PSScriptRoot\..\JustData\BaseWindow.SqlResults.cs" (Get-Range 1016 1270).ToArray() '// BaseWindow SQL results UI partial (diagnostics, toolbars).'

$sqlBody = New-Object System.Collections.Generic.List[string]
foreach ($r in @(@(2464,4629), @(4912,5038), @(9399,10142))) {
    foreach ($line in (Get-Range $r[0] $r[1])) { [void]$sqlBody.Add($line) }
}
Write-Partial "$PSScriptRoot\..\JustData\BaseWindow.SqlExecution.cs" $sqlBody.ToArray() '// BaseWindow SQL execution partial.'

$errorLines = Get-Range 4631 4892
$errorPath = "$PSScriptRoot\..\AppBase.Services\Sql\_error_extract.txt"
$errorDir = Split-Path $errorPath -Parent
if (-not (Test-Path $errorDir)) { New-Item -ItemType Directory -Path $errorDir -Force | Out-Null }
[System.IO.File]::WriteAllLines($errorPath, $errorLines, [System.Text.UTF8Encoding]::new($false))

# --- Remove from main (single pass, original 1-based line numbers) ---
Remove-Ranges @(
    @(471,527), @(539,1014), @(1016,1270), @(1274,1516), @(1559,1710), @(1712,1757),
    @(1932,1989), @(2210,2460), @(2464,4629), @(4631,4892), @(4912,5038),
    @(9348,9396), @(9399,10142)
)

for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '_lintDiagnosticsTargets') {
        $marker = @(
            '',
            '        // --- Shared services (injected) ---',
            '        // --- Chrome / theme: BaseWindow.Theme.cs ---',
            '        // --- Tabs: BaseWindow.Tabs.cs ---',
            '        // --- SQL: BaseWindow.SqlExecution.cs ---',
            ''
        )
        for ($j = $marker.Length - 1; $j -ge 0; $j--) { $lines.Insert($i + 1, $marker[$j]) }
        break
    }
}

[System.IO.File]::WriteAllLines($SourcePath, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Host "Split complete. Remaining lines:" $lines.Count
