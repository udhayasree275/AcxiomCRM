$ErrorActionPreference = "Stop"
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Starting AcxiomCRM Enterprise Application (.NET 8)" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Web Interface: http://localhost:5000" -ForegroundColor Green
Write-Host "Swagger UI:    http://localhost:5000/swagger" -ForegroundColor Green
Write-Host "--------------------------------------------------------"
Write-Host "Default User Accounts:"
Write-Host "  Admin:           admin@acxiomcrm.com  / Admin@123!" -ForegroundColor Yellow
Write-Host "  Manager:         manager@acxiomcrm.com / Manager@123!" -ForegroundColor Yellow
Write-Host "  Sales Executive: sales@acxiomcrm.com   / Sales@123!" -ForegroundColor Yellow
Write-Host "========================================================"
Write-Host "Starting server... Keep this window open while using the app!`n" -ForegroundColor Cyan

$env:Path = "C:\Users\udhay\.dotnet;" + $env:Path
$env:DOTNET_CLI_HOME = "$PSScriptRoot\.dotnet_home"
$env:NUGET_PACKAGES = "$PSScriptRoot\.nuget_packages"
$env:ASPNETCORE_URLS = "http://localhost:5000"
$env:ASPNETCORE_ENVIRONMENT = "Development"

Set-Location "$PSScriptRoot\AcxiomCRM"

Start-Job -ScriptBlock {
    Start-Sleep -Seconds 2
    Start-Process "http://localhost:5000"
} | Out-Null

$dll = "$PSScriptRoot\AcxiomCRM\bin\Debug\net8.0\AcxiomCRM.dll"
if (Test-Path $dll) {
    & "C:\Users\udhay\.dotnet\dotnet.exe" $dll
} else {
    & "C:\Users\udhay\.dotnet\dotnet.exe" run
}
