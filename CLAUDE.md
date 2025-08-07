# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MyDataHelper is a disk space analyzer for Windows, built as a modern alternative to WinDirStat. It uses ASP.NET Core 8.0 with Blazor Server for the web UI, SQLite for data storage, and Windows Forms for the launcher and system tray integration.

## Related Projects

- **C:\ReposFred\MyPhotoHelper**: A similar application that can provide insights and potential learning opportunities for MyDataHelper development
- **C:\ReposFred\MyGithubHelper**: This is a similar application we can learn from

## Build Commands

### Development
```bash
# Start with hot reload (recommended for development)
start-dev.bat

# Or manually with dotnet CLI
cd src\MyDataHelper
dotnet watch run
```

### Building
```bash
# Test build (both Debug and Release)
test-build.bat

# Build release version
build-release.bat

# Clean build artifacts
clean.bat

# Manual build
dotnet build src\MyDataHelper.sln -c Release
```

### Running
```bash
# Start application normally
start-app.bat

# Or run directly
dotnet run --project src\MyDataHelper\MyDataHelper.csproj
```

## Architecture

### Application Structure
- **Launcher**: Windows Forms application (`Program.cs`, `BlazorServerStarter.cs`) that starts the Blazor server and manages system tray
- **Web UI**: Blazor Server application running on http://localhost:5250
- **Database**: SQLite database (`MyDataHelper.db`) with Entity Framework Core
- **Services**: Dependency-injected services following interface-based design

### Key Services
- **DiskScanService**: Multi-threaded disk scanning with progress reporting
- **DuplicateDetectionService**: File duplicate detection using hash calculation
- **TreemapService**: Generates treemap visualizations of disk usage
- **DirectoryMonitoringService**: Real-time file system monitoring using FileSystemWatcher
- **DatabaseInitializationService**: Handles database creation and schema updates
- **SystemTrayService**: Manages system tray icon and context menu

### Service Registration Pattern
All services are registered in `Program.cs:91-148` using dependency injection:
- Singleton services for application-wide state (Settings, Database, SystemTray)
- Scoped services for per-request operations (Scan, Reports, Analysis)
- Background services for long-running operations

### Database Schema
Tables defined in `Data/MyDataHelperDbContext.cs`:
- `tbl_folders`: Directory structure and sizes
- `tbl_files`: Individual file information
- `tbl_scan_roots`: Root directories being monitored
- `tbl_file_types`: File extension statistics
- `tbl_scan_history`: Scan operation history
- `tbl_app_settings`: Application configuration
- `tbl_version`: Database schema version

Database initialization SQL in `Database/DatabaseVersion_001.sql`

### UI Pages
- `Pages/Index.razor`: Dashboard with overview and statistics
- `Pages/DiskScan.razor`: Scan management and tree view
- `Pages/_Host.cshtml`: Blazor Server host page
- `Shared/MainLayout.razor`: Application layout wrapper
- `Shared/NavMenu.razor`: Navigation sidebar

### Configuration
- Settings stored in `%LOCALAPPDATA%\MyDataHelper\settings.json`
- Configuration managed by `SettingsService`
- App settings in `appsettings.json` and `appsettings.Development.json`

## Development Notes

### Port Configuration
Application runs on port 5250 (configured in `Program.cs:40` and `BlazorServerStarter.cs:79`)
- Changed from 5113 to avoid conflict with MyPhotoHelper
- MyGithubHelper uses port 5200

### Single Instance
Application enforces single instance using mutex (GUID: `7A5B9AC4-C8E2-46fd-B9CF-83F05E7CDE9F`)

### Error Handling
- Global exception handlers in `Program.cs:21-23`
- Startup errors logged to `startup_errors.log` via `StartupErrorLogger`
- Error display form: `Forms/StartupErrorForm.cs`

### Python Integration
Python requirements in `Python/requirements.txt` for potential data analysis features

### Auto-updater
Uses AutoUpdater.NET package for automatic updates (configured in `Program.cs`)

## Testing

Currently no test projects are set up. When adding tests:
1. Create test project: `src/MyDataHelper.Tests/MyDataHelper.Tests.csproj`
2. Reference main project
3. Add test frameworks (xUnit, NUnit, or MSTest)
4. Run tests with: `dotnet test src/MyDataHelper.sln`