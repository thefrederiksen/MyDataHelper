using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyDataHelper.Forms;
using MyDataHelper.Services;
using MyDataHelper.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
// Python integration removed for now

namespace MyDataHelper
{
    public class BlazorServerStarter
    {
        private StartupForm? startupForm;
        private WebApplication? app;
        
        public void Start(string[] args)
        {
            // Check if app should start minimized
            var startMinimized = args.Contains("--minimized");
            
            // Show startup form
            startupForm = new StartupForm();
            if (!startMinimized)
            {
                startupForm.Show();
                Application.DoEvents(); // Process Windows messages
            }
            
            // Start initialization on background thread
            var initTask = Task.Run(async () =>
            {
                try
                {
                    await InitializeAndStartBlazorServer(args, startMinimized);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Failed to start Blazor server");
                    startupForm?.Invoke(new Action(() =>
                    {
                        ShowError("Startup Failed", ex.Message);
                        Application.Exit();
                    }));
                }
            });
            
            // Run Windows Forms message loop - this keeps the app running
            Application.Run();
        }
        
        private async Task InitializeAndStartBlazorServer(string[] args, bool startMinimized)
        {
            try
            {
                UpdateStatus("Initializing directories...", 10);
                StartupErrorLogger.LogError("Initializing directories", null);
                
                // Initialize PathService
                var pathService = new PathService();
                Logger.Initialize(pathService.GetLogsDirectory(), MyDataHelper.Services.LogLevel.Info);
                Logger.Info("MyDataHelper starting (WinForms launcher)");
                
                pathService.EnsureDirectoriesExist();
                pathService.MigrateDatabaseIfNeeded();
                
                UpdateStatus("Creating web application...", 20);
                StartupErrorLogger.LogError("Creating web application", null);
                
                // Create the Blazor web application
                var builder = WebApplication.CreateBuilder(args);
                
                // Configure Kestrel
                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ListenLocalhost(5250, listenOptions =>
                    {
                        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                    });
                });
                
                UpdateStatus("Configuring services...", 30);
                StartupErrorLogger.LogError("Configuring services", null);
                
                // Configure all the services
                ConfigureServices(builder, pathService);
                
                UpdateStatus("Building application...", 40);
                StartupErrorLogger.LogError("Building application", null);
                
                // Build the app
                app = builder.Build();
                
                // Configure the HTTP pipeline
                ConfigureApp(app);
                
                // Python initialization removed
                
                UpdateStatus("Initializing database...", 70);
                StartupErrorLogger.LogError("Initializing database", null);
            
            // Initialize database
            await InitializeDatabase(app, pathService);
            
            UpdateStatus("Starting system tray...", 85);
            
            // Initialize system tray on UI thread
            if (startupForm?.InvokeRequired == true)
            {
                startupForm.Invoke(new Action(() =>
                {
                    var systemTrayService = app.Services.GetRequiredService<SystemTrayService>();
                    systemTrayService.Initialize();
                }));
            }
            else
            {
                var systemTrayService = app.Services.GetRequiredService<SystemTrayService>();
                systemTrayService.Initialize();
            }
            
            UpdateStatus("Starting web server...", 90);
            
            // Start the Blazor server in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await app.RunAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Blazor server stopped");
                }
            });
            
            // Wait for server to be ready
            await Task.Delay(2000);
            
            UpdateStatus("Ready!", 100);
            await Task.Delay(500);
            
            // Close startup form and open browser if needed
            if (startupForm?.InvokeRequired == true)
            {
                startupForm.Invoke(new Action(() =>
                {
                    startupForm.Close();
                    startupForm = null;
                    
                    if (!startMinimized)
                    {
                        OpenBrowser();
                    }
                }));
            }
            else
            {
                startupForm?.Close();
                startupForm = null;
                
                if (!startMinimized)
                {
                    OpenBrowser();
                }
            }
            }
            catch (Exception ex)
            {
                StartupErrorLogger.LogError("Fatal error during Blazor server initialization", ex);
                
                // Hide startup form
                startupForm?.Invoke(new Action(() => startupForm.Hide()));
                
                // Show error dialog
                if (startupForm?.InvokeRequired == true)
                {
                    startupForm.Invoke(new Action(() =>
                    {
                        using (var errorForm = new StartupErrorForm("Failed to start MyDataHelper server", ex))
                        {
                            errorForm.ShowDialog();
                        }
                        Application.Exit();
                    }));
                }
                else
                {
                    using (var errorForm = new StartupErrorForm("Failed to start MyDataHelper server", ex))
                    {
                        errorForm.ShowDialog();
                    }
                    Application.Exit();
                }
            }
        }
        
        private void ConfigureServices(WebApplicationBuilder builder, IPathService pathService)
        {
            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddDebug();
            builder.Logging.AddEventLog();
            builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            
            // Python integration removed for now
            
            // Configure Entity Framework
            var dbPath = pathService.GetDatabasePath();
            var connectionString = $"Data Source={dbPath};Cache=Shared;";
            
            // Add services
            builder.Services.AddMemoryCache();
            
            // Use DbContextFactory for both singleton and scoped access
            builder.Services.AddDbContextFactory<MyDataHelperDbContext>(options =>
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(30);
                })
                .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
                .EnableDetailedErrors(builder.Environment.IsDevelopment()));
            
            // Register a scoped DbContext that uses the factory
            builder.Services.AddScoped<MyDataHelperDbContext>(provider =>
            {
                var factory = provider.GetRequiredService<IDbContextFactory<MyDataHelperDbContext>>();
                return factory.CreateDbContext();
            });
            
            // Core services
            builder.Services.AddSingleton<IPathService>(pathService);
            builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
            builder.Services.AddSingleton<IDatabaseChangeNotificationService, DatabaseChangeNotificationService>();
            builder.Services.AddSingleton<SystemTrayService>();
            builder.Services.AddHostedService<BackgroundTaskService>();
            builder.Services.AddScoped<ISettingsService, SettingsService>();
            
            // Disk analysis services
            builder.Services.AddSingleton<IDriveDetectionService, DriveDetectionService>();
            builder.Services.AddScoped<IDiskScanService, DiskScanService>();
            builder.Services.AddSingleton<IScanStatusService, ScanStatusService>();
            builder.Services.AddScoped<IFileAnalysisService, FileAnalysisService>();
            builder.Services.AddScoped<IFileTypeService, FileTypeService>();
            builder.Services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
            builder.Services.AddScoped<IHashCalculationService, HashCalculationService>();
            builder.Services.AddScoped<IFolderDialogService, FolderDialogService>();
            builder.Services.AddScoped<IDiskReportService, DiskReportService>();
            builder.Services.AddScoped<IFileIconService, FileIconService>();
            builder.Services.AddScoped<ITreemapService, TreemapService>();
            
            // Directory monitoring service
            builder.Services.AddSingleton<DirectoryMonitoringService>();
            builder.Services.AddSingleton<IDirectoryMonitoringService>(provider => provider.GetRequiredService<DirectoryMonitoringService>());
            builder.Services.AddHostedService(provider => provider.GetRequiredService<DirectoryMonitoringService>());
            
            // Add Blazor services
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddControllers();
        }
        
        private void ConfigureApp(WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
            
            app.UseStaticFiles();
            app.UseRouting();
            app.MapControllers();
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");
        }
        
        // Python initialization method removed
        
        private async Task InitializeDatabase(WebApplication app, IPathService pathService)
        {
            try
            {
                var dbPath = pathService.GetDatabasePath();
                var connectionString = $"Data Source={dbPath};Cache=Shared;";
                
                StartupErrorLogger.LogError($"Database path: {dbPath}", null);
                
                using var scope = app.Services.CreateScope();
                var dbInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                
                var success = await dbInitService.InitializeDatabaseAsync(connectionString);
                if (!success)
                {
                    throw new Exception("Database initialization failed");
                }
                
                await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                await dbContext.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
                await dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
                
                Logger.Info("Database initialized successfully");
                StartupErrorLogger.LogError("Database initialized successfully", null);
            }
            catch (Exception ex)
            {
                StartupErrorLogger.LogError("Database initialization error", ex);
                throw; // Re-throw to be caught by the main error handler
            }
        }
        
        private void UpdateStatus(string message, int progress)
        {
            startupForm?.UpdateStatus(message, progress);
            Logger.Info($"Startup: {message} ({progress}%)");
        }
        
        private void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        
        private void OpenBrowser()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "http://localhost:5250",
                    UseShellExecute = true
                });
                Logger.Info("Opened browser to Blazor application");
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to open browser: {ex.Message}");
            }
        }
    }
}