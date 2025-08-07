@echo off
echo ========================================
echo Testing MyDataHelper Build
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

echo Detected .NET version:
dotnet --version
echo.

echo Restoring NuGet packages...
dotnet restore src\MyDataHelper\MyDataHelper.csproj
if %errorlevel% neq 0 (
    echo ERROR: Package restore failed
    pause
    exit /b 1
)

echo.
echo Building Debug configuration...
dotnet build src\MyDataHelper\MyDataHelper.csproj -c Debug --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Debug build failed
    pause
    exit /b 1
)

echo.
echo Building Release configuration...
dotnet build src\MyDataHelper\MyDataHelper.csproj -c Release --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Release build failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build test completed successfully!
echo ========================================
echo.
echo Both Debug and Release configurations built without errors.
echo You can now run scripts\start-app.bat to launch the application.
echo.
pause