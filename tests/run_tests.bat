@echo off
echo ========================================================
echo Running AcxiomCRM End-to-End Test Suite
echo ========================================================
set PATH=C:\Users\udhay\.dotnet;%PATH%
set DOTNET_CLI_HOME=%~dp0..\.dotnet_home
set NUGET_PACKAGES=%~dp0..\.nuget_packages

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0test_runner.ps1"
pause
