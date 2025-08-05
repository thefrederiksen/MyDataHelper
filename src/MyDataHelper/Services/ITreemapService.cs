using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface ITreemapService
    {
        Task<TreemapNode> GenerateTreemapAsync(int? rootFolderId = null, int maxDepth = 5, CancellationToken cancellationToken = default);
        Task<List<TreemapNode>> GetChildrenAsync(int folderId, CancellationToken cancellationToken = default);
    }
    
    public class TreemapNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Type { get; set; } = "folder"; // folder or file
        public string? Extension { get; set; }
        public string Color { get; set; } = string.Empty;
        public List<TreemapNode> Children { get; set; } = new List<TreemapNode>();
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
        public double PercentageOfParent { get; set; }
    }
}