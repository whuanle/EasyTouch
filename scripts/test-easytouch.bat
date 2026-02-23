@echo off
chcp 65001 >nul
echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║     EasyTouch Cross-Platform Test Suite                   ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

set SCRIPT_DIR=%~dp0
node "%SCRIPT_DIR%test-easytouch.js" %*

exit /b %errorlevel%
