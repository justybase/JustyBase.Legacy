$path = Join-Path $PSScriptRoot "..\JustData\BaseWindow.cs"
$lines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $path -Encoding UTF8)
$toRemove = New-Object System.Collections.Generic.HashSet[int]
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'gridViewCurrentRow\.Invalidate' -or 
        ($lines[$i] -match '^\s+\}\s*$' -and $i -ge 1155 -and $i -le 1162)) {
        [void]$toRemove.Add($i)
    }
}
foreach ($idx in ($toRemove | Sort-Object -Descending)) { $lines.RemoveAt($idx) }
[System.IO.File]::WriteAllLines($path, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Host "Lines:" $lines.Count
