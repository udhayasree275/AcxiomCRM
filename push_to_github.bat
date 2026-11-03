@echo off
set PATH=%~dp0..\.git_tools\cmd;%PATH%
title Push AcxiomCRM to GitHub
echo ========================================================
echo Pushing AcxiomCRM to:
echo https://github.com/udhayasree275/AcxiomCRM.git
echo ========================================================
echo.

cd /d "%~dp0"
git remote remove origin 2>nul
git remote add origin https://github.com/udhayasree275/AcxiomCRM.git
git branch -M main

echo Starting push to GitHub...
echo (If a GitHub sign-in window appears in your browser, please approve it)
echo.
git push -u origin main

if %errorlevel% equ 0 (
    echo.
    echo ========================================================
    echo [SUCCESS] Code successfully pushed to GitHub!
    echo Visit: https://github.com/udhayasree275/AcxiomCRM
    echo ========================================================
) else (
    echo.
    echo ========================================================
    echo [NOTICE] If prompted for authentication, you can also use
    echo a GitHub Personal Access Token (PAT).
    echo ========================================================
)
echo.
pause
