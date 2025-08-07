using System;
using System.Collections.Generic;
using System.IO;

namespace MyDataHelper.Services
{
    public interface IDriveDetectionService
    {
        IEnumerable<DriveInfoModel> GetAllDrives();
        DriveInfoModel? GetDriveInfo(string driveLetter);
        bool IsDriveReady(string driveLetter);
        event EventHandler<DriveChangedEventArgs>? DriveChanged;
        void StartMonitoring();
        void StopMonitoring();
    }

    public class DriveInfoModel
    {
        public string Name { get; set; } = string.Empty;
        public string VolumeLabel { get; set; } = string.Empty;
        public DriveType DriveType { get; set; }
        public string DriveFormat { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public long TotalFreeSpace { get; set; }
        public long AvailableFreeSpace { get; set; }
        public bool IsReady { get; set; }
        public string RootDirectory { get; set; } = string.Empty;
        
        public long UsedSpace => TotalSize - TotalFreeSpace;
        public double UsedPercentage => TotalSize > 0 ? (double)UsedSpace / TotalSize * 100 : 0;
        
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(VolumeLabel))
                    return $"{VolumeLabel} ({Name})";
                return Name;
            }
        }
        
        public string DriveTypeDisplay
        {
            get
            {
                return DriveType switch
                {
                    DriveType.Fixed => "Local Disk",
                    DriveType.Removable => "Removable Drive",
                    DriveType.Network => "Network Drive",
                    DriveType.CDRom => "CD/DVD Drive",
                    DriveType.Ram => "RAM Disk",
                    _ => "Unknown"
                };
            }
        }
        
        public string DriveIcon
        {
            get
            {
                return DriveType switch
                {
                    DriveType.Fixed => "bi-hdd-fill",
                    DriveType.Removable => "bi-usb-drive-fill",
                    DriveType.Network => "bi-hdd-network-fill",
                    DriveType.CDRom => "bi-disc-fill",
                    DriveType.Ram => "bi-memory",
                    _ => "bi-device-hdd"
                };
            }
        }
    }

    public class DriveChangedEventArgs : EventArgs
    {
        public string DriveLetter { get; set; } = string.Empty;
        public DriveChangeType ChangeType { get; set; }
    }

    public enum DriveChangeType
    {
        Added,
        Removed,
        Changed
    }
}