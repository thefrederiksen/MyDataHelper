using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface IFileTypeService
    {
        string GetFileCategory(string extension);
        string GetFileIcon(string extension);
        string GetMimeType(string extension);
        bool IsTextFile(string extension);
        bool IsImageFile(string extension);
        bool IsVideoFile(string extension);
        bool IsAudioFile(string extension);
        bool IsArchiveFile(string extension);
        bool IsExecutableFile(string extension);
        Task<Dictionary<string, FileTypeInfo>> GetAllFileTypesAsync(CancellationToken cancellationToken = default);
    }
    
    public class FileTypeInfo
    {
        public string Extension { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public bool IsText { get; set; }
    }
}