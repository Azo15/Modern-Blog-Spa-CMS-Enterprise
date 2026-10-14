param(
    [ValidateRange(0, 100)] [double] $MinimumCoverage = 70,
    [string] $Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$runDirectory = Join-Path $workspace ('.artifacts/quality/' + [Guid]::NewGuid().ToString('N'))
$settings = Join-Path $workspace 'tests/coverage.runsettings'
$project = Join-Path $workspace 'BusinessLayer.Tests/BusinessLayer.Tests.csproj'
$buildDirectory = Join-Path $workspace '.artifacts/quality-build'

& dotnet test $project --configuration $Configuration --settings $settings --collect 'XPlat Code Coverage' --results-directory $runDirectory --artifacts-path $buildDirectory --logger 'trx;LogFileName=tests.trx'
if ($LASTEXITCODE -ne 0) { throw "Tests failed. Results: $runDirectory" }
$coverageFiles = @(Get-ChildItem -LiteralPath $runDirectory -Recurse -Filter 'coverage.cobertura.xml')
if ($coverageFiles.Count -eq 0) { throw 'No fresh coverage report was produced.' }
# The Windows TRX logger can deploy an identical attachment under its In/ directory.
# Accept byte-identical copies; never merge or pick between different measurements.
$coverageHashes = @($coverageFiles | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } | Sort-Object -Unique)
if ($coverageHashes.Count -ne 1) { throw 'Conflicting fresh coverage reports were produced.' }
[xml] $coverage = Get-Content -LiteralPath $coverageFiles[0].FullName -Raw
$packages = @($coverage.coverage.packages.package)
if ($packages.Count -ne 1 -or $packages[0].name -ne 'BusinessLayer') { throw 'Coverage must measure the entire BusinessLayer assembly.' }
$valid = [long] $coverage.coverage.'lines-valid'
$covered = [long] $coverage.coverage.'lines-covered'
if ($valid -le 0) { throw 'Coverage report has no measurable production lines.' }
$percent = 100.0 * $covered / $valid
$summary = [ordered]@{
    assembly = 'BusinessLayer'
    coveredLines = $covered
    totalLines = $valid
    coveragePercent = [Math]::Round($percent, 4)
    minimumPercent = $MinimumCoverage
    passed = $percent -ge $MinimumCoverage
    report = $coverageFiles[0].FullName
}
$summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runDirectory 'coverage-summary.json') -Encoding utf8
Write-Host ('BusinessLayer coverage: {0:N2}% ({1}/{2} lines). Report: {3}' -f $percent, $covered, $valid, $coverageFiles[0].FullName)
if ($percent -lt $MinimumCoverage) { throw "Coverage is below $MinimumCoverage%." }
