@echo off
echo ========================================
echo Building MyDataHelper (Release)
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

echo Cleaning previous builds...
if exist "src\MyDataHelper\bin\Release" rmdir /s /q "src\MyDataHelper\bin\Release"
if exist "src\MyDataHelper\obj\Release" rmdir /s /q "src\MyDataHelper\obj\Release"
if exist "publish" rmdir /s /q "publish"

echo.
echo Restoring NuGet packages...
dotnet restore src\MyDataHelper\MyDataHelper.csproj
if %errorlevel% neq 0 (
    echo ERROR: Package restore failed
    pause
    exit /b 1
)

echo.
echo Building Release configuration...
dotnet build src\MyDataHelper\MyDataHelper.csproj -c Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Publishing self-contained application...
dotnet publish src\MyDataHelper\MyDataHelper.csproj -c Release -r win-x64 --self-contained false -o publish
if %errorlevel% neq 0 (
    echo ERROR: Publish failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Output location: publish\
echo Run MyDataHelper.exe from the publish folder
echo.
pause