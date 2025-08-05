using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyDataHelper.Data;
using MyDataHelper.Models;

namespace MyDataHelper.Services
{
    public class TreemapService : ITreemapService
    {
        private readonly IDbContextFactory<MyDataHelperDbContext> _contextFactory;
        private readonly IFileTypeService _fileTypeService;
        
        public TreemapService(
            IDbContextFactory<MyDataHelperDbContext> contextFactory,
            IFileTypeService fileTypeService)
        {
            _contextFactory = contextFactory;
            _fileTypeService = fileTypeService;
        }
        
        public async Task<TreemapNode> GenerateTreemapAsync(int? rootFolderId = null, int maxDepth = 5, CancellationToken cancellationToken = default)
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            if (rootFolderId.HasValue)
            {
                // Generate treemap for specific folder
                var folder = await context.tbl_folders
                    .Include(f => f.files)
                    .Include(f => f.subfolders)
                    .FirstOrDefaultAsync(f => f.id == rootFolderId.Value, cancellationToken);
                    
                if (folder == null)
                    return new TreemapNode { Name = "Not Found" };
                    
                return await BuildTreemapNode(context, folder, 0, maxDepth, cancellationToken);
            }
            else
            {
                // Generate treemap for all scan roots
                var rootNode = new TreemapNode
                {
                    Name = "All Drives",
                    Type = "root",
                    Color = "#808080"
                };
                
                var scanRoots = await context.tbl_scan_roots
                    .Where(sr => sr.is_active)
                    .ToListAsync(cancellationToken);
                    
                foreach (var scanRoot in scanRoots)
                {
                    var rootFolder = await context.tbl_folders
                        .Include(f => f.files)
                        .Include(f => f.subfolders)
                        .FirstOrDefaultAsync(f => f.scan_root_id == scanRoot.id && f.parent_folder_id == null, cancellationToken);
                        
                    if (rootFolder != null)
                    {
                        var node = await BuildTreemapNode(context, rootFolder, 0, maxDepth, cancellationToken);
                        rootNode.Children.Add(node);
                        rootNode.Size += node.Size;
                        rootNode.FileCount += node.FileCount;
                        rootNode.FolderCount += node.FolderCount;
                    }
                }
                
                return rootNode;
            }
        }
        
        public async Task<List<TreemapNode>> GetChildrenAsync(int folderId, CancellationToken cancellationToken = default)
        {
            using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var folder = await context.tbl_folders
                .Include(f => f.files)
                .Include(f => f.subfolders)
                .FirstOrDefaultAsync(f => f.id == folderId, cancellationToken);
                
            if (folder == null)
                return new List<TreemapNode>();
                
            var nodes = new List<TreemapNode>();
            
            // Add subfolders
            foreach (var subfolder in folder.subfolders)
            {
                nodes.Add(new TreemapNode
                {
                    Id = subfolder.id,
                    Name = subfolder.name,
                    Path = subfolder.path,
                    Size = subfolder.total_size,
                    Type = "folder",
                    FileCount = subfolder.total_file_count,
                    FolderCount = subfolder.subfolder_count,
                    Color = "#4169E1", // Royal blue for folders
                    PercentageOfParent = folder.total_size > 0 ? (subfolder.total_size * 100.0 / folder.total_size) : 0
                });
            }
            
            // Add files
            var filesByExtension = folder.files
                .GroupBy(f => f.extension ?? "(no extension)")
                .OrderByDescending(g => g.Sum(f => f.size));
                
            foreach (var group in filesByExtension)
            {
                var totalSize = group.Sum(f => f.size);
                var color = GetColorForExtension(group.Key);
                
                nodes.Add(new TreemapNode
                {
                    Id = -1, // Files grouped by extension don't have individual IDs
                    Name = $"{group.Key} ({group.Count()} files)",
                    Path = folder.path,
                    Size = totalSize,
                    Type = "file",
                    Extension = group.Key,
                    FileCount = group.Count(),
                    Color = color,
                    PercentageOfParent = folder.total_size > 0 ? (totalSize * 100.0 / folder.total_size) : 0
                });
            }
            
            return nodes.OrderByDescending(n => n.Size).ToList();
        }
        
        private async Task<TreemapNode> BuildTreemapNode(
            MyDataHelperDbContext context,
            tbl_folders folder,
            int currentDepth,
            int maxDepth,
            CancellationToken cancellationToken)
        {
            var node = new TreemapNode
            {
                Id = folder.id,
                Name = folder.name,
                Path = folder.path,
                Size = folder.total_size,
                Type = "folder",
                FileCount = folder.total_file_count,
                FolderCount = folder.subfolder_count,
                Color = "#4169E1" // Royal blue for folders
            };
            
            if (currentDepth < maxDepth && folder.subfolder_count > 0)
            {
                // Load subfolders if not already loaded
                if (!folder.subfolders.Any())
                {
                    await context.Entry(folder)
                        .Collection(f => f.subfolders)
                        .LoadAsync(cancellationToken);
                }
                
                foreach (var subfolder in folder.subfolders.OrderByDescending(f => f.total_size).Take(10))
                {
                    var childNode = await BuildTreemapNode(context, subfolder, currentDepth + 1, maxDepth, cancellationToken);
                    childNode.PercentageOfParent = folder.total_size > 0 ? (childNode.Size * 100.0 / folder.total_size) : 0;
                    node.Children.Add(childNode);
                }
            }
            
            // Add file groups if at max depth or no subfolders
            if (currentDepth >= maxDepth || folder.subfolder_count == 0)
            {
                // Load files if not already loaded
                if (!folder.files.Any() && folder.file_count > 0)
                {
                    await context.Entry(folder)
                        .Collection(f => f.files)
                        .LoadAsync(cancellationToken);
                }
                
                var fileGroups = folder.files
                    .GroupBy(f => f.extension ?? "(no extension)")
                    .OrderByDescending(g => g.Sum(f => f.size))
                    .Take(5); // Top 5 file types
                    
                foreach (var group in fileGroups)
                {
                    var totalSize = group.Sum(f => f.size);
                    node.Children.Add(new TreemapNode
                    {
                        Name = $"{group.Key} ({group.Count()} files)",
                        Path = folder.path,
                        Size = totalSize,
                        Type = "file",
                        Extension = group.Key,
                        FileCount = group.Count(),
                        Color = GetColorForExtension(group.Key),
                        PercentageOfParent = folder.total_size > 0 ? (totalSize * 100.0 / folder.total_size) : 0
                    });
                }
            }
            
            return node;
        }
        
        private string GetColorForExtension(string extension)
        {
            var category = _fileTypeService.GetFileCategory(extension);
            
            return category switch
            {
                "Images" => "#FF6347",      // Tomato
                "Videos" => "#9370DB",      // Medium Purple
                "Audio" => "#20B2AA",       // Light Sea Green
                "Documents" => "#FFD700",   // Gold
                "Spreadsheets" => "#32CD32", // Lime Green
                "Archives" => "#FF8C00",    // Dark Orange
                "Code" => "#00CED1",        // Dark Turquoise
                "Executables" => "#DC143C", // Crimson
                "Databases" => "#4682B4",   // Steel Blue
                _ => "#808080"              // Gray
            };
        }
    }
}