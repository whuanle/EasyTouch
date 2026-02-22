@echo off
chcp 65001 >nul
echo ==========================================
echo EasyTouch Windows - Build Script
echo ==========================================
echo.

REM Check if dotnet is installed
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo Error: .NET SDK is not installed or not in PATH
    echo Please install .NET 10 SDK from https://dotnet.microsoft.com/download
    exit /b 1
)

echo Cleaning previous builds...
dotnet clean EasyTouch-Windows.csproj -c Release
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo.
echo Restoring packages...
dotnet restore EasyTouch-Windows.csproj

echo.
echo Building project (AOT compilation)...
dotnet publish EasyTouch-Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true

if %errorlevel% neq 0 (
    echo.
    echo Build failed!
    exit /b 1
)

echo.
echo ==========================================
echo Build successful!
echo ==========================================
echo Output: bin\Release\net10.0\win-x64\publish\et.exe

REM Copy to dist folder
if not exist dist mkdir dist
copy /y bin\Release\net10.0\win-x64\publish\et.exe dist\
copy /y README.md dist\
copy /y SKILL.md dist\
copy /y package.json dist\

echo.
echo Distribution files copied to: dist\
dir dist\

echo.
echo Done!
pause
