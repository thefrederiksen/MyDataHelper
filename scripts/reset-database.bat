@echo off
echo ========================================
echo Reset MyDataHelper Database
echo ========================================
echo.
echo WARNING: This will delete all data in the database!
echo.
set /p confirm="Are you sure you want to reset the database? (y/n): "
if /i not "%confirm%"=="y" (
    echo Operation cancelled.
    pause
    exit /b 0
)

echo.
echo Locating database...
set "dbPath=%LOCALAPPDATA%\MyDataHelper\mydatahelper.db"

if exist "%dbPath%" (
    echo Found database at: %dbPath%
    echo Deleting database files...
    del "%dbPath%" 2>nul
    del "%dbPath%-shm" 2>nul
    del "%dbPath%-wal" 2>nul
    echo Database deleted successfully.
) else (
    echo No database found at: %dbPath%
)

echo.
echo The database will be recreated when you next start MyDataHelper.
echo.
pause