using System;
using System.Collections.Concurrent;
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
    public interface IParallelDiskScanService
    {
        bool IsScanning { get; }
        bool IsPaused { get; }
        EnhancedScanProgress? CurrentProgress { get; }
        
        // In-memory scan results
        ConcurrentDictionary<string, InMemoryFileInfo> ScannedFiles { get; }
        ConcurrentDictionary<string, InMemoryFolderInfo> ScannedFolders { get; }
        
        event EventHandler<EnhancedScanProgressEventArgs>? ScanProgressChanged;
        event EventHandler<EnhancedScanCompletedEventArgs>? ScanCompleted;
        
        Task StartEnhancedScanAsync(int scanRootId, CancellationToken cancellationToken = default);
        Task StartEnhancedDriveScanAsync(string driveLetter, CancellationToken cancellationToken = default);
        void CancelScan();
        void PauseScan();
        void ResumeScan();
    }
    
    // In-memory data structures for fast scanning
    public class InMemoryFileInfo
    {
        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime LastAccessed { get; set; }
        public FileAttributes Attributes { get; set; }
        public string DirectoryPath { get; set; } = string.Empty;
    }
    
    public class InMemoryFolderInfo
    {
        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ParentPath { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public int SubfolderCount { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime ScannedAt { get; set; }
        public int Depth { get; set; }
        public List<string> Files { get; set; } = new();
        public List<string> Subfolders { get; set; } = new();
    }

    public class ParallelDiskScanService : IParallelDiskScanService
    {
        private readonly ILogger<ParallelDiskScanService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDirectoryTreeBuilder _treeBuilder;
        private readonly IScanStatusService _scanStatusService;
        
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _scanTask;
        private EnhancedScanProgress? _currentProgress;
        private readonly object _progressLock = new object();
        private DateTime _lastUiUpdate = DateTime.MinValue;
        
        // Pause/Resume functionality
        private readonly ManualResetEventSlim _pauseEvent = new(true); // Start in resumed state
        private volatile bool _isPaused = false;
        
        // Thread-safe collections for parallel scanning
        private readonly ConcurrentBag<DirectoryScanProgress> _completedDirectories = new();
        private readonly ConcurrentQueue<DirectoryScanProgress> _scanQueue = new();
        private readonly ConcurrentDictionary<string, DirectoryScanProgress> _progressDict = new();
        
        // In-memory storage for scan results - MUCH FASTER!
        private readonly ConcurrentDictionary<string, InMemoryFileInfo> _scannedFiles = new();
        private readonly ConcurrentDictionary<string, InMemoryFolderInfo> _scannedFolders = new();
        
        public event EventHandler<EnhancedScanProgressEventArgs>? ScanProgressChanged;
        public event EventHandler<EnhancedScanCompletedEventArgs>? ScanCompleted;
        
        public bool IsScanning => _scanTask != null && !_scanTask.IsCompleted;
        public bool IsPaused => _isPaused;
        public ConcurrentDictionary<string, InMemoryFileInfo> ScannedFiles => _scannedFiles;
        public ConcurrentDictionary<string, InMemoryFolderInfo> ScannedFolders => _scannedFolders;
        public EnhancedScanProgress? CurrentProgress => _currentProgress;

        public ParallelDiskScanService(
            ILogger<ParallelDiskScanService> logger,
            IServiceProvider serviceProvider,
            IDirectoryTreeBuilder treeBuilder,
            IScanStatusService scanStatusService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _treeBuilder = treeBuilder;
            _scanStatusService = scanStatusService;
        }

        public async Task StartEnhancedScanAsync(int scanRootId, CancellationToken cancellationToken = default)
        {
            if (IsScanning)
            {
                _logger.LogWarning("Enhanced scan already in progress");
                return;
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _scanTask = Task.Run(() => PerformEnhancedScanAsync(scanRootId, _cancellationTokenSource.Token));
        }

        public async Task StartEnhancedDriveScanAsync(string driveLetter, CancellationToken cancellationToken = default)
        {
            if (IsScanning)
            {
                _logger.LogWarning("Enhanced drive scan already in progress");
                return;
            }

            // Normalize drive letter
            if (!driveLetter.EndsWith(":\\"))
            {
                driveLetter = driveLetter.TrimEnd(':') + ":\\";
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _scanTask = Task.Run(() => PerformEnhancedDriveScanAsync(driveLetter, _cancellationTokenSource.Token));
        }

        public void CancelScan()
        {
            _cancellationTokenSource?.Cancel();
            _logger.LogInformation("Enhanced scan cancellation requested");
        }
        
        public void PauseScan()
        {
            if (IsScanning && !_isPaused)
            {
                _isPaused = true;
                _pauseEvent.Reset(); // Block scanning threads
                _logger.LogInformation("Enhanced scan paused");
            }
        }
        
        public void ResumeScan()
        {
            if (IsScanning && _isPaused)
            {
                _isPaused = false;
                _pauseEvent.Set(); // Unblock scanning threads
                _logger.LogInformation("Enhanced scan resumed");
            }
        }

        private async Task PerformEnhancedScanAsync(int scanRootId, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("Starting enhanced scan for scan root ID {ScanRootId}", scanRootId);

                // Get scan root from database
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                var scanRoot = await dbContext.tbl_scan_roots.FindAsync(scanRootId);
                
                if (scanRoot == null)
                {
                    throw new ArgumentException($"Scan root with ID {scanRootId} not found");
                }

                await PerformEnhancedScanInternalAsync(scanRoot.path, scanRootId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Enhanced scan was cancelled");
                NotifyCompleted(false, scanRootId, DateTime.UtcNow - startTime, "Scan was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced scan failed");
                NotifyCompleted(false, scanRootId, DateTime.UtcNow - startTime, ex.Message);
            }
        }

        private async Task PerformEnhancedDriveScanAsync(string driveLetter, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("Starting enhanced drive scan for {DriveLetter}", driveLetter);

                // Create or get scan root for drive
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                
                var scanRoot = await dbContext.tbl_scan_roots
                    .FirstOrDefaultAsync(sr => sr.path == driveLetter, cancellationToken);
                
                if (scanRoot == null)
                {
                    scanRoot = new tbl_scan_roots
                    {
                        path = driveLetter,
                        display_name = $"Drive {driveLetter[0]}",
                        is_active = true,
                        include_subdirectories = true,
                        follow_symlinks = false
                    };
                    
                    dbContext.tbl_scan_roots.Add(scanRoot);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                await PerformEnhancedScanInternalAsync(driveLetter, scanRoot.id, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Enhanced drive scan was cancelled");
                NotifyCompleted(false, 0, DateTime.UtcNow - startTime, "Scan was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced drive scan failed");
                NotifyCompleted(false, 0, DateTime.UtcNow - startTime, ex.Message);
            }
        }

        private async Task PerformEnhancedScanInternalAsync(string rootPath, int scanRootId, CancellationToken cancellationToken)
        {
            var scanStartTime = DateTime.UtcNow;
            
            // Initialize progress
            _currentProgress = new EnhancedScanProgress
            {
                ScanRootPath = rootPath,
                ScanRootId = scanRootId,
                ScanStartTime = scanStartTime
            };

            // Phase 1: Directory Discovery
            _logger.LogInformation("Phase 1: Building directory tree for {RootPath}", rootPath);
            var currentPhase = new ScanPhaseProgress
            {
                PhaseName = "Directory Discovery",
                CurrentStep = 0,
                TotalSteps = 1,
                CurrentActivity = "Scanning directory structure...",
                PhaseStartTime = DateTime.UtcNow
            };

            NotifyProgress(currentPhase, isSignificant: true);

            var directoryTree = await _treeBuilder.BuildDirectoryTreeAsync(rootPath, maxDepth: 4, cancellationToken);
            var flatDirectories = _treeBuilder.FlattenDirectoryTree(directoryTree);
            
            // Filter out error directories for scanning
            var scannableDirectories = flatDirectories.Where(d => d.Status != ScanStatus.Error).ToList();
            
            _currentProgress.TotalDirectories = scannableDirectories.Count;
            _currentProgress.QueuedDirectories = scannableDirectories.Count;

            // Populate progress dictionary
            foreach (var dir in flatDirectories)
            {
                _progressDict[dir.DirectoryPath] = dir;
                _currentProgress.DirectoryProgress[dir.DirectoryPath] = dir;
            }

            // Estimate total files for progress calculation
            _logger.LogInformation("Estimating total files in {DirectoryCount} directories", scannableDirectories.Count);
            var estimationTasks = scannableDirectories.Take(10).Select(d => 
                _treeBuilder.EstimateFilesInDirectoryAsync(d.DirectoryPath, cancellationToken));
            
            var estimates = await Task.WhenAll(estimationTasks);
            var avgFilesPerDir = estimates.Any() ? estimates.Average() : 1000;
            _currentProgress.EstimatedFilesTotal = (long)(avgFilesPerDir * scannableDirectories.Count);

            // Phase 2: Parallel Directory Scanning
            _logger.LogInformation("Phase 2: Starting parallel scan of {DirectoryCount} directories", scannableDirectories.Count);
            currentPhase = new ScanPhaseProgress
            {
                PhaseName = "Parallel Scanning",
                CurrentStep = 0,
                TotalSteps = scannableDirectories.Count,
                CurrentActivity = "Scanning files in parallel...",
                PhaseStartTime = DateTime.UtcNow
            };

            // Clear collections
            _completedDirectories.Clear();
            while (_scanQueue.TryDequeue(out _)) { }

            // Enqueue directories for scanning
            foreach (var dir in scannableDirectories)
            {
                _scanQueue.Enqueue(dir);
            }

            // Start parallel scanning with limited concurrency
            var maxConcurrency = Math.Min(Environment.ProcessorCount, 8);
            var scanTasks = new List<Task>();
            
            for (int i = 0; i < maxConcurrency; i++)
            {
                scanTasks.Add(ScanDirectoryConcurrentlyAsync(scanRootId, cancellationToken));
            }

            // Wait for all scanning tasks to complete
            await Task.WhenAll(scanTasks);

            // Phase 3: Database Operations
            _logger.LogInformation("Phase 3: Finalizing database operations");
            currentPhase = new ScanPhaseProgress
            {
                PhaseName = "Database Operations",
                CurrentStep = 0,
                TotalSteps = 1,
                CurrentActivity = "Updating database statistics...",
                PhaseStartTime = DateTime.UtcNow
            };

            NotifyProgress(currentPhase, isSignificant: true);

            // Save scan data to database
            await SaveScanDataToDatabaseAsync(scanRootId, rootPath, cancellationToken);
            
            await FinalizeStatisticsAsync(scanRootId, cancellationToken);

            // Scan completed successfully
            var duration = DateTime.UtcNow - scanStartTime;
            _logger.LogInformation("Enhanced scan completed in {Duration:mm\\:ss}", duration);
            
            NotifyCompleted(true, scanRootId, duration);
        }

        private async Task ScanDirectoryConcurrentlyAsync(int scanRootId, CancellationToken cancellationToken)
        {
            // FAST IN-MEMORY SCANNING - NO DATABASE CONTEXT NEEDED!

            while (!cancellationToken.IsCancellationRequested && _scanQueue.TryDequeue(out var directory))
            {
                try
                {
                    // Update directory status
                    directory.Status = ScanStatus.Scanning;
                    directory.StartTime = DateTime.UtcNow;
                    _currentProgress!.ScanningDirectories++;
                    _currentProgress.QueuedDirectories--;

                    // FAST SCAN: Only get what we need!
                    var (files, folders) = await FastScanDirectoryAsync(directory.DirectoryPath, scanRootId, cancellationToken);
                    
                    // Update progress
                    directory.FilesFound = files.Count;
                    directory.SubdirectoriesFound = folders.Count;
                    directory.SizeScanned = files.Sum(f => f.Size);
                    directory.Status = ScanStatus.Completed;
                    directory.CompletionTime = DateTime.UtcNow;

                    // NO DATABASE OPERATIONS - IN-MEMORY ONLY!

                    // Update global progress
                    lock (_progressLock)
                    {
                        _currentProgress.ProcessedFiles += files.Count;
                        _currentProgress.TotalSizeBytes += directory.SizeScanned;
                        _currentProgress.CompletedDirectories++;
                        _currentProgress.ScanningDirectories--;
                    }

                    _completedDirectories.Add(directory);

                    // NO DATABASE OPERATIONS - PURE IN-MEMORY!

                    // Throttled UI updates (every 2 seconds max)
                    if (DateTime.UtcNow - _lastUiUpdate > TimeSpan.FromSeconds(2))
                    {
                        NotifyProgress(null, directory.DirectoryPath, isSignificant: false);
                        _lastUiUpdate = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error scanning directory {Directory}", directory.DirectoryPath);
                    
                    directory.Status = ScanStatus.Error;
                    directory.ErrorMessage = ex.Message;
                    directory.CompletionTime = DateTime.UtcNow;
                    
                    lock (_progressLock)
                    {
                        _currentProgress!.ErrorDirectories++;
                        _currentProgress.QueuedDirectories--;
                    }
                }
            }

            // NO FINAL DATABASE OPERATIONS - ALL IN MEMORY!
        }

        private async Task<(List<InMemoryFileInfo> files, List<InMemoryFolderInfo> folders)> FastScanDirectoryAsync(
            string directoryPath, int scanRootId, CancellationToken cancellationToken)
        {
            var files = new List<InMemoryFileInfo>();
            var folders = new List<InMemoryFolderInfo>();

            try
            {
                // Check for pause
                _pauseEvent.Wait(cancellationToken);
                
                var dirInfo = new DirectoryInfo(directoryPath);
                
                // FAST SCAN: Only get essential file info - name, size, extension!
                var fileInfos = dirInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                foreach (var fileInfo in fileInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _pauseEvent.Wait(cancellationToken); // Check for pause
                    
                    try
                    {
                        // MINIMAL DATA - MAXIMUM SPEED!
                        var fastFile = new InMemoryFileInfo
                        {
                            FullPath = fileInfo.FullName,
                            Name = fileInfo.Name,
                            Extension = Path.GetExtension(fileInfo.Name).TrimStart('.'),
                            Size = fileInfo.Length, // This is the main thing we need
                            DirectoryPath = directoryPath
                            // Skip all the slow attributes and dates!
                        };
                        
                        files.Add(fastFile);
                        
                        // Store in concurrent dictionary for instant access
                        _scannedFiles.TryAdd(fileInfo.FullName, fastFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error processing file {File}", fileInfo.FullName);
                    }
                }

                // FAST SCAN: Only get essential folder info - name and path!
                var subdirInfos = dirInfo.GetDirectories();
                foreach (var subdirInfo in subdirInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _pauseEvent.Wait(cancellationToken); // Check for pause
                    
                    try
                    {
                        // MINIMAL DATA - MAXIMUM SPEED!
                        var fastFolder = new InMemoryFolderInfo
                        {
                            FullPath = subdirInfo.FullName,
                            Name = subdirInfo.Name,
                            ParentPath = directoryPath,
                            ScannedAt = DateTime.UtcNow,
                            // Skip all the slow calculations!
                        };
                        
                        folders.Add(fastFolder);
                        
                        // Store in concurrent dictionary for instant access
                        _scannedFolders.TryAdd(subdirInfo.FullName, fastFolder);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error processing folder {Folder}", subdirInfo.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scanning directory {Directory}", directoryPath);
                throw;
            }

            return (files, folders);
        }

        private async Task BatchInsertToDatabase(MyDataHelperDbContext dbContext, 
            List<tbl_files> files, List<tbl_folders> folders, CancellationToken cancellationToken)
        {
            try
            {
                if (files.Any())
                {
                    await dbContext.tbl_files.AddRangeAsync(files, cancellationToken);
                }
                
                if (folders.Any())
                {
                    await dbContext.tbl_folders.AddRangeAsync(folders, cancellationToken);
                }
                
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch inserting to database");
                throw;
            }
        }

        private async Task FinalizeStatisticsAsync(int scanRootId, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                
                // Update scan root statistics
                var scanRoot = await dbContext.tbl_scan_roots.FindAsync(scanRootId);
                if (scanRoot != null)
                {
                    var stats = await dbContext.tbl_files
                        .Join(dbContext.tbl_folders,
                            f => f.folder_id,
                            folder => folder.id,
                            (f, folder) => new { File = f, Folder = folder })
                        .Where(joined => joined.Folder.scan_root_id == scanRootId)
                        .GroupBy(joined => 1)
                        .Select(g => new { TotalSize = g.Sum(joined => joined.File.size), FileCount = g.Count() })
                        .FirstOrDefaultAsync(cancellationToken);

                    scanRoot.last_scan_time = DateTime.UtcNow;
                    scanRoot.last_scan_size = stats?.TotalSize ?? 0;
                    scanRoot.last_scan_file_count = stats?.FileCount ?? 0;
                    
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                
                // Update file type statistics  
                await UpdateFileTypeStatisticsAsync(dbContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing statistics");
            }
        }

        private async Task UpdateFileTypeStatisticsAsync(MyDataHelperDbContext dbContext, CancellationToken cancellationToken)
        {
            // This is a simplified version - you can expand based on existing implementation
            _logger.LogInformation("Updating file type statistics");
            
            var fileStats = await dbContext.tbl_files
                .GroupBy(f => f.extension ?? "(no extension)")
                .Select(g => new
                {
                    Extension = g.Key,
                    TotalSize = g.Sum(f => f.size),
                    FileCount = g.Count()
                })
                .OrderByDescending(s => s.TotalSize)
                .ToListAsync(cancellationToken);

            // Clear and update file types table
            dbContext.tbl_file_types.RemoveRange(dbContext.tbl_file_types);
            
            foreach (var stat in fileStats)
            {
                dbContext.tbl_file_types.Add(new tbl_file_types
                {
                    extension = stat.Extension,
                    total_size = stat.TotalSize,
                    file_count = stat.FileCount,
                    last_updated = DateTime.UtcNow
                });
            }
            
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task SaveScanDataToDatabaseAsync(int scanRootId, string rootPath, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Saving scan data to database for scan root {ScanRootId}", scanRootId);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDataHelperDbContext>();
                
                // Clear existing data for this scan root
                var existingFolders = dbContext.tbl_folders.Where(f => f.scan_root_id == scanRootId);
                dbContext.tbl_folders.RemoveRange(existingFolders);
                await dbContext.SaveChangesAsync(cancellationToken);
                
                // Get all scanned data from memory
                var allDirectories = _completedDirectories.ToList();
                var allFiles = _scannedFiles.Values.ToList();
                
                _logger.LogInformation("Saving {FolderCount} folders and {FileCount} files to database", 
                    allDirectories.Count, allFiles.Count);
                
                // Create a dictionary to map folder paths to folder IDs
                var folderIdMap = new Dictionary<string, int>();
                
                // Save folders first
                foreach (var dir in allDirectories)
                {
                    var folder = new tbl_folders
                    {
                        scan_root_id = scanRootId,
                        path = dir.DirectoryPath,
                        name = Path.GetFileName(dir.DirectoryPath) ?? dir.DirectoryPath,
                        total_size = dir.SizeScanned,
                        folder_size = dir.SizeScanned,
                        file_count = dir.FilesFound,
                        total_file_count = dir.FilesFound,
                        subfolder_count = dir.SubdirectoriesFound,
                        last_modified = DateTime.UtcNow,
                        last_scanned = DateTime.UtcNow,
                        depth = dir.DirectoryPath.Count(c => c == Path.DirectorySeparatorChar) - rootPath.Count(c => c == Path.DirectorySeparatorChar),
                        is_accessible = dir.Status != ScanStatus.Error,
                        error_message = dir.ErrorMessage
                    };
                    
                    // Set parent folder ID if this is not the root
                    if (dir.DirectoryPath != rootPath)
                    {
                        var parentPath = Path.GetDirectoryName(dir.DirectoryPath);
                        if (!string.IsNullOrEmpty(parentPath) && folderIdMap.ContainsKey(parentPath))
                        {
                            folder.parent_folder_id = folderIdMap[parentPath];
                        }
                    }
                    
                    dbContext.tbl_folders.Add(folder);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    
                    // Store the folder ID for child folders to reference
                    folderIdMap[dir.DirectoryPath] = folder.id;
                }
                
                // Save files
                foreach (var fileInfo in allFiles)
                {
                    // Find the folder ID for this file
                    var folderPath = Path.GetDirectoryName(fileInfo.FullPath) ?? "";
                    if (folderIdMap.TryGetValue(folderPath, out var folderId))
                    {
                        var file = new tbl_files
                        {
                            folder_id = folderId,
                            name = fileInfo.Name,
                            extension = fileInfo.Extension,
                            size = fileInfo.Size,
                            created = DateTime.UtcNow,
                            last_modified = DateTime.UtcNow,
                            last_accessed = DateTime.UtcNow,
                            is_readonly = false,
                            is_hidden = false,
                            is_system = false,
                            is_archive = false,
                            is_compressed = false,
                            is_encrypted = false
                        };
                        
                        dbContext.tbl_files.Add(file);
                    }
                }
                
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully saved scan data to database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving scan data to database");
            }
        }
        
        private void NotifyProgress(ScanPhaseProgress? phase = null, string? completedDirectory = null, bool isSignificant = false)
        {
            ScanProgressChanged?.Invoke(this, new EnhancedScanProgressEventArgs
            {
                Progress = _currentProgress!,
                CurrentPhase = phase,
                RecentlyCompletedDirectory = completedDirectory,
                IsSignificantUpdate = isSignificant
            });
        }

        private void NotifyCompleted(bool success, int scanRootId, TimeSpan duration, string? errorMessage = null)
        {
            var completedArgs = new EnhancedScanCompletedEventArgs
            {
                Success = success,
                ScanRootId = scanRootId,
                Duration = duration,
                FilesScanned = _currentProgress?.ProcessedFiles ?? 0,
                FoldersScanned = _currentProgress?.CompletedDirectories ?? 0,
                TotalSize = _currentProgress?.TotalSizeBytes ?? 0,
                ErrorMessage = errorMessage,
                ErrorCount = _currentProgress?.ErrorDirectories ?? 0,
                FinalProgress = _currentProgress ?? new EnhancedScanProgress()
            };

            ScanCompleted?.Invoke(this, completedArgs);
        }
    }
}