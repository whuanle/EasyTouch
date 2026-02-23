@echo off
chcp 65001 > nul
setlocal EnableDelayedExpansion

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║     EasyTouch NPM Publisher - Main Package                ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

set VERSION=%1
if "%VERSION%"=="" (
    echo Usage: %~nx0 ^<version^>
    echo Example: %~nx0 1.0.0
    exit /b 1
)

set SCRIPT_DIR=%~dp0
set PROJECT_DIR=%SCRIPT_DIR%..
set TEMP_DIR=%TEMP%\easytouch-npm-main-%RANDOM%
set DIST_DIR=npm-dist-main

echo 📦 Version: %VERSION%
echo 📁 Temp directory: %TEMP_DIR%
echo.

:: 创建临时目录
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%"
mkdir "%TEMP_DIR%"

:: 1. 从 npx/main 复制基础文件
echo 📋 Copying package template from npx/main...
if not exist "%PROJECT_DIR%\npx\main\package.json" (
    echo ❌ Error: npx/main/package.json not found!
    exit /b 1
)

copy "%PROJECT_DIR%\npx\main\package.json" "%TEMP_DIR%\package.json" > nul
copy "%PROJECT_DIR%\npx\main\install.js" "%TEMP_DIR%\install.js" > nul 2>&1
copy "%PROJECT_DIR%\npx\main\test.js" "%TEMP_DIR%\test.js" > nul 2>&1

:: 更新版本号
powershell -Command "(Get-Content '%TEMP_DIR%\package.json') -replace '\"version\": \"[^\"]*\"', '\"version\": \"%VERSION%\"' | Set-Content '%TEMP_DIR%\package.json'"

:: 2. 复制 bin 目录
echo 📋 Copying bin scripts...
if exist "%PROJECT_DIR%\npx\main\bin" (
    xcopy /E /I "%PROJECT_DIR%\npx\main\bin" "%TEMP_DIR%\bin" > nul 2>&1
)

:: 3. 创建 README
echo 📋 Creating README.md...
copy "%PROJECT_DIR%\docs\NPM_TEST_GUIDE.md" "%TEMP_DIR%\README.md" > nul 2>&1

:: 4. 移动到 dist 目录
echo 📦 Moving to distribution directory...
if exist "%PROJECT_DIR%\%DIST_DIR%" rmdir /s /q "%PROJECT_DIR%\%DIST_DIR%"
move "%TEMP_DIR%" "%PROJECT_DIR%\%DIST_DIR%"

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║  ✅ NPM Main Package Ready!                                 ║
echo ╚════════════════════════════════════════════════════════════╝
echo.
echo 📁 Location: .\%DIST_DIR%\
echo 📦 Package: easytouch@%VERSION%
echo.
echo 🚀 To publish to NPM:
echo    cd %DIST_DIR%
echo    npm publish --access public
echo.
echo 🧪 To test locally:
echo    cd %DIST_DIR%
echo    npm link
echo    et --help
echo.

endlocal
