param(
    [string]$SourceDir = "$PSScriptRoot\..\bin\Release\net8.0\BenchmarkDotNet.Artifacts\results",
    [string]$OutputDir = "$PSScriptRoot\..\BenchmarkDotNet.Artifacts\results"
)

$Invariant = [System.Globalization.CultureInfo]::InvariantCulture

function Parse-Nanoseconds([string]$Value) {
    $v = $Value.Trim('"').Replace(',', '').Trim()
    if ($v -match '^([\d.]+)\s*(ns|us|μs|ms|s)$') {
        $num = [double]::Parse($Matches[1], $Invariant)
        switch ($Matches[2]) {
            'ns' { return $num }
            'us' { return $num * 1e3 }
            'μs' { return $num * 1e3 }
            'ms' { return $num * 1e6 }
            's'  { return $num * 1e9 }
        }
    }
    throw "Cannot parse time value: $Value"
}

function Parse-Bytes([string]$Value) {
    $v = $Value.Trim('"').Trim()
    if ($v -match '^([\d.]+)\s*(B|KB|MB)$') {
        $num = [double]::Parse($Matches[1], $Invariant)
        switch ($Matches[2]) {
            'B'  { return $num }
            'KB' { return $num * 1024 }
            'MB' { return $num * 1024 * 1024 }
        }
    }
    throw "Cannot parse size value: $Value"
}

function Format-Nanoseconds([double]$Ns) {
    if ($Ns -ge 1000) {
        return $Ns.ToString('N2', $Invariant) + ' ns'
    }
    return $Ns.ToString('N2', $Invariant) + ' ns'
}

function Format-Bytes([double]$Bytes) {
    return [math]::Round($Bytes).ToString('N0', $Invariant) + ' B'
}

function Format-Ratio([double]$Value) {
    return $Value.ToString('N2', $Invariant)
}

function Format-Gen1($Row) {
    $gen1 = $null
    if ($Row.PSObject.Properties.Name -contains 'Gen1') {
        $gen1 = $Row.Gen1
    }
    if ([string]::IsNullOrWhiteSpace($gen1) -or $gen1 -eq '0.0000' -or $gen1 -eq '0') {
        return '-'
    }
    return $gen1
}

function Quote-CsvIfNeeded([string]$Value) {
    if ($Value -match '[,]') {
        return '"' + $Value + '"'
    }
    return $Value
}

$csvFiles = Get-ChildItem -Path $SourceDir -Filter '*-report.csv' | Sort-Object Name
if ($csvFiles.Count -eq 0) {
    throw "No *-report.csv files found in $SourceDir"
}

$benchmarks = @()
foreach ($file in $csvFiles) {
    $typeName = $file.BaseName -replace '^RailwayHelper\.Benchmarks\.', '' -replace '-report$', ''
    $rows = Import-Csv -Path $file.FullName -Delimiter ';'
    foreach ($row in $rows) {
        $benchmarks += [PSCustomObject]@{
            Type       = $typeName
            Method     = $row.Method
            Row        = $row
            MeanNs     = Parse-Nanoseconds $row.Mean
            StdDevNs   = Parse-Nanoseconds $row.StdDev
            AllocBytes = Parse-Bytes $row.Allocated
        }
    }
}

$baseline = $benchmarks | Where-Object { $_.Type -eq 'DoNextChainSmallBenchmarks' -and $_.Method -eq 'Baseline' } | Select-Object -First 1
if (-not $baseline) {
    throw 'DoNextChainSmallBenchmarks.Baseline not found — cannot compute global ratios.'
}

$baselineMeanNs = $baseline.MeanNs
$baselineAllocBytes = $baseline.AllocBytes

$ordered = @(
    ($benchmarks | Where-Object { $_.Method -eq 'Baseline' } | Sort-Object Type)
    ($benchmarks | Where-Object { $_.Method -eq 'Railway' } | Sort-Object Type)
) | ForEach-Object { $_ }

$githubMdPath = Get-ChildItem -Path $SourceDir -Filter '*-report-github.md' | Sort-Object Name | Select-Object -First 1
$headerLines = @()
if ($githubMdPath) {
    $inHeader = $false
    foreach ($line in Get-Content -Path $githubMdPath.FullName) {
        if ($line -eq '```') {
            if (-not $inHeader) { $inHeader = $true; continue }
            break
        }
        if ($inHeader) { $headerLines += $line }
    }
}

$timestamp = Get-Date -Format 'yyyy-MM-dd-HH-mm-ss'
$baseName = "BenchmarkRun-joined-$timestamp"
$outputCsv = Join-Path $OutputDir "$baseName-report.csv"
$outputMd = Join-Path $OutputDir "$baseName-report-github.md"
$outputHtml = Join-Path $OutputDir "$baseName-report.html"

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$templateRow = $ordered | ForEach-Object { $_.Row } | Where-Object { $_.PSObject.Properties.Name -contains 'Gen1' } | Select-Object -First 1
if (-not $templateRow) {
    $templateRow = $ordered[0].Row
}

$csvHeader = @('Type') + ($templateRow.PSObject.Properties.Name)
if ($csvHeader -notcontains 'Gen1') {
    $idx = [array]::IndexOf($csvHeader, 'Gen0')
    if ($idx -ge 0) {
        $csvHeader = $csvHeader[0..$idx] + @('Gen1') + $csvHeader[($idx + 1)..($csvHeader.Length - 1)]
    }
}

$csvLines = New-Object System.Collections.Generic.List[string]
$csvLines.Add(($csvHeader -join ';'))

$mdRows = New-Object System.Collections.Generic.List[string]
$htmlRows = New-Object System.Collections.Generic.List[string]

foreach ($item in $ordered) {
    $row = $item.Row
    $ratio = $item.MeanNs / $baselineMeanNs
    $ratioSd = $item.StdDevNs / $baselineMeanNs
    $allocRatio = $item.AllocBytes / $baselineAllocBytes
    $mean = Format-Nanoseconds $item.MeanNs
    $allocated = Format-Bytes $item.AllocBytes
    $gen1 = Format-Gen1 $row

    $values = New-Object System.Collections.Generic.List[string]
    $values.Add($item.Type)
    foreach ($propName in $templateRow.PSObject.Properties.Name) {
        $val = if ($row.PSObject.Properties.Name -contains $propName) { $row.$propName } else { '' }
        switch ($propName) {
            'Mean' { $val = (Quote-CsvIfNeeded $mean) }
            'Ratio' { $val = Format-Ratio $ratio }
            'RatioSD' { $val = Format-Ratio $ratioSd }
            'Allocated' { $val = $allocated }
            'Alloc Ratio' { $val = Format-Ratio $allocRatio }
            'Gen1' { $val = if ($gen1 -eq '-') { '0.0000' } else { $gen1 } }
            'Error' { $val = Quote-CsvIfNeeded $val }
            'StdDev' { $val = Quote-CsvIfNeeded $val }
            default { }
        }
        $values.Add($val)
    }
    $csvLines.Add(($values -join ';'))

    $mdRows.Add(('| {0,-37} | {1,-8} | {2,11} | {3,10} | {4,10} | {5,6} | {6,7} | {7,6} | {8,6} | {9,9} | {10,11} |' -f `
            $item.Type, $item.Method, $mean, $row.Error, $row.StdDev, (Format-Ratio $ratio), (Format-Ratio $ratioSd), $row.Gen0, $gen1, $allocated, (Format-Ratio $allocRatio)))

    $htmlRows.Add("<tr><td>$($item.Type)</td><td>$($item.Method)</td><td>$mean</td><td>$($row.Error)</td><td>$($row.StdDev)</td><td>$(Format-Ratio $ratio)</td><td>$(Format-Ratio $ratioSd)</td><td>$($row.Gen0)</td><td>$gen1</td><td>$allocated</td><td>$(Format-Ratio $allocRatio)</td>`n</tr>")
}

Set-Content -Path $outputCsv -Value $csvLines -Encoding UTF8

$mdContent = @"
``````
$($headerLines -join "`n")


``````
| Type                                  | Method   | Mean        | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |--------- |------------:|-----------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
$($mdRows -join "`n")
"@
Set-Content -Path $outputMd -Value $mdContent -Encoding UTF8

$htmlContent = @"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<title>$baseName</title>

<style type="text/css">
	table { border-collapse: collapse; display: block; width: 100%; overflow: auto; }
	td, th { padding: 6px 13px; border: 1px solid #ddd; text-align: right; }
	tr { background-color: #fff; border-top: 1px solid #ccc; }
	tr:nth-child(even) { background: #f8f8f8; }
</style>
</head>
<body>
<pre><code>
$($headerLines -join "`n")
</code></pre>
<pre><code></code></pre>

<table>
<thead><tr><th>Type                           </th><th>Method</th><th>Mean </th><th>Error</th><th>StdDev</th><th>Ratio</th><th>RatioSD</th><th>Gen0</th><th>Gen1</th><th>Allocated</th><th>Alloc Ratio</th>
</tr>
</thead><tbody>$($htmlRows -join '')</tbody></table>
</body>
</html>
"@
Set-Content -Path $outputHtml -Value $htmlContent -Encoding UTF8

Write-Host "Joined reports written to:"
Write-Host "  $outputCsv"
Write-Host "  $outputMd"
Write-Host "  $outputHtml"
