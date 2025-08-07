using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MyDataHelper.Services
{
    public class SystemTrayService
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);
        
        private NotifyIcon? _trayIcon;
        private ContextMenuStrip? _contextMenu;
        private readonly IServiceProvider _serviceProvider;
        
        public SystemTrayService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public void Initialize()
        {
            try
            {
                Logger.Info("Initializing system tray service");
                
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
                Visible = true
                // Don't assign ContextMenuStrip directly - we'll handle clicks manually
            };
            
            // Set icon - create a simple one if no valid icon file exists
            _trayIcon.Icon = GetOrCreateIcon();
            
            // Handle both left and right clicks
            _trayIcon.MouseClick += TrayIcon_MouseClick;
            _trayIcon.DoubleClick += (s, e) => OpenApplication();
            
                // Don't show initial balloon - it will be shown after startup completes
                
                Logger.Info("System tray service initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to initialize system tray");
                StartupErrorLogger.LogError("Failed to initialize system tray", ex);
                throw; // Re-throw to ensure startup fails properly
            }
        }
        
        private void OpenApplication()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5250",
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
                    FileName = "http://localhost:5250/settings",
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
        
        public void ShowBalloonTip(string title, string text, ToolTipIcon icon)
        {
            _trayIcon?.ShowBalloonTip(3000, title, text, icon);
        }
        
        public void UpdateTooltip(string text)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Text = text.Length > 63 ? text.Substring(0, 60) + "..." : text;
            }
        }
        
        private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Show context menu on left click
                if (_contextMenu != null)
                {
                    // Use reflection to show the context menu at cursor position
                    var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", 
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    mi?.Invoke(_trayIcon, null);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Right click also shows the menu (default behavior)
                _trayIcon!.ContextMenuStrip = _contextMenu;
            }
        }
        
        private Icon GetOrCreateIcon()
        {
            try
            {
                // Try to load from file first
                var iconPaths = new[] {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyDataHelper.ico"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "favicon.ico")
                };
                
                foreach (var iconPath in iconPaths)
                {
                    if (File.Exists(iconPath))
                    {
                        try
                        {
                            return new Icon(iconPath);
                        }
                        catch
                        {
                            // File exists but is not a valid icon, continue
                        }
                    }
                }
                
                // Try to extract from executable
                try
                {
                    var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    if (exeIcon != null)
                        return exeIcon;
                }
                catch { }
                
                // Create a simple icon programmatically as last resort
                return CreateDefaultIcon();
            }
            catch
            {
                // Ultimate fallback
                return SystemIcons.Application;
            }
        }
        
        private Icon CreateDefaultIcon()
        {
            // Create a simple 16x16 icon programmatically
            using (var bmp = new Bitmap(16, 16))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    
                    // Draw a simple disk icon
                    using (var brush = new SolidBrush(Color.FromArgb(0, 120, 212)))
                    {
                        // Draw a cylinder shape (disk)
                        g.FillEllipse(brush, 2, 2, 12, 4);
                        g.FillRectangle(brush, 2, 4, 12, 8);
                        g.FillEllipse(brush, 2, 10, 12, 4);
                    }
                    
                    // Add highlight
                    using (var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1))
                    {
                        g.DrawEllipse(pen, 2, 2, 12, 4);
                    }
                }
                
                // Convert bitmap to icon
                IntPtr hIcon = bmp.GetHicon();
                Icon tempIcon = Icon.FromHandle(hIcon);
                Icon icon = (Icon)tempIcon.Clone(); // Clone to avoid disposal issues
                DestroyIcon(hIcon); // Clean up the handle
                return icon;
            }
        }
        
        public void Dispose()
        {
            _trayIcon?.Dispose();
            _contextMenu?.Dispose();
        }
    }
}