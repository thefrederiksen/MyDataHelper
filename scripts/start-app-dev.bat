@echo off
echo ========================================
echo Starting MyDataHelper (Development Mode)
echo ========================================
echo.

cd /d "%~dp0\.."

echo Checking .NET SDK...
dotnet --version
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo.
echo Restoring packages...
cd src\MyDataHelper
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Package restore failed
    pause
    exit /b 1
)

echo.
echo Building application (Debug)...
dotnet build --configuration Debug
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo Starting MyDataHelper with verbose logging...
echo.
echo System Tray: Look for the disk icon near the clock
echo Web Interface: http://localhost:5250
echo.
echo Press Ctrl+C to stop the application
echo ========================================
echo.

set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5250
set Logging__LogLevel__Default=Debug
set Logging__LogLevel__Microsoft=Information
set Logging__LogLevel__Microsoft.Hosting.Lifetime=Information

dotnet run --no-build --configuration Debug

pause