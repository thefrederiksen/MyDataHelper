using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MyDataHelper.Services
{
    public class DirectoryMonitoringService : BackgroundService, IDirectoryMonitoringService
    {
        private readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers;
        private readonly IServiceProvider _serviceProvider;
        
        public event EventHandler<DirectoryChangeEventArgs>? DirectoryChanged;
        
        public DirectoryMonitoringService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Service keeps running in background
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        
        public Task StartMonitoringAsync(string path)
        {
            if (_watchers.ContainsKey(path))
            {
                Logger.Warning($"Already monitoring path: {path}");
                return Task.CompletedTask;
            }
            
            try
            {
                var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | 
                                   NotifyFilters.DirectoryName | 
                                   NotifyFilters.Size | 
                                   NotifyFilters.LastWrite
                };
                
                watcher.Created += OnCreated;
                watcher.Changed += OnChanged;
                watcher.Deleted += OnDeleted;
                watcher.Renamed += OnRenamed;
                watcher.Error += OnError;
                
                watcher.EnableRaisingEvents = true;
                
                if (_watchers.TryAdd(path, watcher))
                {
                    Logger.Info($"Started monitoring path: {path}");
                }
                else
                {
                    watcher.Dispose();
                    Logger.Warning($"Failed to add watcher for path: {path}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to start monitoring path: {path}");
            }
            
            return Task.CompletedTask;
        }
        
        public Task StopMonitoringAsync(string path)
        {
            if (_watchers.TryRemove(path, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                Logger.Info($"Stopped monitoring path: {path}");
            }
            
            return Task.CompletedTask;
        }
        
        public Task StopAllMonitoringAsync()
        {
            foreach (var kvp in _watchers)
            {
                kvp.Value.EnableRaisingEvents = false;
                kvp.Value.Dispose();
            }
            
            _watchers.Clear();
            Logger.Info("Stopped all directory monitoring");
            
            return Task.CompletedTask;
        }
        
        public bool IsMonitoring(string path)
        {
            return _watchers.ContainsKey(path);
        }
        
        public string[] GetMonitoredPaths()
        {
            return _watchers.Keys.ToArray();
        }
        
        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            RaiseDirectoryChanged(e.FullPath, DirectoryChangeType.Created);
        }
        
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            RaiseDirectoryChanged(e.FullPath, DirectoryChangeType.Changed);
        }
        
        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            RaiseDirectoryChanged(e.FullPath, DirectoryChangeType.Deleted);
        }
        
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            DirectoryChanged?.Invoke(this, new DirectoryChangeEventArgs
            {
                Path = e.Name ?? string.Empty,
                FullPath = e.FullPath,
                OldPath = e.OldFullPath,
                ChangeType = DirectoryChangeType.Renamed,
                Timestamp = DateTime.UtcNow
            });
        }
        
        private void OnError(object sender, ErrorEventArgs e)
        {
            Logger.LogException(e.GetException(), "FileSystemWatcher error");
            
            // Try to recover by recreating the watcher
            if (sender is FileSystemWatcher watcher)
            {
                var path = watcher.Path;
                _ = Task.Run(async () =>
                {
                    await StopMonitoringAsync(path);
                    await Task.Delay(5000); // Wait before retrying
                    await StartMonitoringAsync(path);
                });
            }
        }
        
        private void RaiseDirectoryChanged(string fullPath, DirectoryChangeType changeType)
        {
            var path = Path.GetFileName(fullPath) ?? string.Empty;
            
            DirectoryChanged?.Invoke(this, new DirectoryChangeEventArgs
            {
                Path = path,
                FullPath = fullPath,
                ChangeType = changeType,
                Timestamp = DateTime.UtcNow
            });
        }
        
        public override void Dispose()
        {
            StopAllMonitoringAsync().Wait();
            base.Dispose();
        }
    }
}