# MyDataHelper Scripts

This directory contains utility scripts for building, running, and managing MyDataHelper.

## Quick Start Scripts

### For Users
- **`start-app.bat`** - Start MyDataHelper normally
- **`first-time-setup.bat`** - Run this once after cloning the repository

### For Developers
- **`start-app-dev.bat`** - Start with hot reload and verbose logging
- **`test-build.bat`** - Test both Debug and Release builds

## All Scripts

| Script | Purpose |
|--------|---------|
| `first-time-setup.bat` | Initial setup after cloning - restores packages and builds |
| `start-app.bat` | Start the application in normal mode |
| `start-app-dev.bat` | Start in development mode with hot reload |
| `start-app-release.bat` | Start the optimized release build |
| `build-release.bat` | Build and publish a release version |
| `test-build.bat` | Test both Debug and Release configurations |
| `clean.bat` | Remove all build artifacts and temporary files |
| `reset-database.bat` | Delete the database (will be recreated on next run) |

## Application Details

- **Web Interface**: http://localhost:5250
- **System Tray**: Look for the disk icon near the clock
- **Database Location**: `%LOCALAPPDATA%\MyDataHelper\mydatahelper.db`
- **Log Files**: `%LOCALAPPDATA%\MyDataHelper\logs\`

## Requirements

- .NET 8.0 SDK or later
- Windows 10 or later
- 4GB RAM minimum

## Troubleshooting

If the application fails to start:
1. Check `%LOCALAPPDATA%\MyDataHelper\startup_errors.log`
2. Run `scripts\clean.bat` and try again
3. Run `scripts\reset-database.bat` if database issues occur
4. Ensure no other application is using port 5250