@echo off
echo ========================================
echo   Starting MyDataHelper (Development)
echo ========================================
echo.
echo Hot reload is enabled - changes will be reflected automatically
echo.

REM Check if dotnet is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Set development environment
set ASPNETCORE_ENVIRONMENT=Development
set DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true

echo Starting with hot reload...
echo Press Ctrl+C to stop
echo.

REM Run with watch for hot reload
cd src\MyDataHelper
dotnet watch run

cd ..\..
pause