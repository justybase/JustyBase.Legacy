$ErrorActionPreference = "Stop"
$bak = Get-Content "$PSScriptRoot\..\JustData\BaseWindow.cs.bak" -Encoding UTF8

function Get-Range([int]$start, [int]$end) {
    $result = New-Object System.Collections.Generic.List[string]
    for ($i = $start - 1; $i -le $end - 1; $i++) {
        if ($i -ge 0 -and $i -lt $bak.Count) { [void]$result.Add($bak[$i]) }
    }
    return ,$result
}

$usingBlock = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $bak.Count; $i++) {
    if ($bak[$i] -match '^namespace ') { break }
    if ($bak[$i] -match '^using ') { [void]$usingBlock.Add($bak[$i]) }
}

$body = New-Object System.Collections.Generic.List[string]
foreach ($r in @(@(2464,4629), @(4912,5038), @(9399,10142))) {
    foreach ($line in (Get-Range $r[0] $r[1])) { [void]$body.Add($line) }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('// BaseWindow SQL execution partial.')
foreach ($u in $usingBlock) { [void]$sb.AppendLine($u) }
[void]$sb.AppendLine('using AppBase.Services.Sql;')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('namespace JustyBaseLegacy.UI')
[void]$sb.AppendLine('{')
[void]$sb.AppendLine('    public partial class BaseWindow')
[void]$sb.AppendLine('    {')
[void]$sb.AppendLine('        private readonly NetezzaSqlErrorHighlighter _nzErrorHighlighter = new();')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('        private void HandleNzErrors(string msg, FastColoredTextBox fctb, int selectionStart, int selectionLength, bool fromOleDB = false)')
[void]$sb.AppendLine('        {')
[void]$sb.AppendLine('            _nzErrorHighlighter.Highlight(msg, fctb, _colorTheme.CurrentFctbColors.ErrorStyle, selectionStart, selectionLength, fromOleDB);')
[void]$sb.AppendLine('        }')
[void]$sb.AppendLine('')
foreach ($b in $body) { [void]$sb.AppendLine($b) }

# Service integration overrides at end (before closing braces)
[void]$sb.AppendLine('')
[void]$sb.AppendLine('        public async Task RunNzSQL(bool keepConnectionOpen, int mode = 0, ExportOptions opcjaEksportu = ExportOptions.grid, bool explain = false, string filePath = null) =>')
[void]$sb.AppendLine('            await _netezzaSqlExecutionService.RunAsync(this, keepConnectionOpen, mode, opcjaEksportu, explain, filePath);')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('        Task ISqlExecutionHost.RunNetezzaSqlAsync(bool keepConnectionOpen, int mode, ExportOptions exportOption, bool explain, string filePath) =>')
[void]$sb.AppendLine('            RunNzSQLCore(keepConnectionOpen, mode, exportOption, explain, filePath);')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('        Task ISqlExecutionHost.RunGeneralSqlAsync(bool keepConnectionOpen, int mode, ExportOptions exportOption) =>')
[void]$sb.AppendLine('            RunForGeneralEx(keepConnectionOpen, mode, exportOption);')
[void]$sb.AppendLine('    }')
[void]$sb.AppendLine('}')

$out = "$PSScriptRoot\..\JustData\BaseWindow.SqlExecution.cs"
[System.IO.File]::WriteAllText($out, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Rebuilt $out with $($body.Count) body lines"
