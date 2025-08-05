using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyDataHelper.Models;

namespace MyDataHelper.Services
{
    public interface IFileAnalysisService
    {
        Task<FileAnalysisResult> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<List<FileTypeStatistics>> GetFileTypeStatisticsAsync(int? scanRootId = null, CancellationToken cancellationToken = default);
        Task<List<LargeFileInfo>> GetLargestFilesAsync(int count = 100, int? scanRootId = null, CancellationToken cancellationToken = default);
        Task<long> CalculateFolderSizeAsync(int folderId, CancellationToken cancellationToken = default);
    }
    
    public class FileAnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public long Size { get; set; }
        public string? MimeType { get; set; }
        public bool IsText { get; set; }
        public bool IsBinary { get; set; }
    }
    
    public class FileTypeStatistics
    {
        public string Extension { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public double PercentageOfTotal { get; set; }
        public string ColorCode { get; set; } = string.Empty;
    }
    
    public class LargeFileInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Extension { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }
}