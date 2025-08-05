using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyDataHelper.Data;

namespace MyDataHelper.Services
{
    public class DatabaseInitializationService : IDatabaseInitializationService
    {
        private readonly IServiceProvider _serviceProvider;
        
        public DatabaseInitializationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public async Task<bool> InitializeDatabaseAsync(string connectionString)
        {
            try
            {
                // Create database directory if needed
                var builder = new SqliteConnectionStringBuilder(connectionString);
                var dbPath = builder.DataSource;
                var dbDirectory = Path.GetDirectoryName(dbPath);
                
                if (!string.IsNullOrEmpty(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                }
                
                // Check if database exists
                bool isNewDatabase = !File.Exists(dbPath);
                
                // Create or migrate database
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                
                await dbContext.Database.EnsureCreatedAsync();
                
                // Apply any pending migrations
                await dbContext.Database.MigrateAsync();
                
                // Initialize version if new database
                if (isNewDatabase)
                {
                    dbContext.tbl_version.Add(new Models.tbl_version
                    {
                        version = 1,
                        applied_date = DateTime.UtcNow
                    });
                    
                    // Add default settings
                    dbContext.tbl_app_settings.Add(new Models.tbl_app_settings
                    {
                        setting_key = "ScanOnStartup",
                        setting_value = "false",
                        data_type = "bool",
                        description = "Automatically start scanning when the application starts",
                        last_modified = DateTime.UtcNow
                    });
                    
                    dbContext.tbl_app_settings.Add(new Models.tbl_app_settings
                    {
                        setting_key = "MonitorChanges",
                        setting_value = "true",
                        data_type = "bool",
                        description = "Monitor directories for changes in real-time",
                        last_modified = DateTime.UtcNow
                    });
                    
                    dbContext.tbl_app_settings.Add(new Models.tbl_app_settings
                    {
                        setting_key = "CalculateHashes",
                        setting_value = "false",
                        data_type = "bool",
                        description = "Calculate file hashes during scan (slower but enables duplicate detection)",
                        last_modified = DateTime.UtcNow
                    });
                    
                    dbContext.tbl_app_settings.Add(new Models.tbl_app_settings
                    {
                        setting_key = "MaxScanThreads",
                        setting_value = "4",
                        data_type = "int",
                        description = "Maximum number of threads to use for scanning",
                        last_modified = DateTime.UtcNow
                    });
                    
                    await dbContext.SaveChangesAsync();
                }
                
                Logger.Info($"Database initialized successfully at {dbPath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to initialize database");
                return false;
            }
        }
        
        public async Task<bool> IsDatabaseInitializedAsync(string connectionString)
        {
            try
            {
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='tbl_version'";
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<int> GetDatabaseVersionAsync(string connectionString)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                
                var version = await dbContext.tbl_version
                    .OrderByDescending(v => v.version)
                    .FirstOrDefaultAsync();
                    
                return version?.version ?? 0;
            }
            catch
            {
                return 0;
            }
        }
        
        public async Task<bool> UpgradeDatabaseAsync(string connectionString)
        {
            try
            {
                var currentVersion = await GetDatabaseVersionAsync(connectionString);
                
                // Add upgrade logic here when needed
                // For now, just ensure database is up to date
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                await dbContext.Database.MigrateAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to upgrade database");
                return false;
            }
        }
    }
}