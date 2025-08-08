using System;
using System.Collections.Generic;

namespace MyDataHelper.Models
{
    public enum ScanStatus
    {
        Queued,
        Discovering,
        Scanning,
        Completed,
        Error,
        Cancelled
    }

    public class EnhancedScanProgress
    {
        public int TotalDirectories { get; set; }
        public int CompletedDirectories { get; set; }
        public int QueuedDirectories { get; set; }
        public int ScanningDirectories { get; set; }
        public int ErrorDirectories { get; set; }
        
        public Dictionary<string, DirectoryScanProgress> DirectoryProgress { get; set; } = new();
        
        public long EstimatedFilesTotal { get; set; }
        public long ProcessedFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        
        public DateTime ScanStartTime { get; set; }
        public TimeSpan ElapsedTime => DateTime.UtcNow - ScanStartTime;
        
        public double CompletionPercentage => TotalDirectories > 0 ? (double)CompletedDirectories / TotalDirectories * 100 : 0;
        public double FilesPerSecond => ElapsedTime.TotalSeconds > 0 ? ProcessedFiles / ElapsedTime.TotalSeconds : 0;
        
        public TimeSpan? EstimatedTimeRemaining
        {
            get
            {
                // Better ETA: Based on directory completion rate, not file count
                if (ElapsedTime.TotalSeconds <= 0 || CompletedDirectories == 0) return null;
                
                var directoriesPerSecond = CompletedDirectories / ElapsedTime.TotalSeconds;
                if (directoriesPerSecond <= 0) return null;
                
                var remainingDirectories = TotalDirectories - CompletedDirectories;
                if (remainingDirectories <= 0) return TimeSpan.Zero;
                
                var remainingSeconds = remainingDirectories / directoriesPerSecond;
                return TimeSpan.FromSeconds(remainingSeconds);
            }
        }
        
        public string ScanRootPath { get; set; } = string.Empty;
        public int ScanRootId { get; set; }
        public bool IsComplete => CompletedDirectories >= TotalDirectories;
        public bool HasErrors => ErrorDirectories > 0;
    }

    public class DirectoryScanProgress
    {
        public string DirectoryPath { get; set; } = string.Empty;
        public string DirectoryName => System.IO.Path.GetFileName(DirectoryPath.TrimEnd('\\', '/')) ?? DirectoryPath;
        
        public ScanStatus Status { get; set; } = ScanStatus.Queued;
        public int FilesFound { get; set; }
        public int SubdirectoriesFound { get; set; }
        public long SizeScanned { get; set; }
        
        public DateTime StartTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public TimeSpan? ScanDuration => CompletionTime?.Subtract(StartTime);
        
        public string? ErrorMessage { get; set; }
        public int Depth { get; set; } // For tree hierarchy display
        public bool IsExpanded { get; set; } = true; // For UI tree expansion
        
        public List<DirectoryScanProgress> Subdirectories { get; set; } = new();
        
        // Progress indicators
        public double FilesPerSecond => ScanDuration?.TotalSeconds > 0 ? FilesFound / ScanDuration.Value.TotalSeconds : 0;
        
        // UI helpers
        public string StatusIcon => Status switch
        {
            ScanStatus.Queued => "⏳",
            ScanStatus.Discovering => "🔍",
            ScanStatus.Scanning => "⟲",
            ScanStatus.Completed => "✅",
            ScanStatus.Error => "⚠️",
            ScanStatus.Cancelled => "❌",
            _ => "❓"
        };
        
        public string StatusColor => Status switch
        {
            ScanStatus.Queued => "text-gray-500",
            ScanStatus.Discovering => "text-blue-500",
            ScanStatus.Scanning => "text-blue-600 animate-spin",
            ScanStatus.Completed => "text-green-600",
            ScanStatus.Error => "text-red-600",
            ScanStatus.Cancelled => "text-gray-400",
            _ => "text-gray-500"
        };
    }

    public class ScanPhaseProgress
    {
        public string PhaseName { get; set; } = string.Empty;
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public string CurrentActivity { get; set; } = string.Empty;
        public DateTime PhaseStartTime { get; set; }
        
        public double PhaseCompletion => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;
        public TimeSpan PhaseElapsed => DateTime.UtcNow - PhaseStartTime;
    }

    public class EnhancedScanProgressEventArgs : EventArgs
    {
        public EnhancedScanProgress Progress { get; set; } = new();
        public ScanPhaseProgress? CurrentPhase { get; set; }
        public string? RecentlyCompletedDirectory { get; set; }
        public bool IsSignificantUpdate { get; set; } // For UI update throttling
    }

    public class EnhancedScanCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public int ScanRootId { get; set; }
        public TimeSpan Duration { get; set; }
        public long FilesScanned { get; set; }
        public long FoldersScanned { get; set; }
        public long TotalSize { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCount { get; set; }
        public List<string> ErrorDirectories { get; set; } = new();
        public EnhancedScanProgress FinalProgress { get; set; } = new();
    }
}