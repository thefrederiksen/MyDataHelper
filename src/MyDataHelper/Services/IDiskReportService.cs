using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface IDiskReportService
    {
        Task<DiskUsageReport> GenerateReportAsync(int? scanRootId = null, CancellationToken cancellationToken = default);
        Task<List<FolderSizeInfo>> GetTopFoldersAsync(int count = 20, int? scanRootId = null, CancellationToken cancellationToken = default);
        Task<string> ExportReportToCsvAsync(DiskUsageReport report, string filePath, CancellationToken cancellationToken = default);
        Task<string> ExportReportToJsonAsync(DiskUsageReport report, string filePath, CancellationToken cancellationToken = default);
    }
    
    public class DiskUsageReport
    {
        public DateTime GeneratedAt { get; set; }
        public List<ScanRootSummary> ScanRoots { get; set; } = new List<ScanRootSummary>();
        public long TotalSize { get; set; }
        public long TotalFiles { get; set; }
        public long TotalFolders { get; set; }
        public List<FileTypeStatistics> FileTypeBreakdown { get; set; } = new List<FileTypeStatistics>();
        public List<LargeFileInfo> LargestFiles { get; set; } = new List<LargeFileInfo>();
        public List<FolderSizeInfo> LargestFolders { get; set; } = new List<FolderSizeInfo>();
        public DuplicatesSummary? DuplicatesSummary { get; set; }
    }
    
    public class ScanRootSummary
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long Size { get; set; }
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
        public DateTime? LastScanned { get; set; }
        public double PercentageOfTotal { get; set; }
    }
    
    public class FolderSizeInfo
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public long FolderSize { get; set; }
        public int FileCount { get; set; }
        public int SubfolderCount { get; set; }
        public double PercentageOfParent { get; set; }
        public double PercentageOfTotal { get; set; }
    }
    
    public class DuplicatesSummary
    {
        public int TotalDuplicateGroups { get; set; }
        public long TotalWastedSpace { get; set; }
        public int TotalDuplicateFiles { get; set; }
        public List<DuplicateGroup> TopDuplicateGroups { get; set; } = new List<DuplicateGroup>();
    }
}