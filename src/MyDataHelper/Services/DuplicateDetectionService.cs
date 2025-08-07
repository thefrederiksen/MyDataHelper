using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyDataHelper.Data;

namespace MyDataHelper.Services
{
    public class DuplicateDetectionService : IDuplicateDetectionService
    {
        private readonly IDbContextFactory<MyDataHelperDbContext> _contextFactory;
        private readonly IHashCalculationService _hashCalculationService;
        
        public DuplicateDetectionService(
            IDbContextFactory<MyDataHelperDbContext> contextFactory,
            IHashCalculationService hashCalculationService)
        {
            _contextFactory = contextFactory;
            _hashCalculationService = hashCalculationService;
        }
        
        public async Task<List<DuplicateGroup>> FindDuplicatesAsync(
            int? scanRootId = null, 
            DuplicateSearchOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            options ??= new DuplicateSearchOptions();
            
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            // First, find files with the same size (potential duplicates)
            var query = context.tbl_files
                .Include(f => f.folder)
                .Where(f => f.size > 0); // Skip empty files by default
                
            if (options.SkipEmptyFiles)
            {
                query = query.Where(f => f.size > 0);
            }
            
            if (options.MinFileSize.HasValue)
            {
                query = query.Where(f => f.size >= options.MinFileSize.Value);
            }
            
            if (options.MaxFileSize.HasValue)
            {
                query = query.Where(f => f.size <= options.MaxFileSize.Value);
            }
            
            if (scanRootId.HasValue)
            {
                query = query.Where(f => f.folder!.scan_root_id == scanRootId.Value);
            }
            
            if (options.IncludeExtensions?.Any() == true)
            {
                query = query.Where(f => f.extension != null && options.IncludeExtensions.Contains(f.extension));
            }
            
            if (options.ExcludeExtensions?.Any() == true)
            {
                query = query.Where(f => f.extension == null || !options.ExcludeExtensions.Contains(f.extension));
            }
            
            // Group by size first
            var sizeGroups = await query
                .GroupBy(f => f.size)
                .Where(g => g.Count() > 1)
                .Select(g => new { Size = g.Key, Files = g.ToList() })
                .ToListAsync(cancellationToken);
            
            var duplicateGroups = new List<DuplicateGroup>();
            
            foreach (var sizeGroup in sizeGroups)
            {
                // Group by hash within same size
                var hashGroups = sizeGroup.Files
                    .Where(f => !string.IsNullOrEmpty(f.hash))
                    .GroupBy(f => f.hash)
                    .Where(g => g.Count() > 1);
                    
                foreach (var hashGroup in hashGroups)
                {
                    var files = hashGroup.Select(f => new DuplicateFile
                    {
                        Id = f.id,
                        Name = f.name,
                        FullPath = Path.Combine(f.folder!.path, f.name),
                        Size = f.size,
                        LastModified = f.last_modified,
                        Created = f.created
                    }).ToList();
                    
                    var duplicateGroup = new DuplicateGroup
                    {
                        Hash = hashGroup.Key!,
                        FileSize = sizeGroup.Size,
                        FileCount = files.Count,
                        TotalWastedSpace = (files.Count - 1) * sizeGroup.Size,
                        Files = files
                    };
                    
                    duplicateGroups.Add(duplicateGroup);
                }
                
                // For files without hashes but same size, we might need to calculate hashes
                var filesWithoutHash = sizeGroup.Files.Where(f => string.IsNullOrEmpty(f.hash)).ToList();
                if (filesWithoutHash.Count > 1)
                {
                    // This could be expensive, so only do it for smaller files or when explicitly requested
                    if (sizeGroup.Size < 100 * 1024 * 1024) // Less than 100MB
                    {
                        var hashMap = new Dictionary<string, List<DuplicateFile>>();
                        
                        foreach (var file in filesWithoutHash)
                        {
                            var fullPath = Path.Combine(file.folder!.path, file.name);
                            if (File.Exists(fullPath))
                            {
                                var hash = await _hashCalculationService.CalculateQuickHashAsync(fullPath, cancellationToken);
                                if (!string.IsNullOrEmpty(hash))
                                {
                                    if (!hashMap.ContainsKey(hash))
                                        hashMap[hash] = new List<DuplicateFile>();
                                        
                                    hashMap[hash].Add(new DuplicateFile
                                    {
                                        Id = file.id,
                                        Name = file.name,
                                        FullPath = fullPath,
                                        Size = file.size,
                                        LastModified = file.last_modified,
                                        Created = file.created
                                    });
                                }
                            }
                        }
                        
                        foreach (var kvp in hashMap.Where(h => h.Value.Count > 1))
                        {
                            var duplicateGroup = new DuplicateGroup
                            {
                                Hash = kvp.Key,
                                FileSize = sizeGroup.Size,
                                FileCount = kvp.Value.Count,
                                TotalWastedSpace = (kvp.Value.Count - 1) * sizeGroup.Size,
                                Files = kvp.Value
                            };
                            
                            duplicateGroups.Add(duplicateGroup);
                        }
                    }
                }
            }
            
            return duplicateGroups.OrderByDescending(g => g.TotalWastedSpace).ToList();
        }
        
        public async Task<string> CalculateFileHashAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await _hashCalculationService.CalculateSHA256Async(filePath, cancellationToken);
        }
        
        public async Task UpdateFileHashesAsync(int scanRootId, IProgress<HashingProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            // Get all files without hashes
            var filesWithoutHash = await context.tbl_files
                .Include(f => f.folder)
                .Where(f => f.folder!.scan_root_id == scanRootId && string.IsNullOrEmpty(f.hash))
                .ToListAsync(cancellationToken);
                
            var totalFiles = filesWithoutHash.Count;
            var processedFiles = 0;
            var totalBytes = filesWithoutHash.Sum(f => f.size);
            var processedBytes = 0L;
            
            foreach (var file in filesWithoutHash)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var fullPath = Path.Combine(file.folder!.path, file.name);
                
                progress?.Report(new HashingProgress
                {
                    TotalFiles = totalFiles,
                    ProcessedFiles = processedFiles,
                    CurrentFile = file.name,
                    TotalBytes = totalBytes,
                    BytesProcessed = processedBytes
                });
                
                if (File.Exists(fullPath))
                {
                    try
                    {
                        file.hash = await _hashCalculationService.CalculateSHA256Async(fullPath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, $"Failed to calculate hash for {fullPath}");
                    }
                }
                
                processedFiles++;
                processedBytes += file.size;
                
                // Save periodically
                if (processedFiles % 100 == 0)
                {
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
            
            // Final save
            await context.SaveChangesAsync(cancellationToken);
            
            progress?.Report(new HashingProgress
            {
                TotalFiles = totalFiles,
                ProcessedFiles = processedFiles,
                CurrentFile = "Complete",
                TotalBytes = totalBytes,
                BytesProcessed = processedBytes
            });
        }
    }
}