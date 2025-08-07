@echo off
echo ========================================
echo Starting MyDataHelper (Release Mode)
echo ========================================
echo.

cd /d "%~dp0\.."

echo Checking .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Building application (Release)...
cd src\MyDataHelper
dotnet build --configuration Release >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Build failed. Running with detailed output...
    echo.
    dotnet build --configuration Release
    pause
    exit /b 1
)

echo Starting MyDataHelper...
echo.
echo The application will start in the system tray.
echo Look for the disk icon in your system tray (near the clock).
echo.
echo To access the web interface, open: http://localhost:5250
echo.
echo Press Ctrl+C to stop the application.
echo ========================================
echo.

set ASPNETCORE_ENVIRONMENT=Production
dotnet run --no-build --configuration Release

pause