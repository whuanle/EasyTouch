@echo off
chcp 65001 >nul
echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║     EasyTouch Browser Automation Tests                    ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

set SCRIPT_DIR=%~dp0
node "%SCRIPT_DIR%test-browser.js" %*

exit /b %errorlevel%
