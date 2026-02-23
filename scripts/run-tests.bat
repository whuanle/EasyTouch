@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

echo ================================================
echo     EasyTouch Cross-Platform Test Runner
echo ================================================
echo.

:: 检测操作系统
set "PLATFORM=Unknown"
set "TEST_PROJECT="
set "MAIN_PROJECT="

if "%OS%"=="Windows_NT" (
    set "PLATFORM=Windows"
    set "TEST_PROJECT=EasyTouch.Tests.Windows"
    set "MAIN_PROJECT=EasyTouch-Windows"
) else (
    echo This script is for Windows only.
    echo For Linux/Mac, please use run-tests.sh
    exit /b 1
)

echo Platform detected: %PLATFORM%
echo Test project: %TEST_PROJECT%
echo Main project: %MAIN_PROJECT%
echo.

:: 检查 .NET SDK
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo Error: .NET SDK not found. Please install .NET 10.0 SDK.
    exit /b 1
)

echo .NET SDK version:
dotnet --version
echo.

:: 设置配置
set "CONFIG=Release"
if not "%~1"=="" set "CONFIG=%~1"

echo Configuration: %CONFIG%
echo.

:: 构建主项目
echo [1/3] Building main project: %MAIN_PROJECT%...
dotnet build %MAIN_PROJECT%\%MAIN_PROJECT%.csproj -c %CONFIG%
if errorlevel 1 (
    echo Error: Failed to build %MAIN_PROJECT%
    exit /b 1
)
echo     ✓ Build successful
echo.

:: 发布 AOT 版本（测试需要）
echo [2/3] Publishing AOT executable...
dotnet publish %MAIN_PROJECT%\%MAIN_PROJECT%.csproj -c %CONFIG% -r win-x64 --self-contained true -p:PublishAot=true
if errorlevel 1 (
    echo Warning: AOT publish failed, tests may use Debug build
) else (
    echo     ✓ AOT publish successful
)
echo.

:: 运行测试
echo [3/3] Running tests: %TEST_PROJECT%...
dotnet test %TEST_PROJECT%\%TEST_PROJECT%.csproj -c %CONFIG% --verbosity normal
if errorlevel 1 (
    echo.
    echo ================================================
    echo     Tests FAILED
    echo ================================================
    exit /b 1
)

echo.
echo ================================================
echo     All tests PASSED ✓
echo ================================================

endlocal
