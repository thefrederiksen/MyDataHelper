using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyDataHelper.Services
{
    public class DriveDetectionService : IDriveDetectionService, IDisposable
    {
        private readonly ILogger<DriveDetectionService> _logger;
        private System.Threading.Timer? _pollTimer;
        private Dictionary<string, DriveInfoModel> _lastKnownDrives;
        
        public event EventHandler<DriveChangedEventArgs>? DriveChanged;
        
        public DriveDetectionService(ILogger<DriveDetectionService> logger)
        {
            _logger = logger;
            _lastKnownDrives = new Dictionary<string, DriveInfoModel>();
            RefreshDriveList();
        }
        
        public IEnumerable<DriveInfoModel> GetAllDrives()
        {
            var drives = new List<DriveInfoModel>();
            
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        var driveModel = new DriveInfoModel
                        {
                            Name = drive.Name.TrimEnd('\\'),
                            DriveType = drive.DriveType,
                            IsReady = drive.IsReady,
                            RootDirectory = drive.RootDirectory.FullName
                        };
                        
                        if (drive.IsReady)
                        {
                            driveModel.VolumeLabel = drive.VolumeLabel ?? string.Empty;
                            driveModel.DriveFormat = drive.DriveFormat ?? string.Empty;
                            driveModel.TotalSize = drive.TotalSize;
                            driveModel.TotalFreeSpace = drive.TotalFreeSpace;
                            driveModel.AvailableFreeSpace = drive.AvailableFreeSpace;
                        }
                        
                        drives.Add(driveModel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get information for drive {Drive}", drive.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate drives");
            }
            
            return drives.OrderBy(d => d.Name);
        }
        
        public DriveInfoModel? GetDriveInfo(string driveLetter)
        {
            try
            {
                // Normalize drive letter
                if (!driveLetter.EndsWith(":\\"))
                {
                    driveLetter = driveLetter.TrimEnd(':') + ":\\";
                }
                
                var drive = new DriveInfo(driveLetter);
                
                var driveModel = new DriveInfoModel
                {
                    Name = drive.Name.TrimEnd('\\'),
                    DriveType = drive.DriveType,
                    IsReady = drive.IsReady,
                    RootDirectory = drive.RootDirectory.FullName
                };
                
                if (drive.IsReady)
                {
                    driveModel.VolumeLabel = drive.VolumeLabel ?? string.Empty;
                    driveModel.DriveFormat = drive.DriveFormat ?? string.Empty;
                    driveModel.TotalSize = drive.TotalSize;
                    driveModel.TotalFreeSpace = drive.TotalFreeSpace;
                    driveModel.AvailableFreeSpace = drive.AvailableFreeSpace;
                }
                
                return driveModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get information for drive {Drive}", driveLetter);
                return null;
            }
        }
        
        public bool IsDriveReady(string driveLetter)
        {
            try
            {
                if (!driveLetter.EndsWith(":\\"))
                {
                    driveLetter = driveLetter.TrimEnd(':') + ":\\";
                }
                
                var drive = new DriveInfo(driveLetter);
                return drive.IsReady;
            }
            catch
            {
                return false;
            }
        }
        
        public void StartMonitoring()
        {
            try
            {
                _logger.LogInformation("Starting drive monitoring");
                
                // Use a timer to poll for drive changes every 5 seconds
                // This is more reliable than WMI events for detecting all types of changes
                _pollTimer = new System.Threading.Timer(CheckForDriveChanges, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start drive monitoring");
            }
        }
        
        public void StopMonitoring()
        {
            try
            {
                _logger.LogInformation("Stopping drive monitoring");
                
                _pollTimer?.Dispose();
                _pollTimer = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop drive monitoring");
            }
        }
        
        private void CheckForDriveChanges(object? state)
        {
            try
            {
                var currentDrives = GetAllDrives().ToDictionary(d => d.Name);
                
                // Check for removed drives
                foreach (var oldDrive in _lastKnownDrives.Keys.ToList())
                {
                    if (!currentDrives.ContainsKey(oldDrive))
                    {
                        _logger.LogInformation("Drive {Drive} was removed", oldDrive);
                        DriveChanged?.Invoke(this, new DriveChangedEventArgs 
                        { 
                            DriveLetter = oldDrive, 
                            ChangeType = DriveChangeType.Removed 
                        });
                        _lastKnownDrives.Remove(oldDrive);
                    }
                }
                
                // Check for new drives
                foreach (var currentDrive in currentDrives)
                {
                    if (!_lastKnownDrives.ContainsKey(currentDrive.Key))
                    {
                        _logger.LogInformation("Drive {Drive} was added", currentDrive.Key);
                        DriveChanged?.Invoke(this, new DriveChangedEventArgs 
                        { 
                            DriveLetter = currentDrive.Key, 
                            ChangeType = DriveChangeType.Added 
                        });
                        _lastKnownDrives[currentDrive.Key] = currentDrive.Value;
                    }
                    else
                    {
                        // Check for significant changes (like free space changes > 1GB)
                        var oldDrive = _lastKnownDrives[currentDrive.Key];
                        var newDrive = currentDrive.Value;
                        
                        if (Math.Abs(oldDrive.TotalFreeSpace - newDrive.TotalFreeSpace) > 1073741824) // 1GB
                        {
                            DriveChanged?.Invoke(this, new DriveChangedEventArgs 
                            { 
                                DriveLetter = currentDrive.Key, 
                                ChangeType = DriveChangeType.Changed 
                            });
                            _lastKnownDrives[currentDrive.Key] = newDrive;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for drive changes");
            }
        }
        
        private void RefreshDriveList()
        {
            _lastKnownDrives = GetAllDrives().ToDictionary(d => d.Name);
        }
        
        public void Dispose()
        {
            StopMonitoring();
        }
    }
}