@echo off
chcp 65001 >nul
echo ==========================================
echo EasyTouch Windows - Publish to npm
echo ==========================================
echo.

REM 1. Build the project
echo [1/5] Building project...
call build.bat
if %errorlevel% neq 0 (
    echo Build failed!
    exit /b 1
)

REM 2. Check if logged in
echo.
echo [2/5] Checking npm login status...
npm whoami >nul 2>nul
if %errorlevel% neq 0 (
    echo Please login to npm first:
    echo npm login
    exit /b 1
)
echo Already logged in.

REM 3. Preview package contents
echo.
echo [3/5] Previewing package contents...
cd dist
npm pack --dry-run
cd ..

REM 4. Confirm publish
echo.
echo [4/5] Ready to publish
echo Package: easytouch-windows
echo Version: 1.0.0
echo.
set /p confirm="Do you want to publish? (yes/no): "
if /i not "%confirm%"=="yes" (
    echo Publish cancelled.
    exit /b 0
)

REM 5. Publish
echo.
echo [5/5] Publishing to npm...
cd dist
npm publish --access public
if %errorlevel% neq 0 (
    echo.
    echo Publish failed!
    cd ..
    exit /b 1
)

cd ..
echo.
echo ==========================================
echo Publish successful!
echo ==========================================
echo.
echo You can now install it with:
echo npm install -g easytouch-windows
echo.

pause
