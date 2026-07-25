param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('ImportExport', 'FileOps', 'SchemaRefresh', 'DbExplorerMenus', 'ObjectExplorer', 'Editor', 'GridResults', 'Lifecycle')]
    [string]$Phase,

    [string]$SourcePath = ""
)

$ErrorActionPreference = "Stop"
$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $SourcePath) {
    $SourcePath = Join-Path $scriptDir "..\JustData\BaseWindow.cs"
}
$SourcePath = [System.IO.Path]::GetFullPath($SourcePath)
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

$phaseConfig = @{
    ImportExport = @{
        Target = (Join-Path $scriptDir "..\JustData\BaseWindow.ImportExport.cs")
        Comment = '// BaseWindow import/export partial (file import, clipboard, grid XLSX export).'
        Ranges = @(
            @(246, 246),
            @(499, 538),
            @(2397, 2492),
            @(4179, 4314),
            @(4617, 4649),
            @(4899, 4907),
            @(4914, 4968),
            @(5347, 5365)
        )
        NavComment = '        // --- Import/export: BaseWindow.ImportExport.cs ---'
    }
    FileOps = @{
        Target = (Join-Path $scriptDir "..\JustData\BaseWindow.FileOps.cs")
        Comment = '// BaseWindow file open/save and recent files partial.'
        Ranges = @(
            @(919, 1204),
            @(1312, 1420),
            @(1677, 1742),
            @(2724, 2849),
            @(3056, 3060)
        )
        NavComment = '        // --- File ops: BaseWindow.FileOps.cs ---'
    }
    SchemaRefresh = @{
        Target = (Join-Path $scriptDir "..\JustData\BaseWindow.SchemaRefresh.cs")
        Comment = '// BaseWindow schema refresh and database explorer tree orchestration partial.'
        Ranges = @(
            @(1058, 1280),
            @(2528, 2913),
            @(3530, 3750),
            @(4232, 4305)
        )
        NavComment = '        // --- Schema refresh: BaseWindow.SchemaRefresh.cs ---'
    }
    DbExplorerMenus = @{
        Target = (Join-Path $scriptDir "..\JustData\BaseWindow.DbExplorerMenus.cs")
        Comment = '// BaseWindow database explorer context menu and admin handlers partial.'
        Ranges = @(
            @(111, 122),
            @(2355, 2778),
            @(3542, 3859),
            @(3888, 3891)
        )
        NavComment = '        // --- DB explorer menus: BaseWindow.DbExplorerMenus.cs ---'
    }
    ObjectExplorer = @{
        Target = (Join-Path $scriptDir "..\JustData\BaseWindow.ObjectExplorer.cs")
        Comment = '// BaseWindow SQL object explorer (Legend) and editor integration partial.'
        Ranges = @(
            @(368, 402),
            @(679, 883),
            @(1667, 2028)
        )
        NavComment = '        // --- Object explorer: BaseWindow.ObjectExplorer.cs ---'
    }
    Editor = @{
        Target = (Join-Path $scriptDir "..\JustData\BaseWindow.Editor.cs")
        Comment = '// BaseWindow editor menu handlers and document map partial.'
        Ranges = @(
            @(673, 1203),
            @(1696, 1884)
        )
        NavComment = '        // --- Editor: BaseWindow.Editor.cs ---'
    }
    GridResults = @{
        Target = (Join-Path $scriptDir "..\JustData\BaseWindow.GridResults.cs")
        Comment = '// BaseWindow result grid UI helpers partial.'
        Ranges = @(
            @(652, 657),
            @(1891, 2136)
        )
        NavComment = '        // --- Grid results: BaseWindow.GridResults.cs ---'
    }
    Lifecycle = @{
        Target = (Join-Path $scriptDir "..\JustData\BaseWindow.Lifecycle.cs")
        Comment = '// BaseWindow form lifecycle (close, save state, notifications) partial.'
        Ranges = @(
            @(2220, 2332),
            @(2421, 2446)
        )
        NavComment = '        // --- Lifecycle: BaseWindow.Lifecycle.cs ---'
    }
}

$config = $phaseConfig[$Phase]
if (-not $config) { throw "Unknown phase: $Phase" }
$config.Target = [System.IO.Path]::GetFullPath($config.Target)

$body = New-Object System.Collections.Generic.List[string]
foreach ($r in $config.Ranges) {
    foreach ($line in (Get-Range $r[0] $r[1])) {
        [void]$body.Add($line)
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine($config.Comment)
foreach ($u in $usingBlock) { [void]$sb.AppendLine($u) }
[void]$sb.AppendLine('')
[void]$sb.AppendLine('namespace JustyBaseLegacy.UI')
[void]$sb.AppendLine('{')
[void]$sb.AppendLine('    public partial class BaseWindow')
[void]$sb.AppendLine('    {')
foreach ($b in $body) { [void]$sb.AppendLine($b) }
[void]$sb.AppendLine('    }')
[void]$sb.AppendLine('}')
[System.IO.File]::WriteAllText($config.Target, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $($body.Count) lines to $($config.Target)"

Remove-Ranges $config.Ranges

# Insert nav comment after SQL comment block if not already present
if ($lines -notcontains $config.NavComment) {
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '// --- SQL: BaseWindow.SqlExecution.cs ---') {
            $lines.Insert($i + 1, $config.NavComment)
            break
        }
    }
}

[System.IO.File]::WriteAllLines($SourcePath, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Host "BaseWindow.cs now $($lines.Count) lines"
