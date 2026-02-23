@echo off
chcp 65001 >nul
echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║   EasyTouch NPM Publisher - Platform Packages             ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

set VERSION=%1
if "%VERSION%"=="" (
    echo Usage: %~nx0 ^<version^>
    echo Example: %~nx0 1.0.0
    exit /b 1
)

echo 📦 Publishing version: %VERSION%
echo.

set SCRIPT_DIR=%~dp0

REM Build Windows package
echo 🔨 Building Windows package...
call "%SCRIPT_DIR%publish-npm-win-x64.bat" %VERSION%
if errorlevel 1 (
    echo ❌ Windows package build failed!
    exit /b 1
)

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║  ✅ Platform package build successful!                      ║
echo ╚════════════════════════════════════════════════════════════╝
echo.
echo 📁 Distribution directories:
echo    - npm-dist-win-x64/       (Windows: easytouch-windows)
echo.
echo 🚀 To publish to NPM:
echo.
echo    cd npm-dist-win-x64
echo    npm publish --access public
echo.
echo 🧪 To test locally:
echo    cd npm-dist-win-x64
echo    npm link
echo    et --help
echo.

pause
