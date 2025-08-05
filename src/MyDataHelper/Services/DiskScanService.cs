using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyDataHelper.Data;
using MyDataHelper.Models;

namespace MyDataHelper.Services
{
    public class DiskScanService : IDiskScanService
    {
        private readonly ILogger<DiskScanService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPathService _pathService;
        private readonly IScanStatusService _scanStatusService;
        
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _scanTask;
        private ScanProgress? _currentProgress;
        
        public event EventHandler<ScanProgressEventArgs>? ScanProgressChanged;
        public event EventHandler<ScanCompletedEventArgs>? ScanCompleted;
        
        public bool IsScanning => _scanTask != null && !_scanTask.IsCompleted;
        public ScanProgress? CurrentProgress => _currentProgress;
        
        public DiskScanService(
            ILogger<DiskScanService> logger,
            IServiceProvider serviceProvider,
            IPathService pathService,
            IScanStatusService scanStatusService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _pathService = pathService;
            _scanStatusService = scanStatusService;
        }
        
        public async Task StartScanAsync(int scanRootId, ScanType scanType = ScanType.Full, CancellationToken cancellationToken = default)
        {
            if (IsScanning)
            {
                _logger.LogWarning("Scan already in progress");
                return;
            }
            
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _scanTask = Task.Run(() => PerformScanAsync(scanRootId, scanType, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
            
            await Task.CompletedTask;
        }
        
        public async Task StartFullScanAsync(CancellationToken cancellationToken = default)
        {
            if (IsScanning)
            {
                _logger.LogWarning("Scan already in progress");
                return;
            }
            
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _scanTask = Task.Run(() => PerformFullScanAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            
            await Task.CompletedTask;
        }
        
        public void CancelScan()
        {
            _cancellationTokenSource?.Cancel();
        }
        
        private async Task PerformFullScanAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                
                var scanRoots = await dbContext.tbl_scan_roots
                    .Where(sr => sr.is_active)
                    .ToListAsync(cancellationToken);
                
                foreach (var scanRoot in scanRoots)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    await PerformScanAsync(scanRoot.id, ScanType.Full, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during full scan");
            }
        }
        
        private async Task PerformScanAsync(int scanRootId, ScanType scanType, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var scanHistory = new tbl_scan_history
            {
                scan_root_id = scanRootId,
                start_time = startTime,
                status = "Running",
                scan_type = scanType.ToString()
            };
            
            try
            {
                _logger.LogInformation($"Starting {scanType} scan for root ID {scanRootId}");
                
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                
                // Get scan root
                var scanRoot = await dbContext.tbl_scan_roots
                    .FirstOrDefaultAsync(sr => sr.id == scanRootId, cancellationToken);
                    
                if (scanRoot == null)
                {
                    _logger.LogError($"Scan root with ID {scanRootId} not found");
                    return;
                }
                
                // Add scan history entry
                dbContext.tbl_scan_history.Add(scanHistory);
                await dbContext.SaveChangesAsync(cancellationToken);
                
                // Update scan status
                _scanStatusService.UpdateStatus($"Scanning {scanRoot.display_name}", 0);
                
                // Initialize progress
                _currentProgress = new ScanProgress
                {
                    CurrentPath = scanRoot.path,
                    StartTime = startTime,
                    ScanRootId = scanRootId
                };
                
                // Clear existing data if full scan
                if (scanType == ScanType.Full)
                {
                    await ClearExistingData(dbContext, scanRootId, cancellationToken);
                }
                
                // Get drive info
                UpdateDriveInfo(scanRoot);
                
                // Start scanning
                var rootFolder = await ScanFolder(
                    dbContext, 
                    scanRoot, 
                    scanRoot.path, 
                    null, 
                    0, 
                    scanHistory,
                    cancellationToken);
                
                // Update scan root statistics
                if (rootFolder != null)
                {
                    scanRoot.last_scan_time = DateTime.UtcNow;
                    scanRoot.last_scan_size = rootFolder.total_size;
                    scanRoot.last_scan_file_count = rootFolder.total_file_count;
                    scanRoot.last_scan_folder_count = await dbContext.tbl_folders
                        .Where(f => f.scan_root_id == scanRootId)
                        .CountAsync(cancellationToken);
                    scanRoot.last_scan_duration = DateTime.UtcNow - startTime;
                }
                
                // Update file type statistics
                await UpdateFileTypeStatistics(dbContext, cancellationToken);
                
                // Complete scan history
                scanHistory.end_time = DateTime.UtcNow;
                scanHistory.status = "Completed";
                scanHistory.duration = scanHistory.end_time.Value - scanHistory.start_time;
                scanHistory.total_size_scanned = scanRoot.last_scan_size ?? 0;
                scanHistory.files_scanned = scanRoot.last_scan_file_count ?? 0;
                scanHistory.folders_scanned = scanRoot.last_scan_folder_count ?? 0;
                
                if (scanHistory.duration.Value.TotalSeconds > 0)
                {
                    scanHistory.scan_speed_mbps = (scanHistory.total_size_scanned / 1024.0 / 1024.0) / scanHistory.duration.Value.TotalSeconds;
                }
                
                await dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation($"Scan completed for {scanRoot.display_name}");
                _scanStatusService.UpdateStatus("Scan completed", 100);
                
                ScanCompleted?.Invoke(this, new ScanCompletedEventArgs
                {
                    Success = true,
                    ScanRootId = scanRootId,
                    Duration = scanHistory.duration.Value,
                    FilesScanned = scanHistory.files_scanned,
                    FoldersScanned = scanHistory.folders_scanned,
                    TotalSize = scanHistory.total_size_scanned
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scan");
                
                scanHistory.end_time = DateTime.UtcNow;
                scanHistory.status = "Failed";
                scanHistory.error_message = ex.Message;
                scanHistory.duration = scanHistory.end_time.Value - scanHistory.start_time;
                
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                dbContext.Update(scanHistory);
                await dbContext.SaveChangesAsync();
                
                _scanStatusService.UpdateStatus("Scan failed", 0);
                
                ScanCompleted?.Invoke(this, new ScanCompletedEventArgs
                {
                    Success = false,
                    ScanRootId = scanRootId,
                    ErrorMessage = ex.Message
                });
            }
        }
        
        private async Task<tbl_folders?> ScanFolder(
            MyDataHelperDbContext dbContext,
            tbl_scan_roots scanRoot,
            string folderPath,
            int? parentFolderId,
            int depth,
            tbl_scan_history scanHistory,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;
                
            try
            {
                var dirInfo = new DirectoryInfo(folderPath);
                if (!dirInfo.Exists)
                    return null;
                
                // Update progress
                _currentProgress!.CurrentPath = folderPath;
                _currentProgress.ProcessedFolders++;
                
                _scanStatusService.UpdateStatus($"Scanning: {Path.GetFileName(folderPath)}", 
                    _currentProgress.ProcessedFolders % 100); // Simple progress cycling
                
                ScanProgressChanged?.Invoke(this, new ScanProgressEventArgs
                {
                    CurrentPath = folderPath,
                    ProcessedFiles = _currentProgress.ProcessedFiles,
                    ProcessedFolders = _currentProgress.ProcessedFolders,
                    CurrentSize = _currentProgress.CurrentSize
                });
                
                // Create or update folder entry
                var folder = await dbContext.tbl_folders
                    .FirstOrDefaultAsync(f => f.path == folderPath && f.scan_root_id == scanRoot.id, cancellationToken);
                    
                if (folder == null)
                {
                    folder = new tbl_folders
                    {
                        scan_root_id = scanRoot.id,
                        parent_folder_id = parentFolderId,
                        path = folderPath,
                        name = dirInfo.Name,
                        depth = depth
                    };
                    dbContext.tbl_folders.Add(folder);
                    scanHistory.new_folders++;
                }
                
                folder.last_modified = dirInfo.LastWriteTimeUtc;
                folder.last_scanned = DateTime.UtcNow;
                folder.is_accessible = true;
                
                await dbContext.SaveChangesAsync(cancellationToken);
                
                long folderSize = 0;
                int fileCount = 0;
                
                // Scan files in this folder
                try
                {
                    var files = dirInfo.GetFiles();
                    foreach (var fileInfo in files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                            
                        await ScanFile(dbContext, folder.id, fileInfo, scanHistory, cancellationToken);
                        folderSize += fileInfo.Length;
                        fileCount++;
                        _currentProgress.ProcessedFiles++;
                        _currentProgress.CurrentSize += fileInfo.Length;
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning($"Access denied to files in {folderPath}: {ex.Message}");
                    folder.is_accessible = false;
                    folder.error_message = "Access denied to files";
                    scanHistory.errors++;
                }
                
                folder.folder_size = folderSize;
                folder.file_count = fileCount;
                folder.total_size = folderSize; // Will be updated with subfolder sizes
                folder.total_file_count = fileCount;
                
                // Scan subfolders
                var subfolders = new List<tbl_folders>();
                if (scanRoot.include_subdirectories)
                {
                    try
                    {
                        var dirs = dirInfo.GetDirectories();
                        foreach (var subDir in dirs)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;
                                
                            // Skip symbolic links if configured
                            if (!scanRoot.follow_symlinks && (subDir.Attributes & FileAttributes.ReparsePoint) != 0)
                                continue;
                                
                            var subFolder = await ScanFolder(
                                dbContext, 
                                scanRoot, 
                                subDir.FullName, 
                                folder.id, 
                                depth + 1,
                                scanHistory,
                                cancellationToken);
                                
                            if (subFolder != null)
                            {
                                subfolders.Add(subFolder);
                                folder.total_size += subFolder.total_size;
                                folder.total_file_count += subFolder.total_file_count;
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogWarning($"Access denied to subdirectories in {folderPath}: {ex.Message}");
                        folder.error_message = "Access denied to subdirectories";
                        scanHistory.errors++;
                    }
                }
                
                folder.subfolder_count = subfolders.Count;
                scanHistory.folders_scanned++;
                
                await dbContext.SaveChangesAsync(cancellationToken);
                
                return folder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scanning folder {folderPath}");
                scanHistory.errors++;
                return null;
            }
        }
        
        private async Task ScanFile(
            MyDataHelperDbContext dbContext,
            int folderId,
            FileInfo fileInfo,
            tbl_scan_history scanHistory,
            CancellationToken cancellationToken)
        {
            try
            {
                var file = await dbContext.tbl_files
                    .FirstOrDefaultAsync(f => f.name == fileInfo.Name && f.folder_id == folderId, cancellationToken);
                    
                if (file == null)
                {
                    file = new tbl_files
                    {
                        folder_id = folderId,
                        name = fileInfo.Name
                    };
                    dbContext.tbl_files.Add(file);
                    scanHistory.new_files++;
                }
                else
                {
                    scanHistory.updated_files++;
                }
                
                file.extension = fileInfo.Extension.ToLowerInvariant();
                file.size = fileInfo.Length;
                file.created = fileInfo.CreationTimeUtc;
                file.last_modified = fileInfo.LastWriteTimeUtc;
                file.last_accessed = fileInfo.LastAccessTimeUtc;
                file.is_readonly = fileInfo.IsReadOnly;
                file.is_hidden = (fileInfo.Attributes & FileAttributes.Hidden) != 0;
                file.is_system = (fileInfo.Attributes & FileAttributes.System) != 0;
                file.is_archive = (fileInfo.Attributes & FileAttributes.Archive) != 0;
                file.is_compressed = (fileInfo.Attributes & FileAttributes.Compressed) != 0;
                file.is_encrypted = (fileInfo.Attributes & FileAttributes.Encrypted) != 0;
                
                scanHistory.files_scanned++;
                scanHistory.total_size_scanned += fileInfo.Length;
                
                // Save periodically to avoid memory issues
                if (scanHistory.files_scanned % 1000 == 0)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scanning file {fileInfo.FullName}");
                scanHistory.errors++;
            }
        }
        
        private async Task ClearExistingData(MyDataHelperDbContext dbContext, int scanRootId, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Clearing existing data for scan root {scanRootId}");
            
            // Delete files first (due to foreign key)
            var filesToDelete = await dbContext.tbl_files
                .Where(f => f.folder!.scan_root_id == scanRootId)
                .ToListAsync(cancellationToken);
            dbContext.tbl_files.RemoveRange(filesToDelete);
            
            // Delete folders
            var foldersToDelete = await dbContext.tbl_folders
                .Where(f => f.scan_root_id == scanRootId)
                .ToListAsync(cancellationToken);
            dbContext.tbl_folders.RemoveRange(foldersToDelete);
            
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        
        private void UpdateDriveInfo(tbl_scan_roots scanRoot)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(scanRoot.path) ?? scanRoot.path);
                if (driveInfo.IsReady)
                {
                    scanRoot.drive_type = driveInfo.DriveType.ToString();
                    scanRoot.volume_label = driveInfo.VolumeLabel;
                    scanRoot.total_space = driveInfo.TotalSize;
                    scanRoot.free_space = driveInfo.AvailableFreeSpace;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not get drive info for {scanRoot.path}");
            }
        }
        
        private async Task UpdateFileTypeStatistics(MyDataHelperDbContext dbContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating file type statistics");
            
            // Clear existing statistics
            dbContext.tbl_file_types.RemoveRange(dbContext.tbl_file_types);
            
            // Calculate new statistics
            var fileStats = await dbContext.tbl_files
                .GroupBy(f => f.extension ?? "(no extension)")
                .Select(g => new
                {
                    Extension = g.Key,
                    TotalSize = g.Sum(f => f.size),
                    FileCount = g.Count(),
                    AverageSize = g.Average(f => f.size),
                    MinSize = g.Min(f => f.size),
                    MaxSize = g.Max(f => f.size)
                })
                .OrderByDescending(s => s.TotalSize)
                .ToListAsync(cancellationToken);
                
            var totalSize = fileStats.Sum(s => s.TotalSize);
            var colors = GenerateColors(fileStats.Count);
            
            for (int i = 0; i < fileStats.Count; i++)
            {
                var stat = fileStats[i];
                var fileType = new tbl_file_types
                {
                    extension = stat.Extension,
                    category = GetFileCategory(stat.Extension),
                    total_size = stat.TotalSize,
                    file_count = stat.FileCount,
                    average_size = (long)stat.AverageSize,
                    min_size = stat.MinSize,
                    max_size = stat.MaxSize,
                    percentage_of_total = totalSize > 0 ? (stat.TotalSize * 100.0 / totalSize) : 0,
                    color_code = colors[i],
                    last_updated = DateTime.UtcNow
                };
                
                dbContext.tbl_file_types.Add(fileType);
            }
            
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        
        private string GetFileCategory(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" or ".webp" or ".ico" or ".tiff" => "Images",
                ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" or ".m4v" or ".mpg" => "Videos",
                ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" or ".m4a" or ".opus" => "Audio",
                ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" or ".xz" or ".cab" => "Archives",
                ".doc" or ".docx" or ".pdf" or ".txt" or ".odt" or ".rtf" or ".tex" => "Documents",
                ".xls" or ".xlsx" or ".csv" or ".ods" => "Spreadsheets",
                ".ppt" or ".pptx" or ".odp" => "Presentations",
                ".exe" or ".dll" or ".so" or ".dylib" => "Executables",
                ".cs" or ".js" or ".py" or ".java" or ".cpp" or ".c" or ".h" or ".go" or ".rs" or ".ts" => "Code",
                ".html" or ".htm" or ".css" or ".scss" or ".xml" or ".json" or ".yaml" or ".yml" => "Web",
                ".db" or ".sqlite" or ".mdb" or ".sql" => "Databases",
                _ => "Other"
            };
        }
        
        private List<string> GenerateColors(int count)
        {
            var colors = new List<string>();
            var hueStep = 360.0 / Math.Max(count, 1);
            
            for (int i = 0; i < count; i++)
            {
                var hue = i * hueStep;
                var color = HslToHex(hue, 0.7, 0.5);
                colors.Add(color);
            }
            
            return colors;
        }
        
        private string HslToHex(double h, double s, double l)
        {
            var c = (1 - Math.Abs(2 * l - 1)) * s;
            var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            var m = l - c / 2;
            
            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            
            int ri = (int)((r + m) * 255);
            int gi = (int)((g + m) * 255);
            int bi = (int)((b + m) * 255);
            
            return $"#{ri:X2}{gi:X2}{bi:X2}";
        }
    }
    
    public enum ScanType
    {
        Full,
        Incremental,
        Quick
    }
    
    public class ScanProgress
    {
        public string CurrentPath { get; set; } = string.Empty;
        public int ProcessedFiles { get; set; }
        public int ProcessedFolders { get; set; }
        public long CurrentSize { get; set; }
        public DateTime StartTime { get; set; }
        public int ScanRootId { get; set; }
    }
    
    public class ScanProgressEventArgs : EventArgs
    {
        public string CurrentPath { get; set; } = string.Empty;
        public int ProcessedFiles { get; set; }
        public int ProcessedFolders { get; set; }
        public long CurrentSize { get; set; }
    }
    
    public class ScanCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public int ScanRootId { get; set; }
        public TimeSpan Duration { get; set; }
        public long FilesScanned { get; set; }
        public long FoldersScanned { get; set; }
        public long TotalSize { get; set; }
        public string? ErrorMessage { get; set; }
    }
}