@echo off
echo ========================================
echo MyDataHelper - First Time Setup
echo ========================================
echo.
echo This script will help you set up MyDataHelper for the first time.
echo.

cd /d "%~dp0\.."

echo Step 1: Checking .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo.
    echo Please install .NET 8.0 SDK from:
    echo https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)
echo .NET SDK found: 
dotnet --version
echo.

echo Step 2: Restoring NuGet packages...
cd src\MyDataHelper
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Package restore failed
    pause
    exit /b 1
)
echo Packages restored successfully.
echo.

echo Step 3: Building application...
dotnet build --configuration Debug
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)
echo Build completed successfully.
echo.

echo Step 4: Creating application directories...
if not exist "%LOCALAPPDATA%\MyDataHelper" (
    mkdir "%LOCALAPPDATA%\MyDataHelper"
    echo Created: %LOCALAPPDATA%\MyDataHelper
)
if not exist "%LOCALAPPDATA%\MyDataHelper\logs" (
    mkdir "%LOCALAPPDATA%\MyDataHelper\logs"
    echo Created: %LOCALAPPDATA%\MyDataHelper\logs
)
echo.

echo ========================================
echo Setup completed successfully!
echo ========================================
echo.
echo You can now start MyDataHelper by running:
echo   scripts\start-app.bat
echo.
echo The application will:
echo - Run in your system tray (look for the disk icon)
echo - Open a web interface at http://localhost:5250
echo - Create a database on first run
echo.
pause