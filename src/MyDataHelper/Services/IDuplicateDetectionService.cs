using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface IDuplicateDetectionService
    {
        Task<List<DuplicateGroup>> FindDuplicatesAsync(int? scanRootId = null, DuplicateSearchOptions? options = null, CancellationToken cancellationToken = default);
        Task<string> CalculateFileHashAsync(string filePath, CancellationToken cancellationToken = default);
        Task UpdateFileHashesAsync(int scanRootId, IProgress<HashingProgress>? progress = null, CancellationToken cancellationToken = default);
    }
    
    public class DuplicateGroup
    {
        public string Hash { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int FileCount { get; set; }
        public long TotalWastedSpace { get; set; } // (FileCount - 1) * FileSize
        public List<DuplicateFile> Files { get; set; } = new List<DuplicateFile>();
    }
    
    public class DuplicateFile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime Created { get; set; }
    }
    
    public class DuplicateSearchOptions
    {
        public long? MinFileSize { get; set; }
        public long? MaxFileSize { get; set; }
        public List<string>? IncludeExtensions { get; set; }
        public List<string>? ExcludeExtensions { get; set; }
        public bool SkipEmptyFiles { get; set; } = true;
    }
    
    public class HashingProgress
    {
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
        public long BytesProcessed { get; set; }
        public long TotalBytes { get; set; }
    }
}