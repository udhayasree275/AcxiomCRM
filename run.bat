@echo off
title AcxiomCRM Server (.NET 8)
echo ========================================================
echo Starting AcxiomCRM Enterprise Application (.NET 8)
echo ========================================================
echo Web Application: http://127.0.0.1:5000
echo Swagger OpenAPI: http://127.0.0.1:5000/swagger
echo --------------------------------------------------------
echo Default Credentials:
echo   Admin:           admin@acxiomcrm.com  / Admin@123!
echo   Manager:         manager@acxiomcrm.com / Manager@123!
echo   Sales Executive: sales@acxiomcrm.com   / Sales@123!
echo ========================================================

set PATH=C:\Users\udhay\.dotnet;%PATH%
set DOTNET_CLI_HOME=%~dp0.dotnet_home
set NUGET_PACKAGES=%~dp0.nuget_packages
set ASPNETCORE_URLS=http://127.0.0.1:5000;http://localhost:5000
set ASPNETCORE_ENVIRONMENT=Development

cd /d "%~dp0AcxiomCRM"

echo Launching browser in 2 seconds... Keep this console open!
start /b cmd /c "timeout /t 2 /nobreak >nul && start http://127.0.0.1:5000"

"C:\Users\udhay\.dotnet\dotnet.exe" "bin\Debug\net8.0\AcxiomCRM.dll"
if %errorlevel% neq 0 (
    echo.
    echo Server stopped or port 5000 already in use.
    pause
)
