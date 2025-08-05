using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MyDataHelper.Services
{
    public class BackgroundTaskService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private System.Threading.Timer? _scheduledScanTimer;
        private System.Threading.Timer? _cleanupTimer;
        
        public BackgroundTaskService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Start scheduled scan timer (runs daily at 3 AM)
            var now = DateTime.Now;
            var scheduledTime = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0);
            if (now > scheduledTime)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }
            
            var timeUntilScheduledScan = scheduledTime - now;
            
            _scheduledScanTimer = new System.Threading.Timer(
                async _ => await PerformScheduledScan(),
                null,
                timeUntilScheduledScan,
                TimeSpan.FromDays(1));
                
            // Start cleanup timer (runs every hour)
            _cleanupTimer = new System.Threading.Timer(
                async _ => await PerformCleanup(),
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromHours(1));
                
            // Keep service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        
        private async Task PerformScheduledScan()
        {
            try
            {
                Logger.Info("Starting scheduled scan");
                
                using var scope = _serviceProvider.CreateScope();
                var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
                var scanService = scope.ServiceProvider.GetRequiredService<IDiskScanService>();
                
                // Check if scheduled scanning is enabled
                var isEnabled = await settingsService.GetBoolSettingAsync("ScheduledScanEnabled", false);
                if (!isEnabled)
                {
                    Logger.Info("Scheduled scanning is disabled");
                    return;
                }
                
                // Check if already scanning
                if (scanService.IsScanning)
                {
                    Logger.Warning("Scheduled scan skipped - scan already in progress");
                    return;
                }
                
                // Start full scan
                await scanService.StartFullScanAsync();
                
                Logger.Info("Scheduled scan started successfully");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to perform scheduled scan");
            }
        }
        
        private async Task PerformCleanup()
        {
            try
            {
                Logger.Debug("Performing cleanup tasks");
                
                using var scope = _serviceProvider.CreateScope();
                var pathService = scope.ServiceProvider.GetRequiredService<IPathService>();
                
                // Clean temp directory
                var tempDir = pathService.GetTempDirectory();
                if (Directory.Exists(tempDir))
                {
                    var di = new DirectoryInfo(tempDir);
                    foreach (var file in di.GetFiles())
                    {
                        if (file.CreationTime < DateTime.Now.AddDays(-1))
                        {
                            try
                            {
                                file.Delete();
                                Logger.Debug($"Deleted temp file: {file.Name}");
                            }
                            catch { /* Ignore file in use */ }
                        }
                    }
                }
                
                // Clean old log files
                var logsDir = pathService.GetLogsDirectory();
                if (Directory.Exists(logsDir))
                {
                    var di = new DirectoryInfo(logsDir);
                    foreach (var file in di.GetFiles("*.log"))
                    {
                        if (file.CreationTime < DateTime.Now.AddDays(-30))
                        {
                            try
                            {
                                file.Delete();
                                Logger.Debug($"Deleted old log file: {file.Name}");
                            }
                            catch { /* Ignore file in use */ }
                        }
                    }
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to perform cleanup");
            }
        }
        
        public override void Dispose()
        {
            _scheduledScanTimer?.Dispose();
            _cleanupTimer?.Dispose();
            base.Dispose();
        }
    }
}