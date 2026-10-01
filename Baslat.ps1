$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 9 SDK kurulu olmali.'
}
if (-not (Get-Command SqlLocalDB -ErrorAction SilentlyContinue)) {
    throw 'SQL Server Express LocalDB kurulu olmali.'
}

SqlLocalDB start MSSQLLocalDB
if ($LASTEXITCODE -ne 0) { throw 'LocalDB baslatilamadi.' }

Write-Host 'Site: http://localhost:5157'
Write-Host 'Durdurmak icin Ctrl+C kullanin.'
dotnet run --project WebUI/WebUI.csproj --launch-profile Development
if ($LASTEXITCODE -ne 0) { throw 'Uygulama baslatilamadi. Yukaridaki hatayi kontrol edin.' }
