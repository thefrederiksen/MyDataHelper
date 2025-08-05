namespace MyDataHelper.Services
{
    public interface IPathService
    {
        string GetAppDataDirectory();
        string GetDatabasePath();
        string GetLogsDirectory();
        string GetTempDirectory();
        string GetSettingsPath();
        void EnsureDirectoriesExist();
        void MigrateDatabaseIfNeeded();
    }
}