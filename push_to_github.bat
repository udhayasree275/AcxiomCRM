@echo off
set PATH=%~dp0..\.git_tools\cmd;%PATH%
echo ========================================================
echo Push AcxiomCRM to GitHub
echo ========================================================
echo.
set /p REPO_URL="Enter your GitHub Repository URL (e.g., https://github.com/YOUR_USERNAME/AcxiomCRM.git): "
if "%REPO_URL%"=="" (
    echo No repository URL provided. Aborting.
    pause
    exit /b 1
)

echo Adding remote origin...
git remote remove origin 2>nul
git remote add origin %REPO_URL%
git branch -M main

echo.
echo Pushing main branch to GitHub...
git push -u origin main

if %errorlevel% equ 0 (
    echo.
    echo ========================================================
    echo [SUCCESS] Successfully pushed to GitHub!
    echo ========================================================
) else (
    echo.
    echo [FAILED] Push failed. Please check your credentials or GitHub Personal Access Token.
)
pause
