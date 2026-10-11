$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$trimPathCharacters = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
$allowedPackageIds = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$allowedPackageIds.Add('JustyBase.NetezzaDriver') | Out-Null
$allowedPackageIds.Add('JustyBase.Db2.IBMCompat') | Out-Null

$violations = [System.Collections.Generic.List[string]]::new()
$projectFiles = Get-ChildItem -LiteralPath $repositoryRoot -Filter '*.csproj' -File -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

foreach ($projectFile in $projectFiles) {
    [xml]$project = Get-Content -LiteralPath $projectFile.FullName -Raw
    foreach ($reference in $project.SelectNodes("//*[local-name()='PackageReference']")) {
        $packageId = $reference.GetAttribute('Include')
        if (-not $packageId) {
            $packageId = $reference.GetAttribute('Update')
        }

        if ($packageId.StartsWith('JustyBase.', [System.StringComparison]::OrdinalIgnoreCase) -and
            -not $allowedPackageIds.Contains($packageId)) {
            $relativePath = $projectFile.FullName.Substring($repositoryRoot.Length).TrimStart($trimPathCharacters)
            $violations.Add("$relativePath references internal NuGet package '$packageId'.")
        }
    }
}

$centralPackagesFile = Join-Path $repositoryRoot 'Directory.Packages.props'
[xml]$centralPackages = Get-Content -LiteralPath $centralPackagesFile -Raw
foreach ($packageVersion in $centralPackages.SelectNodes("//*[local-name()='PackageVersion']")) {
    $packageId = $packageVersion.GetAttribute('Include')
    if ($packageId.StartsWith('JustyBase.', [System.StringComparison]::OrdinalIgnoreCase) -and
        -not $allowedPackageIds.Contains($packageId)) {
        $violations.Add("Directory.Packages.props versions internal NuGet package '$packageId'.")
    }
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'No internal JustyBase NuGet references found outside the allowed driver packages.'
