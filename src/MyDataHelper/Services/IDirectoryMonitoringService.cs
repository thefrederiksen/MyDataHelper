using System;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface IDirectoryMonitoringService
    {
        event EventHandler<DirectoryChangeEventArgs>? DirectoryChanged;
        
        Task StartMonitoringAsync(string path);
        Task StopMonitoringAsync(string path);
        Task StopAllMonitoringAsync();
        bool IsMonitoring(string path);
        string[] GetMonitoredPaths();
    }
    
    public class DirectoryChangeEventArgs : EventArgs
    {
        public string Path { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DirectoryChangeType ChangeType { get; set; }
        public string? OldPath { get; set; } // For rename operations
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    public enum DirectoryChangeType
    {
        Created,
        Changed,
        Deleted,
        Renamed
    }
}