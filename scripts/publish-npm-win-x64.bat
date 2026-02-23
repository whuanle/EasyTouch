@echo off
echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║     EasyTouch NPM Publisher - Windows x64                 ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

if "%~1"=="" (
    echo Usage: %~nx0 ^<version^>
    echo Example: %~nx0 1.0.0
    exit /b 1
)

node "%~dp0publish-npm-win-x64.js" %1
