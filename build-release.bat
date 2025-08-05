@echo off
echo ========================================
echo   Building MyDataHelper Release
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

REM Clean previous builds
echo Cleaning previous builds...
if exist "src\MyDataHelper\bin\Release" rmdir /s /q "src\MyDataHelper\bin\Release"
if exist "src\MyDataHelper\obj\Release" rmdir /s /q "src\MyDataHelper\obj\Release"

REM Restore packages
echo.
echo Restoring NuGet packages...
dotnet restore src\MyDataHelper.sln

REM Build Release
echo.
echo Building Release configuration...
dotnet build src\MyDataHelper.sln -c Release

if errorlevel 1 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

REM Publish self-contained
echo.
echo Publishing self-contained application...
dotnet publish src\MyDataHelper\MyDataHelper.csproj -c Release -r win-x64 --self-contained false -o publish

if errorlevel 1 (
    echo.
    echo ERROR: Publish failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Build completed successfully!
echo ========================================
echo.
echo Output location: publish\
echo Run MyDataHelper.exe from the publish folder
echo.
pause