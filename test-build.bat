@echo off
echo ========================================
echo   Testing MyDataHelper Build
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

echo Detected .NET version:
dotnet --version
echo.

REM Restore packages
echo Restoring NuGet packages...
dotnet restore src\MyDataHelper.sln
if errorlevel 1 (
    echo ERROR: Package restore failed!
    pause
    exit /b 1
)

REM Build Debug
echo.
echo Building Debug configuration...
dotnet build src\MyDataHelper\MyDataHelper.csproj -c Debug --no-restore
if errorlevel 1 (
    echo ERROR: Debug build failed!
    pause
    exit /b 1
)

REM Build Release
echo.
echo Building Release configuration...
dotnet build src\MyDataHelper\MyDataHelper.csproj -c Release --no-restore
if errorlevel 1 (
    echo ERROR: Release build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Build test completed successfully!
echo ========================================
echo.
echo Both Debug and Release configurations built without errors.
echo You can now run start-app.bat to launch the application.
echo.
pause