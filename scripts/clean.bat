@echo off
echo ========================================
echo Cleaning MyDataHelper Build Artifacts
echo ========================================
echo.

cd /d "%~dp0\.."

echo Cleaning build directories...

if exist "src\MyDataHelper\bin" (
    echo Removing src\MyDataHelper\bin...
    rmdir /s /q "src\MyDataHelper\bin"
)

if exist "src\MyDataHelper\obj" (
    echo Removing src\MyDataHelper\obj...
    rmdir /s /q "src\MyDataHelper\obj"
)

if exist "publish" (
    echo Removing publish...
    rmdir /s /q "publish"
)

echo.
echo Cleaning user-specific files...

if exist ".vs" (
    echo Removing .vs...
    rmdir /s /q ".vs"
)

del /s /q *.user 2>nul

echo.
echo ========================================
echo Clean completed successfully!
echo ========================================
echo.
pause