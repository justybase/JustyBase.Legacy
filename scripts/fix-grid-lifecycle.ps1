$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$mainPath = Join-Path $scriptDir "..\JustData\BaseWindow.cs"
$gridPath = Join-Path $scriptDir "..\JustData\BaseWindow.GridResults.cs"
$lifePath = Join-Path $scriptDir "..\JustData\BaseWindow.Lifecycle.cs"

$lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $mainPath -Encoding UTF8)

function Find-Line([string]$pattern) {
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $pattern) { return $i }
    }
    throw "Pattern not found: $pattern"
}

# Remove orphaned RowPostPaint body fragment
$orphanStart = Find-Line '^\s+string rowIdx = \(e\.RowIndex \+ 1\)\.ToString\(\);'
$orphanEnd = $orphanStart
while ($orphanEnd -lt $lines.Count -and $lines[$orphanEnd] -notmatch '^\s+\}\s*$') { $orphanEnd++ }
for ($i = $orphanEnd; $i -ge $orphanStart; $i--) { $lines.RemoveAt($i) }

$gridStart = Find-Line '^\s+private void CopyWithHeadersStripMenuItem_Click'
$gridEnd = Find-Line '^\s+private void ShowDiff_Click'
while ($gridEnd -lt $lines.Count -and $lines[$gridEnd] -notmatch '^\s+\}\s*$') { $gridEnd++ }

$lifeStart = Find-Line '^\s+private bool DoSaveTabState'
$lifeEnd = Find-Line '^\s+public static void OnScreenMessage'
while ($lifeEnd -lt $lines.Count -and $lines[$lifeEnd] -notmatch '^\s+\}\s*$') { $lifeEnd++ }

# Also move pin images used by grid
$imgStart = Find-Line '^\s+private readonly Image _normalXimage'
$imgEnd = Find-Line '^\s+return objBmpImage;'
while ($imgEnd -lt $lines.Count -and $lines[$imgEnd] -notmatch '^\s+\}\s*$') { $imgEnd++ }

$notifyStart = Find-Line '^\s+private readonly NotifyIcon _notifyIcon1'
$notifyEnd = $notifyStart
while ($notifyEnd -lt $lines.Count -and $lines[$notifyEnd] -notmatch '^\s+\}\s*$') { $notifyEnd++ }
$doMsgStart = Find-Line '^\s+private void DoMessage'
$doMsgEnd = $doMsgStart
while ($doMsgEnd -lt $lines.Count -and $lines[$doMsgEnd] -notmatch '^\s+\}\s*$') { $doMsgEnd++ }
$onScreenStart = Find-Line '^\s+public static void OnScreenMessage'
$onScreenEnd = $onScreenStart
while ($onScreenEnd -lt $lines.Count -and $lines[$onScreenEnd] -notmatch '^\s+\}\s*$') { $onScreenEnd++ }

$gridBody = New-Object System.Collections.Generic.List[string]
foreach ($i in (Get-Content -LiteralPath $gridPath -Encoding UTF8)) {
    if ($i -match 'DataGridViewNowe_') { [void]$gridBody.Add($i) }
}
# read existing partial header from grid file
$usingsEnd = 0
$gridLines = Get-Content -LiteralPath $gridPath -Encoding UTF8
for ($i = 0; $i -lt $gridLines.Count; $i++) {
    if ($gridLines[$i] -match '^namespace ') { $usingsEnd = $i; break }
}

function Write-PartialFile([string]$path, [string]$comment, [int]$start, [int]$end, [string[]]$extra) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine($comment)
    for ($i = 0; $i -lt $usingsEnd; $i++) { [void]$sb.AppendLine($gridLines[$i + 1]) }
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('namespace JustyBaseLegacy.UI')
    [void]$sb.AppendLine('{')
    [void]$sb.AppendLine('    public partial class BaseWindow')
    [void]$sb.AppendLine('    {')
    if ($extra) { foreach ($e in $extra) { [void]$sb.AppendLine($e) } }
    for ($i = $start; $i -le $end; $i++) { [void]$sb.AppendLine($lines[$i]) }
    [void]$sb.AppendLine('    }')
    [void]$sb.AppendLine('}')
    [System.IO.File]::WriteAllText($path, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
}

$existingGridExtras = @(
    '        private void DataGridViewNowe_writeStats(string obj) => mainTextBox.Text = obj;',
    '',
    '        private void DataGridViewNowe_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)',
    '        {',
    '            var grid = sender as DataGridView;',
    '            string rowIdx = (e.RowIndex + 1).ToString();',
    '            var centerFormat = new StringFormat()',
    '            {',
    '                Alignment = StringAlignment.Center,',
    '                LineAlignment = StringAlignment.Center',
    '            };',
    '            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);',
    '            e.Graphics.DrawString(rowIdx, this.Font, _colorTheme.GeneralBrush, headerBounds, centerFormat);',
    '        }',
    ''
)

Write-PartialFile $gridPath '// BaseWindow result grid UI helpers partial.' $gridStart ($gridEnd) $existingGridExtras

$lifeRanges = @($lifeStart..$lifeEnd) + @($notifyStart..$notifyEnd) + @($doMsgStart..$doMsgEnd) + @($onScreenStart..$onScreenEnd)
$lifeBody = New-Object System.Collections.Generic.List[string]
foreach ($idx in ($lifeRanges | Sort-Object -Unique)) { [void]$lifeBody.Add($lines[$idx]) }

$sbLife = New-Object System.Text.StringBuilder
[void]$sbLife.AppendLine('// BaseWindow form lifecycle (close, save state, notifications) partial.')
for ($i = 0; $i -lt $usingsEnd; $i++) { [void]$sbLife.AppendLine($gridLines[$i + 1]) }
[void]$sbLife.AppendLine('')
[void]$sbLife.AppendLine('namespace JustyBaseLegacy.UI')
[void]$sbLife.AppendLine('{')
[void]$sbLife.AppendLine('    public partial class BaseWindow')
[void]$sbLife.AppendLine('    {')
foreach ($b in $lifeBody) { [void]$sbLife.AppendLine($b) }
[void]$sbLife.AppendLine('    }')
[void]$sbLife.AppendLine('}')
[System.IO.File]::WriteAllText($lifePath, $sbLife.ToString(), [System.Text.UTF8Encoding]::new($false))

# Move _normalXimage block into grid partial (append before closing)
$imgBlock = New-Object System.Collections.Generic.List[string]
for ($i = $imgStart; $i -le $imgEnd; $i++) { [void]$imgBlock.Add($lines[$i]) }
$gridContent = Get-Content -LiteralPath $gridPath -Encoding UTF8
$gridList = [System.Collections.Generic.List[string]]$gridContent
$gridList.Insert($gridList.Count - 2, '')
foreach ($b in $imgBlock) { $gridList.Insert($gridList.Count - 2, $b) }
[System.IO.File]::WriteAllLines($gridPath, $gridList, [System.Text.UTF8Encoding]::new($false))

# Remove from main (reverse order)
$toRemove = [System.Collections.Generic.HashSet[int]]::new()
foreach ($idx in ($lifeStart..$lifeEnd) + ($notifyStart..$notifyEnd) + ($doMsgStart..$doMsgEnd) + ($onScreenStart..$onScreenEnd) + ($gridStart..$gridEnd) + ($imgStart..$imgEnd)) {
    [void]$toRemove.Add($idx)
}
foreach ($idx in ($toRemove | Sort-Object -Descending)) { $lines.RemoveAt($idx) }

[System.IO.File]::WriteAllLines($mainPath, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Host "Fixed. BaseWindow.cs lines:" $lines.Count
