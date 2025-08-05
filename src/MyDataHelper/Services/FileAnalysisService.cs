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
    public class FileAnalysisService : IFileAnalysisService
    {
        private readonly IDbContextFactory<MyDataHelperDbContext> _contextFactory;
        private readonly IFileTypeService _fileTypeService;
        
        public FileAnalysisService(
            IDbContextFactory<MyDataHelperDbContext> contextFactory,
            IFileTypeService fileTypeService)
        {
            _contextFactory = contextFactory;
            _fileTypeService = fileTypeService;
        }
        
        public async Task<FileAnalysisResult> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var fileInfo = new FileInfo(filePath);
            var extension = fileInfo.Extension.ToLowerInvariant();
            
            var result = new FileAnalysisResult
            {
                FileName = fileInfo.Name,
                Extension = extension,
                Size = fileInfo.Length,
                Category = _fileTypeService.GetFileCategory(extension),
                MimeType = _fileTypeService.GetMimeType(extension),
                IsText = _fileTypeService.IsTextFile(extension),
                IsBinary = !_fileTypeService.IsTextFile(extension)
            };
            
            return await Task.FromResult(result);
        }
        
        public async Task<List<FileTypeStatistics>> GetFileTypeStatisticsAsync(int? scanRootId = null, CancellationToken cancellationToken = default)
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var query = context.tbl_file_types.AsQueryable();
            
            var stats = await query
                .OrderByDescending(ft => ft.total_size)
                .Select(ft => new FileTypeStatistics
                {
                    Extension = ft.extension,
                    Category = ft.category ?? "Other",
                    TotalSize = ft.total_size,
                    FileCount = ft.file_count,
                    PercentageOfTotal = ft.percentage_of_total,
                    ColorCode = ft.color_code ?? "#808080"
                })
                .ToListAsync(cancellationToken);
                
            // If no stats available, calculate from files directly
            if (!stats.Any() && scanRootId.HasValue)
            {
                var fileStats = await context.tbl_files
                    .Where(f => f.folder!.scan_root_id == scanRootId.Value)
                    .GroupBy(f => new { Extension = f.extension ?? "(no extension)", Category = _fileTypeService.GetFileCategory(f.extension ?? "") })
                    .Select(g => new
                    {
                        g.Key.Extension,
                        g.Key.Category,
                        TotalSize = g.Sum(f => f.size),
                        FileCount = g.Count()
                    })
                    .ToListAsync(cancellationToken);
                    
                var totalSize = fileStats.Sum(s => s.TotalSize);
                var colors = GenerateColors(fileStats.Count);
                
                for (int i = 0; i < fileStats.Count; i++)
                {
                    var stat = fileStats[i];
                    stats.Add(new FileTypeStatistics
                    {
                        Extension = stat.Extension,
                        Category = stat.Category,
                        TotalSize = stat.TotalSize,
                        FileCount = stat.FileCount,
                        PercentageOfTotal = totalSize > 0 ? (stat.TotalSize * 100.0 / totalSize) : 0,
                        ColorCode = colors[i]
                    });
                }
            }
            
            return stats;
        }
        
        public async Task<List<LargeFileInfo>> GetLargestFilesAsync(int count = 100, int? scanRootId = null, CancellationToken cancellationToken = default)
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var query = context.tbl_files
                .Include(f => f.folder)
                .AsQueryable();
                
            if (scanRootId.HasValue)
            {
                query = query.Where(f => f.folder!.scan_root_id == scanRootId.Value);
            }
            
            var largestFiles = await query
                .OrderByDescending(f => f.size)
                .Take(count)
                .Select(f => new LargeFileInfo
                {
                    Id = f.id,
                    Name = f.name,
                    FullPath = Path.Combine(f.folder!.path, f.name),
                    Size = f.size,
                    Extension = f.extension ?? "",
                    LastModified = f.last_modified
                })
                .ToListAsync(cancellationToken);
                
            return largestFiles;
        }
        
        public async Task<long> CalculateFolderSizeAsync(int folderId, CancellationToken cancellationToken = default)
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var folder = await context.tbl_folders
                .FirstOrDefaultAsync(f => f.id == folderId, cancellationToken);
                
            return folder?.total_size ?? 0;
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
}