using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface IDiskScanService
    {
        bool IsScanning { get; }
        ScanProgress? CurrentProgress { get; }
        
        event EventHandler<ScanProgressEventArgs>? ScanProgressChanged;
        event EventHandler<ScanCompletedEventArgs>? ScanCompleted;
        
        Task StartScanAsync(int scanRootId, ScanType scanType = ScanType.Full, CancellationToken cancellationToken = default);
        Task StartFullScanAsync(CancellationToken cancellationToken = default);
        void CancelScan();
    }
}