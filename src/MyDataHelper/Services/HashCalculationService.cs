using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public class HashCalculationService : IHashCalculationService
    {
        private const int BufferSize = 81920; // 80KB buffer
        
        public async Task<string> CalculateSHA256Async(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
                
                var buffer = new byte[BufferSize];
                int bytesRead;
                
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
                
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                
                return BitConverter.ToString(sha256.Hash!).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to calculate SHA256 for {filePath}");
                return string.Empty;
            }
        }
        
        public async Task<string> CalculateMD5Async(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
                
                var buffer = new byte[BufferSize];
                int bytesRead;
                
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
                
                md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                
                return BitConverter.ToString(md5.Hash!).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to calculate MD5 for {filePath}");
                return string.Empty;
            }
        }
        
        public async Task<string> CalculateQuickHashAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return string.Empty;
                
                // For quick hash, we'll use file size + modified time + first/last 1KB
                using var sha256 = SHA256.Create();
                
                // Hash file metadata
                var metadata = $"{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";
                var metadataBytes = System.Text.Encoding.UTF8.GetBytes(metadata);
                sha256.TransformBlock(metadataBytes, 0, metadataBytes.Length, null, 0);
                
                // Hash first 1KB
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var buffer = new byte[1024];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead > 0)
                    {
                        sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                    }
                    
                    // Hash last 1KB if file is larger than 2KB
                    if (fileInfo.Length > 2048)
                    {
                        stream.Seek(-1024, SeekOrigin.End);
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead > 0)
                        {
                            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                        }
                    }
                }
                
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                
                return BitConverter.ToString(sha256.Hash!).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to calculate quick hash for {filePath}");
                return string.Empty;
            }
        }
        
        public bool IsHashValid(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                return false;
                
            // Check if it's a valid hex string
            foreach (char c in hash)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            
            // Check common hash lengths
            return hash.Length == 32 ||  // MD5
                   hash.Length == 40 ||  // SHA1
                   hash.Length == 64;    // SHA256
        }
    }
}