using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public class FileTypeService : IFileTypeService
    {
        private static readonly Dictionary<string, FileTypeInfo> _fileTypes = new()
        {
            // Images
            { ".jpg", new FileTypeInfo { Extension = ".jpg", Category = "Images", Description = "JPEG Image", Icon = "image", MimeType = "image/jpeg" } },
            { ".jpeg", new FileTypeInfo { Extension = ".jpeg", Category = "Images", Description = "JPEG Image", Icon = "image", MimeType = "image/jpeg" } },
            { ".png", new FileTypeInfo { Extension = ".png", Category = "Images", Description = "PNG Image", Icon = "image", MimeType = "image/png" } },
            { ".gif", new FileTypeInfo { Extension = ".gif", Category = "Images", Description = "GIF Image", Icon = "image", MimeType = "image/gif" } },
            { ".bmp", new FileTypeInfo { Extension = ".bmp", Category = "Images", Description = "Bitmap Image", Icon = "image", MimeType = "image/bmp" } },
            { ".svg", new FileTypeInfo { Extension = ".svg", Category = "Images", Description = "SVG Image", Icon = "image", MimeType = "image/svg+xml", IsText = true } },
            { ".webp", new FileTypeInfo { Extension = ".webp", Category = "Images", Description = "WebP Image", Icon = "image", MimeType = "image/webp" } },
            { ".ico", new FileTypeInfo { Extension = ".ico", Category = "Images", Description = "Icon", Icon = "image", MimeType = "image/x-icon" } },
            { ".tiff", new FileTypeInfo { Extension = ".tiff", Category = "Images", Description = "TIFF Image", Icon = "image", MimeType = "image/tiff" } },
            
            // Videos
            { ".mp4", new FileTypeInfo { Extension = ".mp4", Category = "Videos", Description = "MP4 Video", Icon = "video", MimeType = "video/mp4" } },
            { ".avi", new FileTypeInfo { Extension = ".avi", Category = "Videos", Description = "AVI Video", Icon = "video", MimeType = "video/x-msvideo" } },
            { ".mkv", new FileTypeInfo { Extension = ".mkv", Category = "Videos", Description = "Matroska Video", Icon = "video", MimeType = "video/x-matroska" } },
            { ".mov", new FileTypeInfo { Extension = ".mov", Category = "Videos", Description = "QuickTime Video", Icon = "video", MimeType = "video/quicktime" } },
            { ".wmv", new FileTypeInfo { Extension = ".wmv", Category = "Videos", Description = "Windows Media Video", Icon = "video", MimeType = "video/x-ms-wmv" } },
            { ".flv", new FileTypeInfo { Extension = ".flv", Category = "Videos", Description = "Flash Video", Icon = "video", MimeType = "video/x-flv" } },
            { ".webm", new FileTypeInfo { Extension = ".webm", Category = "Videos", Description = "WebM Video", Icon = "video", MimeType = "video/webm" } },
            
            // Audio
            { ".mp3", new FileTypeInfo { Extension = ".mp3", Category = "Audio", Description = "MP3 Audio", Icon = "audio", MimeType = "audio/mpeg" } },
            { ".wav", new FileTypeInfo { Extension = ".wav", Category = "Audio", Description = "WAV Audio", Icon = "audio", MimeType = "audio/wav" } },
            { ".flac", new FileTypeInfo { Extension = ".flac", Category = "Audio", Description = "FLAC Audio", Icon = "audio", MimeType = "audio/flac" } },
            { ".aac", new FileTypeInfo { Extension = ".aac", Category = "Audio", Description = "AAC Audio", Icon = "audio", MimeType = "audio/aac" } },
            { ".ogg", new FileTypeInfo { Extension = ".ogg", Category = "Audio", Description = "OGG Audio", Icon = "audio", MimeType = "audio/ogg" } },
            { ".wma", new FileTypeInfo { Extension = ".wma", Category = "Audio", Description = "Windows Media Audio", Icon = "audio", MimeType = "audio/x-ms-wma" } },
            { ".m4a", new FileTypeInfo { Extension = ".m4a", Category = "Audio", Description = "M4A Audio", Icon = "audio", MimeType = "audio/mp4" } },
            
            // Documents
            { ".pdf", new FileTypeInfo { Extension = ".pdf", Category = "Documents", Description = "PDF Document", Icon = "document", MimeType = "application/pdf" } },
            { ".doc", new FileTypeInfo { Extension = ".doc", Category = "Documents", Description = "Word Document", Icon = "document", MimeType = "application/msword" } },
            { ".docx", new FileTypeInfo { Extension = ".docx", Category = "Documents", Description = "Word Document", Icon = "document", MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
            { ".txt", new FileTypeInfo { Extension = ".txt", Category = "Documents", Description = "Text File", Icon = "document", MimeType = "text/plain", IsText = true } },
            { ".rtf", new FileTypeInfo { Extension = ".rtf", Category = "Documents", Description = "Rich Text Document", Icon = "document", MimeType = "application/rtf" } },
            { ".odt", new FileTypeInfo { Extension = ".odt", Category = "Documents", Description = "OpenDocument Text", Icon = "document", MimeType = "application/vnd.oasis.opendocument.text" } },
            
            // Spreadsheets
            { ".xls", new FileTypeInfo { Extension = ".xls", Category = "Spreadsheets", Description = "Excel Spreadsheet", Icon = "spreadsheet", MimeType = "application/vnd.ms-excel" } },
            { ".xlsx", new FileTypeInfo { Extension = ".xlsx", Category = "Spreadsheets", Description = "Excel Spreadsheet", Icon = "spreadsheet", MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
            { ".csv", new FileTypeInfo { Extension = ".csv", Category = "Spreadsheets", Description = "CSV File", Icon = "spreadsheet", MimeType = "text/csv", IsText = true } },
            { ".ods", new FileTypeInfo { Extension = ".ods", Category = "Spreadsheets", Description = "OpenDocument Spreadsheet", Icon = "spreadsheet", MimeType = "application/vnd.oasis.opendocument.spreadsheet" } },
            
            // Archives
            { ".zip", new FileTypeInfo { Extension = ".zip", Category = "Archives", Description = "ZIP Archive", Icon = "archive", MimeType = "application/zip" } },
            { ".rar", new FileTypeInfo { Extension = ".rar", Category = "Archives", Description = "RAR Archive", Icon = "archive", MimeType = "application/x-rar-compressed" } },
            { ".7z", new FileTypeInfo { Extension = ".7z", Category = "Archives", Description = "7-Zip Archive", Icon = "archive", MimeType = "application/x-7z-compressed" } },
            { ".tar", new FileTypeInfo { Extension = ".tar", Category = "Archives", Description = "TAR Archive", Icon = "archive", MimeType = "application/x-tar" } },
            { ".gz", new FileTypeInfo { Extension = ".gz", Category = "Archives", Description = "GZIP Archive", Icon = "archive", MimeType = "application/gzip" } },
            
            // Code
            { ".cs", new FileTypeInfo { Extension = ".cs", Category = "Code", Description = "C# Source", Icon = "code", MimeType = "text/x-csharp", IsText = true } },
            { ".js", new FileTypeInfo { Extension = ".js", Category = "Code", Description = "JavaScript", Icon = "code", MimeType = "text/javascript", IsText = true } },
            { ".py", new FileTypeInfo { Extension = ".py", Category = "Code", Description = "Python", Icon = "code", MimeType = "text/x-python", IsText = true } },
            { ".java", new FileTypeInfo { Extension = ".java", Category = "Code", Description = "Java Source", Icon = "code", MimeType = "text/x-java", IsText = true } },
            { ".cpp", new FileTypeInfo { Extension = ".cpp", Category = "Code", Description = "C++ Source", Icon = "code", MimeType = "text/x-c++", IsText = true } },
            { ".c", new FileTypeInfo { Extension = ".c", Category = "Code", Description = "C Source", Icon = "code", MimeType = "text/x-c", IsText = true } },
            { ".h", new FileTypeInfo { Extension = ".h", Category = "Code", Description = "Header File", Icon = "code", MimeType = "text/x-c", IsText = true } },
            { ".go", new FileTypeInfo { Extension = ".go", Category = "Code", Description = "Go Source", Icon = "code", MimeType = "text/x-go", IsText = true } },
            { ".rs", new FileTypeInfo { Extension = ".rs", Category = "Code", Description = "Rust Source", Icon = "code", MimeType = "text/x-rust", IsText = true } },
            { ".ts", new FileTypeInfo { Extension = ".ts", Category = "Code", Description = "TypeScript", Icon = "code", MimeType = "text/typescript", IsText = true } },
            
            // Web
            { ".html", new FileTypeInfo { Extension = ".html", Category = "Web", Description = "HTML File", Icon = "web", MimeType = "text/html", IsText = true } },
            { ".htm", new FileTypeInfo { Extension = ".htm", Category = "Web", Description = "HTML File", Icon = "web", MimeType = "text/html", IsText = true } },
            { ".css", new FileTypeInfo { Extension = ".css", Category = "Web", Description = "CSS Stylesheet", Icon = "web", MimeType = "text/css", IsText = true } },
            { ".scss", new FileTypeInfo { Extension = ".scss", Category = "Web", Description = "SCSS Stylesheet", Icon = "web", MimeType = "text/x-scss", IsText = true } },
            { ".xml", new FileTypeInfo { Extension = ".xml", Category = "Web", Description = "XML File", Icon = "web", MimeType = "application/xml", IsText = true } },
            { ".json", new FileTypeInfo { Extension = ".json", Category = "Web", Description = "JSON File", Icon = "web", MimeType = "application/json", IsText = true } },
            { ".yaml", new FileTypeInfo { Extension = ".yaml", Category = "Web", Description = "YAML File", Icon = "web", MimeType = "text/yaml", IsText = true } },
            { ".yml", new FileTypeInfo { Extension = ".yml", Category = "Web", Description = "YAML File", Icon = "web", MimeType = "text/yaml", IsText = true } },
            
            // Executables
            { ".exe", new FileTypeInfo { Extension = ".exe", Category = "Executables", Description = "Windows Executable", Icon = "executable", MimeType = "application/x-msdownload" } },
            { ".dll", new FileTypeInfo { Extension = ".dll", Category = "Executables", Description = "Dynamic Link Library", Icon = "executable", MimeType = "application/x-msdownload" } },
            { ".so", new FileTypeInfo { Extension = ".so", Category = "Executables", Description = "Shared Object", Icon = "executable", MimeType = "application/x-sharedlib" } },
            { ".dylib", new FileTypeInfo { Extension = ".dylib", Category = "Executables", Description = "Dynamic Library", Icon = "executable", MimeType = "application/x-sharedlib" } },
            
            // Databases
            { ".db", new FileTypeInfo { Extension = ".db", Category = "Databases", Description = "Database File", Icon = "database", MimeType = "application/x-sqlite3" } },
            { ".sqlite", new FileTypeInfo { Extension = ".sqlite", Category = "Databases", Description = "SQLite Database", Icon = "database", MimeType = "application/x-sqlite3" } },
            { ".mdb", new FileTypeInfo { Extension = ".mdb", Category = "Databases", Description = "Access Database", Icon = "database", MimeType = "application/x-msaccess" } },
            { ".sql", new FileTypeInfo { Extension = ".sql", Category = "Databases", Description = "SQL Script", Icon = "database", MimeType = "application/sql", IsText = true } }
        };
        
        public string GetFileCategory(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "Other";
                
            extension = extension.ToLowerInvariant();
            if (!extension.StartsWith("."))
                extension = "." + extension;
                
            return _fileTypes.TryGetValue(extension, out var info) ? info.Category : "Other";
        }
        
        public string GetFileIcon(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "file";
                
            extension = extension.ToLowerInvariant();
            if (!extension.StartsWith("."))
                extension = "." + extension;
                
            return _fileTypes.TryGetValue(extension, out var info) ? info.Icon : "file";
        }
        
        public string GetMimeType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";
                
            extension = extension.ToLowerInvariant();
            if (!extension.StartsWith("."))
                extension = "." + extension;
                
            return _fileTypes.TryGetValue(extension, out var info) ? info.MimeType : "application/octet-stream";
        }
        
        public bool IsTextFile(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;
                
            extension = extension.ToLowerInvariant();
            if (!extension.StartsWith("."))
                extension = "." + extension;
                
            return _fileTypes.TryGetValue(extension, out var info) && info.IsText;
        }
        
        public bool IsImageFile(string extension)
        {
            return GetFileCategory(extension) == "Images";
        }
        
        public bool IsVideoFile(string extension)
        {
            return GetFileCategory(extension) == "Videos";
        }
        
        public bool IsAudioFile(string extension)
        {
            return GetFileCategory(extension) == "Audio";
        }
        
        public bool IsArchiveFile(string extension)
        {
            return GetFileCategory(extension) == "Archives";
        }
        
        public bool IsExecutableFile(string extension)
        {
            return GetFileCategory(extension) == "Executables";
        }
        
        public Task<Dictionary<string, FileTypeInfo>> GetAllFileTypesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Dictionary<string, FileTypeInfo>(_fileTypes));
        }
    }
}