@echo off
chcp 65001 > nul
setlocal EnableDelayedExpansion

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║     EasyTouch NPM Publisher - Windows x64                 ║
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
set TEMP_DIR=%TEMP%\easytouch-npm-win-x64-%RANDOM%
set DIST_DIR=npm-dist-win-x64

echo 📦 Version: %VERSION%
echo 📁 Temp directory: %TEMP_DIR%
echo.

:: 创建临时目录
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%"
mkdir "%TEMP_DIR%"

:: 1. 从 npx/windows 复制基础文件
echo 📋 Copying package template from npx/windows...
if not exist "%PROJECT_DIR%\npx\windows\package.json" (
    echo ❌ Error: npx/windows/package.json not found!
    exit /b 1
)

copy "%PROJECT_DIR%\npx\windows\package.json" "%TEMP_DIR%\package.json" > nul
copy "%PROJECT_DIR%\npx\windows\SKILL.md" "%TEMP_DIR%\SKILL.md" > nul 2>&1

:: 更新版本号
powershell -Command "(Get-Content '%TEMP_DIR%\package.json') -replace '\"version\": \"[^\"]*\"', '\"version\": \"%VERSION%\"' | Set-Content '%TEMP_DIR%\package.json'"

:: 2. 构建 AOT 可执行文件
echo 🔨 Building AOT executable for win-x64...
dotnet publish "%PROJECT_DIR%\EasyTouch-Windows\EasyTouch-Windows.csproj" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishAot=true ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=true ^
    -p:TrimMode=full ^
    -o "%TEMP_DIR%"

if errorlevel 1 (
    echo ❌ Build failed!
    rmdir /s /q "%TEMP_DIR%" 2> nul
    exit /b 1
)

:: 3. 复制 Playwright 桥接脚本
echo 📋 Copying Playwright bridge script...
if exist "%PROJECT_DIR%\scripts\playwright-bridge.js" (
    mkdir "%TEMP_DIR%\scripts" 2> nul
    copy "%PROJECT_DIR%\scripts\playwright-bridge.js" "%TEMP_DIR%\scripts\" > nul
)

:: 4. 验证文件
echo ✅ Verifying package contents...
if not exist "%TEMP_DIR%\et.exe" (
    echo ❌ Error: et.exe not found after build!
    rmdir /s /q "%TEMP_DIR%" 2> nul
    exit /b 1
)

:: 5. 移动到 dist 目录
echo 📦 Moving to distribution directory...
if exist "%PROJECT_DIR%\%DIST_DIR%" rmdir /s /q "%PROJECT_DIR%\%DIST_DIR%"
move "%TEMP_DIR%" "%PROJECT_DIR%\%DIST_DIR%"

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║  ✅ NPM Package Ready!                                      ║
echo ╚════════════════════════════════════════════════════════════╝
echo.
echo 📁 Location: .\%DIST_DIR%\
echo 📦 Package: easytouch-windows@%VERSION%
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
