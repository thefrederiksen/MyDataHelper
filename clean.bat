@echo off
echo ========================================
echo   Cleaning MyDataHelper Build Artifacts
echo ========================================
echo.

echo Cleaning bin directories...
for /d /r . %%d in (bin) do @if exist "%%d" (
    echo Removing %%d
    rmdir /s /q "%%d"
)

echo.
echo Cleaning obj directories...
for /d /r . %%d in (obj) do @if exist "%%d" (
    echo Removing %%d
    rmdir /s /q "%%d"
)

echo.
echo Cleaning publish directory...
if exist "publish" (
    echo Removing publish directory
    rmdir /s /q "publish"
)

echo.
echo Cleaning .vs directory...
if exist ".vs" (
    echo Removing .vs directory
    rmdir /s /q ".vs"
)

echo.
echo ========================================
echo   Clean completed!
echo ========================================
echo.
pause