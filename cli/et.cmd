@echo off
setlocal

set SCRIPT_DIR=%~dp0
set BIN_DIR=%SCRIPT_DIR%..\bin
set ET_EXE=%BIN_DIR%\et.exe

if not exist "%ET_EXE%" (
    echo ❌ EasyTouch binary not found. Please run: npm install
    exit /b 1
)

"%ET_EXE%" %*
exit /b %errorlevel%
