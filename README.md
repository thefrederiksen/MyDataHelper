# MyDataHelper

MyDataHelper is a powerful disk space analyzer for Windows, inspired by WinDirStat. It provides a comprehensive view of your disk usage with an intuitive web-based interface.

## Features

- **Real-time Disk Scanning**: Fast, multi-threaded scanning of drives and folders
- **Interactive Tree View**: Navigate your file system with a hierarchical view showing folder sizes
- **File Type Analysis**: Breakdown of disk usage by file types with color-coded visualization
- **Duplicate File Detection**: Find and manage duplicate files to free up space
- **Largest Files Report**: Quickly identify the files consuming the most space
- **Real-time Monitoring**: Watch for changes in monitored directories
- **Export Capabilities**: Export reports to CSV or JSON formats
- **System Tray Integration**: Runs quietly in the background with easy access

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Blazor Server
- **Database**: SQLite for fast, local storage
- **UI Framework**: Blazor with Bootstrap 5
- **Platform**: Windows Forms launcher with web UI

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- 4GB RAM minimum (8GB recommended)
- 100MB free disk space for application

## Installation

1. Download the latest release from the Releases page
2. Run the installer (MyDataHelper-Setup.exe)
3. Follow the installation wizard
4. Launch MyDataHelper from the Start Menu or Desktop shortcut

## Usage

1. **Adding Scan Roots**: Click on "Disk Scan" and add the drives or folders you want to analyze
2. **Starting a Scan**: Click "Start Scan" to begin analyzing the selected locations
3. **Viewing Results**: 
   - Use the Tree View to navigate folders
   - Check the Dashboard for an overview
   - View Reports for detailed statistics
4. **Finding Duplicates**: Go to the Duplicates page to find and manage duplicate files

## Building from Source

### Prerequisites
- Visual Studio 2022 or later
- .NET 8.0 SDK
- Python 3.12+ (for Python integration)

### Build Steps
```bash
# Clone the repository
git clone https://github.com/yourusername/MyDataHelper.git
cd MyDataHelper

# Restore dependencies
dotnet restore src/MyDataHelper.sln

# Build the project
dotnet build src/MyDataHelper.sln -c Release

# Run the application
dotnet run --project src/MyDataHelper/MyDataHelper.csproj
```

## Configuration

Settings are stored in `%LOCALAPPDATA%\MyDataHelper\settings.json`. Key settings include:

- `ScanThreads`: Number of threads for scanning (default: 4)
- `EnableHashCalculation`: Calculate file hashes for duplicate detection
- `MonitorDirectoryChanges`: Enable real-time directory monitoring
- `ScheduledScanEnabled`: Enable automatic scheduled scans

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Inspired by WinDirStat
- Built with the architecture patterns from MyPhotoHelper
- Uses Bootstrap icons and themes

## Support

For issues, questions, or suggestions, please open an issue on GitHub.
