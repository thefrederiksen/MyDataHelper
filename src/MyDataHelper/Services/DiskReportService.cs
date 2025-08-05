using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyDataHelper.Data;

namespace MyDataHelper.Services
{
    public class DiskReportService : IDiskReportService
    {
        private readonly IDbContextFactory<MyDataHelperDbContext> _contextFactory;
        private readonly IFileAnalysisService _fileAnalysisService;
        private readonly IDuplicateDetectionService _duplicateDetectionService;
        
        public DiskReportService(
            IDbContextFactory<MyDataHelperDbContext> contextFactory,
            IFileAnalysisService fileAnalysisService,
            IDuplicateDetectionService duplicateDetectionService)
        {
            _contextFactory = contextFactory;
            _fileAnalysisService = fileAnalysisService;
            _duplicateDetectionService = duplicateDetectionService;
        }
        
        public async Task<DiskUsageReport> GenerateReportAsync(int? scanRootId = null, CancellationToken cancellationToken = default)
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var report = new DiskUsageReport
            {
                GeneratedAt = DateTime.UtcNow
            };
            
            // Get scan roots
            var scanRootsQuery = context.tbl_scan_roots.AsQueryable();
            if (scanRootId.HasValue)
            {
                scanRootsQuery = scanRootsQuery.Where(sr => sr.id == scanRootId.Value);
            }
            
            var scanRoots = await scanRootsQuery.ToListAsync(cancellationToken);
            
            foreach (var scanRoot in scanRoots)
            {
                var summary = new ScanRootSummary
                {
                    Id = scanRoot.id,
                    Path = scanRoot.path,
                    DisplayName = scanRoot.display_name,
                    Size = scanRoot.last_scan_size ?? 0,
                    FileCount = scanRoot.last_scan_file_count ?? 0,
                    FolderCount = scanRoot.last_scan_folder_count ?? 0,
                    LastScanned = scanRoot.last_scan_time
                };
                
                report.ScanRoots.Add(summary);
                report.TotalSize += summary.Size;
                report.TotalFiles += summary.FileCount;
                report.TotalFolders += summary.FolderCount;
            }
            
            // Calculate percentages
            foreach (var root in report.ScanRoots)
            {
                root.PercentageOfTotal = report.TotalSize > 0 ? (root.Size * 100.0 / report.TotalSize) : 0;
            }
            
            // Get file type breakdown
            report.FileTypeBreakdown = await _fileAnalysisService.GetFileTypeStatisticsAsync(scanRootId, cancellationToken);
            
            // Get largest files
            report.LargestFiles = await _fileAnalysisService.GetLargestFilesAsync(20, scanRootId, cancellationToken);
            
            // Get largest folders
            report.LargestFolders = await GetTopFoldersAsync(20, scanRootId, cancellationToken);
            
            // Get duplicates summary
            if (!scanRootId.HasValue) // Only for full reports
            {
                var duplicates = await _duplicateDetectionService.FindDuplicatesAsync(null, new DuplicateSearchOptions
                {
                    MinFileSize = 1024 * 1024 // 1MB minimum
                }, cancellationToken);
                
                if (duplicates.Any())
                {
                    report.DuplicatesSummary = new DuplicatesSummary
                    {
                        TotalDuplicateGroups = duplicates.Count,
                        TotalWastedSpace = duplicates.Sum(g => g.TotalWastedSpace),
                        TotalDuplicateFiles = duplicates.Sum(g => g.FileCount),
                        TopDuplicateGroups = duplicates.OrderByDescending(g => g.TotalWastedSpace).Take(10).ToList()
                    };
                }
            }
            
            return report;
        }
        
        public async Task<List<FolderSizeInfo>> GetTopFoldersAsync(int count = 20, int? scanRootId = null, CancellationToken cancellationToken = default)
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var query = context.tbl_folders.AsQueryable();
            
            if (scanRootId.HasValue)
            {
                query = query.Where(f => f.scan_root_id == scanRootId.Value);
            }
            
            var topFolders = await query
                .OrderByDescending(f => f.total_size)
                .Take(count)
                .Select(f => new FolderSizeInfo
                {
                    Id = f.id,
                    Path = f.path,
                    Name = f.name,
                    TotalSize = f.total_size,
                    FolderSize = f.folder_size,
                    FileCount = f.file_count,
                    SubfolderCount = f.subfolder_count
                })
                .ToListAsync(cancellationToken);
                
            // Calculate percentages
            var totalSize = topFolders.Sum(f => f.TotalSize);
            foreach (var folder in topFolders)
            {
                folder.PercentageOfTotal = totalSize > 0 ? (folder.TotalSize * 100.0 / totalSize) : 0;
            }
            
            return topFolders;
        }
        
        public async Task<string> ExportReportToCsvAsync(DiskUsageReport report, string filePath, CancellationToken cancellationToken = default)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine($"MyDataHelper Disk Usage Report - Generated {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();
            
            // Summary
            csv.AppendLine("Summary");
            csv.AppendLine($"Total Size,{report.TotalSize}");
            csv.AppendLine($"Total Files,{report.TotalFiles}");
            csv.AppendLine($"Total Folders,{report.TotalFolders}");
            csv.AppendLine();
            
            // Scan Roots
            csv.AppendLine("Scan Roots");
            csv.AppendLine("Path,Display Name,Size,Files,Folders,Last Scanned");
            foreach (var root in report.ScanRoots)
            {
                csv.AppendLine($"\"{root.Path}\",\"{root.DisplayName}\",{root.Size},{root.FileCount},{root.FolderCount},{root.LastScanned:yyyy-MM-dd HH:mm:ss}");
            }
            csv.AppendLine();
            
            // File Types
            csv.AppendLine("File Types");
            csv.AppendLine("Category,Extension,Size,Files,Percentage");
            foreach (var type in report.FileTypeBreakdown)
            {
                csv.AppendLine($"\"{type.Category}\",\"{type.Extension}\",{type.TotalSize},{type.FileCount},{type.PercentageOfTotal:F2}");
            }
            csv.AppendLine();
            
            // Largest Files
            csv.AppendLine("Largest Files");
            csv.AppendLine("Name,Path,Size,Extension,Modified");
            foreach (var file in report.LargestFiles)
            {
                csv.AppendLine($"\"{file.Name}\",\"{file.FullPath}\",{file.Size},\"{file.Extension}\",{file.LastModified:yyyy-MM-dd HH:mm:ss}");
            }
            csv.AppendLine();
            
            // Largest Folders
            csv.AppendLine("Largest Folders");
            csv.AppendLine("Name,Path,Total Size,Folder Size,Files,Subfolders");
            foreach (var folder in report.LargestFolders)
            {
                csv.AppendLine($"\"{folder.Name}\",\"{folder.Path}\",{folder.TotalSize},{folder.FolderSize},{folder.FileCount},{folder.SubfolderCount}");
            }
            
            // Duplicates
            if (report.DuplicatesSummary != null)
            {
                csv.AppendLine();
                csv.AppendLine("Duplicates Summary");
                csv.AppendLine($"Total Groups,{report.DuplicatesSummary.TotalDuplicateGroups}");
                csv.AppendLine($"Total Files,{report.DuplicatesSummary.TotalDuplicateFiles}");
                csv.AppendLine($"Wasted Space,{report.DuplicatesSummary.TotalWastedSpace}");
            }
            
            await File.WriteAllTextAsync(filePath, csv.ToString(), cancellationToken);
            
            return filePath;
        }
        
        public async Task<string> ExportReportToJsonAsync(DiskUsageReport report, string filePath, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(report, options);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            
            return filePath;
        }
    }
}