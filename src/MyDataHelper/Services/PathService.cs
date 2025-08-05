using System;
using System.IO;

namespace MyDataHelper.Services
{
    public class PathService : IPathService
    {
        private readonly string _appDataDirectory;
        
        public PathService()
        {
            _appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyDataHelper");
        }
        
        public string GetAppDataDirectory() => _appDataDirectory;
        
        public string GetDatabasePath() => Path.Combine(_appDataDirectory, "mydatahelper.db");
        
        public string GetLogsDirectory() => Path.Combine(_appDataDirectory, "Logs");
        
        public string GetTempDirectory() => Path.Combine(_appDataDirectory, "Temp");
        
        public string GetSettingsPath() => Path.Combine(_appDataDirectory, "settings.json");
        
        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(_appDataDirectory);
            Directory.CreateDirectory(GetLogsDirectory());
            Directory.CreateDirectory(GetTempDirectory());
            
            // Clean old temp files
            CleanTempDirectory();
        }
        
        public void MigrateDatabaseIfNeeded()
        {
            // Check if database exists in old location (if applicable)
            var oldDbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MyDataHelper",
                "mydatahelper.db");
                
            var newDbPath = GetDatabasePath();
            
            if (File.Exists(oldDbPath) && !File.Exists(newDbPath))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newDbPath)!);
                    File.Move(oldDbPath, newDbPath);
                    Logger.Info($"Migrated database from {oldDbPath} to {newDbPath}");
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Failed to migrate database");
                }
            }
        }
        
        private void CleanTempDirectory()
        {
            try
            {
                var tempDir = GetTempDirectory();
                if (Directory.Exists(tempDir))
                {
                    var di = new DirectoryInfo(tempDir);
                    foreach (var file in di.GetFiles())
                    {
                        if (file.CreationTime < DateTime.Now.AddDays(-7))
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch { /* Ignore file in use */ }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to clean temp directory");
            }
        }
    }
}