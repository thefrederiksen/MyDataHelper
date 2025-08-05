@echo off
echo ========================================
echo   Starting MyDataHelper
echo ========================================
echo.

REM Check if dotnet is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Building and starting the application...
echo This may take a moment on first run.
echo.

REM Run the application in a new window
start "MyDataHelper" dotnet run --project src\MyDataHelper

echo.
echo ========================================
echo   MyDataHelper is starting...
echo ========================================
echo Check the new window for application output.
echo The web interface will open at http://localhost:5113
echo.
echo Press any key to close this window...
pause >nul