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
copy EasyTouch-Windows\dist\et.exe skills\windows\ > nul 2>&1
copy EasyTouch-Windows\SKILL.md skills\windows\ > nul 2>&1
if "%BUILD_TYPE%"=="windows" goto :done

:build_linux
echo.
echo [2/4] Building Linux version...
cd EasyTouch-Linux
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true
if not exist ..\skills\linux mkdir ..\skills\linux
copy bin\Release\net10.0\linux-x64\publish\et ..\skills\linux\et-x64 > nul 2>&1
cd ..
copy EasyTouch-Linux\SKILL.md skills\linux\ > nul 2>&1
if "%BUILD_TYPE%"=="linux" goto :done

:build_mac
echo.
echo [3/4] Building macOS Intel (x64) version...
cd EasyTouch-Mac
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishAot=true -o ..\..\skills\mac\et-x64
if not exist ..\..\skills\mac mkdir ..\..\skills\mac
move ..\..\skills\mac\et-x64\et ..\..\skills\mac\et-x64\et > nul 2>&1
rmdir ..\..\skills\mac\et-x64 > nul 2>&1
cd ..

echo.
echo [4/4] Building macOS Apple Silicon (arm64) version...
cd EasyTouch-Mac
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishAot=true -o ..\..\skills\mac\et-arm64
move ..\..\skills\mac\et-arm64\et ..\..\skills\mac\et-arm64 > nul 2>&1
rmdir ..\..\skills\mac\et-arm64 > nul 2>&1
cd ..
copy EasyTouch-Mac\SKILL.md skills\mac\ > nul 2>&1

if "%BUILD_TYPE%"=="mac" goto :done

:done
echo.
echo ==========================================
echo Build completed!
echo.
echo Output structure:
echo   skills/windows/et.exe    (Windows x64)
echo   skills/linux/et-x64      (Linux x64)
echo   skills/mac/et-x64        (macOS Intel)
echo   skills/mac/et-arm64      (macOS Apple Silicon)
echo ==========================================

:end
pause
