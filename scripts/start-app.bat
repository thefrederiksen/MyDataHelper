@echo off
echo ========================================
echo Starting MyDataHelper
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

echo Building application...
cd src\MyDataHelper
dotnet build --configuration Debug >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Build failed. Running with detailed output...
    echo.
    dotnet build --configuration Debug
    pause
    exit /b 1
)

echo Starting MyDataHelper...
echo The application will start in the system tray.
echo Look for the disk icon in your system tray (near the clock).
echo.
echo To access the web interface, open: http://localhost:5250
echo.

REM Run the built executable directly (no console window)
start "" "bin\Debug\net8.0-windows\win-x64\MyDataHelper.exe"

echo.
echo ========================================
echo   MyDataHelper is starting...
echo ========================================
echo Check the system tray for the application icon.