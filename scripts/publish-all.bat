@echo off
chcp 65001 >nul
echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║     EasyTouch NPM Publisher - All Packages                ║
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

REM Build main package
echo.
echo 📦 Building main package...
call "%SCRIPT_DIR%publish-npm-main.bat" %VERSION%
if errorlevel 1 (
    echo ❌ Main package build failed!
    exit /b 1
)

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║  ✅ All packages built successfully!                        ║
echo ╚════════════════════════════════════════════════════════════╝
echo.
echo 📁 Distribution directories:
echo    - npm-dist-main/          (Main package: easytouch)
echo    - npm-dist-win-x64/       (Windows: easytouch-windows)
echo.
echo 🚀 To publish to NPM:
echo.
echo    # 1. Publish platform package first
echo    cd npm-dist-win-x64
echo    npm publish --access public
echo    cd ..
echo.
echo    # 2. Then publish main package
echo    cd npm-dist-main
echo    npm publish --access public
echo.
echo 🧪 To test locally:
echo    cd npm-dist-main
echo    npm link
echo    et --help
echo.

pause
