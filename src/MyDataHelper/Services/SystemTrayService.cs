using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace MyDataHelper.Services
{
    public class SystemTrayService
    {
        private NotifyIcon? _trayIcon;
        private ContextMenuStrip? _contextMenu;
        private readonly IServiceProvider _serviceProvider;
        
        public SystemTrayService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public void Initialize()
        {
            // Create context menu
            _contextMenu = new ContextMenuStrip();
            
            var openMenuItem = new ToolStripMenuItem("Open MyDataHelper");
            openMenuItem.Font = new Font(openMenuItem.Font, FontStyle.Bold);
            openMenuItem.Click += (s, e) => OpenApplication();
            
            var scanMenuItem = new ToolStripMenuItem("Start Full Scan");
            scanMenuItem.Click += (s, e) => StartFullScan();
            
            _contextMenu.Items.Add(openMenuItem);
            _contextMenu.Items.Add(scanMenuItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            var settingsMenuItem = new ToolStripMenuItem("Settings");
            settingsMenuItem.Click += (s, e) => OpenSettings();
            
            var aboutMenuItem = new ToolStripMenuItem("About");
            aboutMenuItem.Click += (s, e) => ShowAbout();
            
            _contextMenu.Items.Add(settingsMenuItem);
            _contextMenu.Items.Add(aboutMenuItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            var exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += (s, e) => ExitApplication();
            
            _contextMenu.Items.Add(exitMenuItem);
            
            // Create tray icon
            _trayIcon = new NotifyIcon
            {
                Text = "MyDataHelper - Disk Space Analyzer",
                Visible = true,
                ContextMenuStrip = _contextMenu
            };
            
            // Try to load icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyDataHelper.ico");
                if (File.Exists(iconPath))
                {
                    _trayIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    // Use default application icon
                    _trayIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _trayIcon.Icon = SystemIcons.Application;
            }
            
            _trayIcon.DoubleClick += (s, e) => OpenApplication();
            
            // Show initial balloon
            _trayIcon.ShowBalloonTip(
                3000,
                "MyDataHelper",
                "MyDataHelper is running in the background",
                ToolTipIcon.Info);
        }
        
        private void OpenApplication()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5113",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private async void StartFullScan()
        {
            try
            {
                var scanService = _serviceProvider.GetService(typeof(IDiskScanService)) as IDiskScanService;
                if (scanService != null && !scanService.IsScanning)
                {
                    _trayIcon?.ShowBalloonTip(
                        3000,
                        "MyDataHelper",
                        "Starting full disk scan...",
                        ToolTipIcon.Info);
                        
                    await scanService.StartFullScanAsync();
                }
                else
                {
                    _trayIcon?.ShowBalloonTip(
                        3000,
                        "MyDataHelper",
                        "A scan is already in progress",
                        ToolTipIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to start scan from tray");
                MessageBox.Show($"Failed to start scan: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void OpenSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5113/settings",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open settings: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ShowAbout()
        {
            var message = "MyDataHelper - Disk Space Analyzer\n\n" +
                          "Version 1.0.0\n\n" +
                          "A powerful disk space analysis tool inspired by WinDirStat.\n\n" +
                          "© 2025 MyDataHelper";
                          
            MessageBox.Show(message, "About MyDataHelper", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void ExitApplication()
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit MyDataHelper?", 
                "Confirm Exit", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                Dispose();
                Application.Exit();
            }
        }
        
        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _trayIcon?.ShowBalloonTip(3000, title, message, icon);
        }
        
        public void UpdateTooltip(string text)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Text = text.Length > 63 ? text.Substring(0, 60) + "..." : text;
            }
        }
        
        public void Dispose()
        {
            _trayIcon?.Dispose();
            _contextMenu?.Dispose();
        }
    }
}