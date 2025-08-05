using System;

namespace MyDataHelper.Services
{
    public class ScanStatusService : IScanStatusService
    {
        private string _currentStatus = "Ready";
        private int _progress = 0;
        private bool _isScanning = false;
        
        public event EventHandler<ScanStatusEventArgs>? StatusChanged;
        
        public string CurrentStatus => _currentStatus;
        public int Progress => _progress;
        public bool IsScanning => _isScanning;
        
        public void UpdateStatus(string status, int progress)
        {
            _currentStatus = status;
            _progress = Math.Min(Math.Max(progress, 0), 100);
            
            StatusChanged?.Invoke(this, new ScanStatusEventArgs
            {
                Status = _currentStatus,
                Progress = _progress,
                IsScanning = _isScanning
            });
        }
        
        public void StartScan()
        {
            _isScanning = true;
            UpdateStatus("Scan started", 0);
        }
        
        public void CompleteScan()
        {
            _isScanning = false;
            UpdateStatus("Scan completed", 100);
        }
    }
}