[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

$trackedSensitivePatterns = @(
    '\.pfx$',
    '\.p12$',
    '\.pem$',
    '\.key$',
    '\.env$',
    '\.user$',
    'appsettings\.Production\.json$',
    'connectionprofiles?\.json$'
)

$tracked = @(git -C $repoRoot ls-files)
$blocked = @($tracked | Where-Object {
    $path = $_
    foreach ($pattern in $trackedSensitivePatterns) {
        if ($path -match $pattern) { return $true }
    }
    return $false
})

if ($blocked.Count -gt 0) {
    throw ("Tracked files that should not be published:`n{0}" -f ($blocked -join "`n"))
}

$grepPatterns = @(
    'AKIA[0-9A-Z]{16}',
    'BEGIN (RSA |OPENSSH )?PRIVATE KEY',
    'xox[baprs]-[0-9A-Za-z-]+'
)

$sourceFiles = @($tracked | Where-Object { $_ -match '\.(cs|json|xml|config|yml|yaml|ps1|env)$' })
foreach ($pattern in $grepPatterns) {
    $hits = [System.Collections.Generic.List[string]]::new()
    foreach ($relativePath in $sourceFiles) {
        $full = Join-Path $repoRoot $relativePath
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
            continue
        }

        if (Select-String -LiteralPath $full -Pattern $pattern -Quiet) {
            $hits.Add($relativePath)
        }
    }

    if ($hits.Count -gt 0) {
        throw ("Pattern '$pattern' matched tracked files: {0}" -f ($hits -join ', '))
    }
}

Write-Host 'No blocked tracked paths or high-risk secret patterns found in source extensions.'
