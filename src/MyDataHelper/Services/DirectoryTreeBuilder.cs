using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyDataHelper.Models;

namespace MyDataHelper.Services
{
    public interface IDirectoryTreeBuilder
    {
        Task<List<DirectoryScanProgress>> BuildDirectoryTreeAsync(string rootPath, int maxDepth = 3, CancellationToken cancellationToken = default);
        Task<long> EstimateFilesInDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
        List<DirectoryScanProgress> FlattenDirectoryTree(List<DirectoryScanProgress> tree);
    }

    public class DirectoryTreeBuilder : IDirectoryTreeBuilder
    {
        private readonly ILogger<DirectoryTreeBuilder> _logger;
        
        public DirectoryTreeBuilder(ILogger<DirectoryTreeBuilder> logger)
        {
            _logger = logger;
        }

        public async Task<List<DirectoryScanProgress>> BuildDirectoryTreeAsync(string rootPath, int maxDepth = 3, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Building directory tree for {RootPath} with max depth {MaxDepth}", rootPath, maxDepth);
            
            var result = new List<DirectoryScanProgress>();
            
            try
            {
                await BuildDirectoryTreeRecursiveAsync(rootPath, result, 0, maxDepth, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building directory tree for {RootPath}", rootPath);
                
                // Create an error entry for the root
                result.Add(new DirectoryScanProgress
                {
                    DirectoryPath = rootPath,
                    Status = ScanStatus.Error,
                    ErrorMessage = ex.Message,
                    Depth = 0
                });
            }
            
            _logger.LogInformation("Built directory tree with {Count} directories for {RootPath}", 
                result.Count, rootPath);
            
            return result;
        }

        private async Task BuildDirectoryTreeRecursiveAsync(
            string currentPath, 
            List<DirectoryScanProgress> result, 
            int currentDepth, 
            int maxDepth,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progress = new DirectoryScanProgress
            {
                DirectoryPath = currentPath,
                Status = ScanStatus.Queued,
                Depth = currentDepth,
                StartTime = DateTime.UtcNow
            };

            result.Add(progress);

            try
            {
                progress.Status = ScanStatus.Discovering;
                
                // Quick scan to get immediate subdirectory count
                var subdirectories = Directory.GetDirectories(currentPath).ToList();
                progress.SubdirectoriesFound = subdirectories.Count;

                // If we haven't reached max depth, recurse into subdirectories
                if (currentDepth < maxDepth)
                {
                    var subdirProgressList = new List<DirectoryScanProgress>();
                    
                    foreach (var subdir in subdirectories)
                    {
                        try
                        {
                            await BuildDirectoryTreeRecursiveAsync(subdir, subdirProgressList, currentDepth + 1, maxDepth, cancellationToken);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Add error entry for inaccessible subdirectory
                            subdirProgressList.Add(new DirectoryScanProgress
                            {
                                DirectoryPath = subdir,
                                Status = ScanStatus.Error,
                                ErrorMessage = "Access denied",
                                Depth = currentDepth + 1
                            });
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // Directory might have been deleted during scan
                            _logger.LogWarning("Directory not found during tree building: {Directory}", subdir);
                        }
                    }
                    
                    progress.Subdirectories = subdirProgressList;
                    result.AddRange(subdirProgressList);
                }
                else if (subdirectories.Count > 0)
                {
                    // At max depth, create placeholder entries for deeper directories
                    foreach (var subdir in subdirectories.Take(10)) // Limit to avoid too many placeholders
                    {
                        progress.Subdirectories.Add(new DirectoryScanProgress
                        {
                            DirectoryPath = subdir,
                            Status = ScanStatus.Queued,
                            Depth = currentDepth + 1
                        });
                    }
                    
                    result.AddRange(progress.Subdirectories);
                }

                progress.Status = ScanStatus.Queued; // Ready for scanning
            }
            catch (UnauthorizedAccessException ex)
            {
                progress.Status = ScanStatus.Error;
                progress.ErrorMessage = "Access denied";
                _logger.LogWarning("Access denied to directory {Directory}: {Error}", currentPath, ex.Message);
            }
            catch (DirectoryNotFoundException ex)
            {
                progress.Status = ScanStatus.Error;
                progress.ErrorMessage = "Directory not found";
                _logger.LogWarning("Directory not found {Directory}: {Error}", currentPath, ex.Message);
            }
            catch (PathTooLongException ex)
            {
                progress.Status = ScanStatus.Error;
                progress.ErrorMessage = "Path too long";
                _logger.LogWarning("Path too long for directory {Directory}: {Error}", currentPath, ex.Message);
            }
            catch (Exception ex)
            {
                progress.Status = ScanStatus.Error;
                progress.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error processing directory {Directory}", currentPath);
            }
        }

        public async Task<long> EstimateFilesInDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            try
            {
                // Quick sampling approach - scan first few subdirectories and extrapolate
                var subdirectories = Directory.GetDirectories(directoryPath);
                var sampleSize = Math.Min(5, subdirectories.Length);
                
                if (sampleSize == 0)
                {
                    // No subdirectories, just count files in this directory
                    return Directory.GetFiles(directoryPath).Length;
                }

                long totalEstimate = 0;
                long sampledFiles = 0;
                
                // Sample first few directories
                for (int i = 0; i < sampleSize; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    try
                    {
                        var dirFiles = Directory.GetFiles(subdirectories[i], "*", SearchOption.AllDirectories).Length;
                        sampledFiles += dirFiles;
                    }
                    catch
                    {
                        // Skip directories we can't access
                    }
                }

                // Extrapolate based on sample
                if (sampleSize > 0 && sampledFiles > 0)
                {
                    var averageFilesPerDir = (double)sampledFiles / sampleSize;
                    totalEstimate = (long)(averageFilesPerDir * subdirectories.Length);
                }

                // Add files in the current directory
                totalEstimate += Directory.GetFiles(directoryPath).Length;
                
                return Math.Max(totalEstimate, 100); // Minimum estimate to avoid divide by zero
            }
            catch
            {
                return 1000; // Default estimate if sampling fails
            }
        }

        public List<DirectoryScanProgress> FlattenDirectoryTree(List<DirectoryScanProgress> tree)
        {
            var flattened = new List<DirectoryScanProgress>();
            
            foreach (var node in tree)
            {
                flattened.Add(node);
                if (node.Subdirectories.Any())
                {
                    flattened.AddRange(FlattenDirectoryTree(node.Subdirectories));
                }
            }
            
            return flattened;
        }
    }
}