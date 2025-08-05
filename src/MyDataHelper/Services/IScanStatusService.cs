using System;

namespace MyDataHelper.Services
{
    public interface IScanStatusService
    {
        event EventHandler<ScanStatusEventArgs>? StatusChanged;
        
        string CurrentStatus { get; }
        int Progress { get; }
        bool IsScanning { get; }
        
        void UpdateStatus(string status, int progress);
        void StartScan();
        void CompleteScan();
    }
    
    public class ScanStatusEventArgs : EventArgs
    {
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public bool IsScanning { get; set; }
    }
}