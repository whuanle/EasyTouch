@echo off
chcp 65001 >nul
echo ==========================================
echo EasyTouch Build Script - All Platforms
echo ==========================================
echo.

set BUILD_TYPE=%1
set ARCH=%2

if "%BUILD_TYPE%"=="" set BUILD_TYPE=all

if "%BUILD_TYPE%"=="all" goto :build_all
if "%BUILD_TYPE%"=="windows" goto :build_windows
if "%BUILD_TYPE%"=="linux" goto :build_linux
if "%BUILD_TYPE%"=="mac" goto :build_mac
echo Usage: build.bat [all^|windows^|linux^|mac] [arch]
echo   For Mac: arch can be 'x64', 'arm64', or 'universal'
goto :end

:build_all
:build_windows
echo [1/4] Building Windows version...
cd EasyTouch-Windows
call build.bat
cd ..
if not exist skills\windows mkdir skills\windows
copy EasyTouch-Windows\SKILL.md skills\windows\ > nul 2>&1
copy README.md skills\README.md > nul 2>&1
del /f /q skills\windows\et.exe > nul 2>&1
if "%BUILD_TYPE%"=="windows" goto :done

:build_linux
echo.
echo [2/4] Building Linux version...
cd EasyTouch-Linux
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true
if not exist ..\skills\linux mkdir ..\skills\linux
cd ..
copy EasyTouch-Linux\SKILL.md skills\linux\ > nul 2>&1
copy README.md skills\README.md > nul 2>&1
del /f /q skills\linux\et-x64 > nul 2>&1
del /f /q skills\linux\et > nul 2>&1
if "%BUILD_TYPE%"=="linux" goto :done

:build_mac
echo.
echo [3/4] Building macOS Intel (x64) version...
cd EasyTouch-Mac
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishAot=true > nul
if not exist ..\skills\mac mkdir ..\skills\mac
cd ..

echo.
echo [4/4] Building macOS Apple Silicon (arm64) version...
cd EasyTouch-Mac
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishAot=true > nul
cd ..
copy EasyTouch-Mac\SKILL.md skills\mac\ > nul 2>&1
copy README.md skills\README.md > nul 2>&1
del /f /q skills\mac\et-x64 > nul 2>&1
del /f /q skills\mac\et-arm64 > nul 2>&1
del /f /q skills\mac\et > nul 2>&1

if "%BUILD_TYPE%"=="mac" goto :done

:done
echo.
echo ==========================================
echo Build completed!
echo.
echo Skills docs synced:
echo   skills/README.md         (from root README.md)
echo   skills/windows/SKILL.md  (Windows docs)
echo   skills/linux/SKILL.md    (Linux docs)
echo   skills/mac/SKILL.md      (macOS docs)
echo ==========================================

:end
pause
