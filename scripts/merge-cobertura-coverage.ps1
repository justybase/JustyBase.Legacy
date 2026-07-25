[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResultsDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ResultsDirectory -PathType Container)) {
    throw "Test results directory was not found: $ResultsDirectory"
}

$reports = @(Get-ChildItem -LiteralPath $ResultsDirectory -Recurse -Filter coverage.cobertura.xml)
if ($reports.Count -eq 0) {
    throw 'No Cobertura reports were produced.'
}

# Keyed by assembly/file/line so the same source line covered by more than one
# test project is counted once.  Hits are accumulated only to retain useful
# Cobertura data; the quality gate uses the unique executable line count.
$lines = @{}
foreach ($reportFile in $reports) {
    [xml]$report = Get-Content -LiteralPath $reportFile.FullName -Raw
    foreach ($package in @($report.coverage.packages.package)) {
        foreach ($class in @($package.classes.class)) {
            $filename = [string]$class.filename
            foreach ($line in @($class.lines.line)) {
                $number = [int]$line.number
                $key = "$($package.name)`u{001f}$filename`u{001f}$number"
                $hits = [int]$line.hits
                if ($lines.ContainsKey($key)) {
                    $lines[$key].Hits += $hits
                }
                else {
                    $lines[$key] = [pscustomobject]@{
                        Package = [string]$package.name
                        Filename = $filename
                        Number = $number
                        Hits = $hits
                    }
                }
            }
        }
    }
}

$document = New-Object System.Xml.XmlDocument
$declaration = $document.CreateXmlDeclaration('1.0', 'utf-8', $null)
[void]$document.AppendChild($declaration)
$coverage = $document.CreateElement('coverage')
$coverage.SetAttribute('line-rate', '0')
[void]$document.AppendChild($coverage)
$packagesNode = $document.CreateElement('packages')
[void]$coverage.AppendChild($packagesNode)

$covered = 0
$valid = 0
foreach ($packageGroup in @($lines.Values | Group-Object Package | Sort-Object Name)) {
    $package = $document.CreateElement('package')
    $package.SetAttribute('name', $packageGroup.Name)
    [void]$packagesNode.AppendChild($package)
    $classes = $document.CreateElement('classes')
    [void]$package.AppendChild($classes)

    foreach ($fileGroup in @($packageGroup.Group | Group-Object Filename | Sort-Object Name)) {
        $class = $document.CreateElement('class')
        $class.SetAttribute('name', $fileGroup.Name)
        $class.SetAttribute('filename', $fileGroup.Name)
        [void]$classes.AppendChild($class)
        $lineNodes = $document.CreateElement('lines')
        [void]$class.AppendChild($lineNodes)
        foreach ($line in @($fileGroup.Group | Sort-Object Number)) {
            $lineNode = $document.CreateElement('line')
            $lineNode.SetAttribute('number', [string]$line.Number)
            $lineNode.SetAttribute('hits', [string]$line.Hits)
            [void]$lineNodes.AppendChild($lineNode)
            $valid++
            if ($line.Hits -gt 0) { $covered++ }
        }
    }
}

if ($valid -gt 0) {
    $coverage.SetAttribute('line-rate', [string]($covered / $valid))
}

$outputDirectory = Split-Path -Parent $OutputFile
if ($outputDirectory) {
    [void][System.IO.Directory]::CreateDirectory($outputDirectory)
}
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.Encoding = New-Object System.Text.UTF8Encoding($false)
$writer = [System.Xml.XmlWriter]::Create($OutputFile, $settings)
try { $document.Save($writer) } finally { $writer.Dispose() }
Write-Host "Merged $($reports.Count) Cobertura reports: $covered/$valid unique executable lines."
