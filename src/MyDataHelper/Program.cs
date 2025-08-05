using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyDataHelper.Forms;
using MyDataHelper.Services;
using System.Diagnostics;
using AutoUpdaterDotNET;
using System.Linq;
using System.IO;

namespace MyDataHelper
{
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {
            // Set up global exception handlers FIRST
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            Application.ThreadException += HandleThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            try
            {
                StartupErrorLogger.LogError("Application starting", null);

                // Check for single instance
                const string appGuid = "MyDataHelper-{7A5B9AC4-C8E2-46fd-B9CF-83F05E7CDE9F}";
                using (Mutex mutex = new Mutex(true, appGuid, out bool createdNew))
                {
                    if (!createdNew)
                    {
                        // Application is already running - open browser and exit
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "http://localhost:5113",
                                UseShellExecute = true
                            });
                        }
                        catch
                        {
                            // If browser fails to open, at least show a message
                            MessageBox.Show("MyDataHelper is already running. Please check your system tray.", 
                                "MyDataHelper", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        return;
                    }
                    
                    // First instance - continue with normal startup
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    
                    // Check for updates first (blocking - app won't start if update is available)
                    InitializeAutoUpdater();
                    
                    StartupErrorLogger.LogError("Starting Blazor server", null);
                    
                    // Start the Blazor server with a progress window
                    var starter = new BlazorServerStarter();
                    starter.Start(args);
                    
                    // Keep mutex alive for the lifetime of the application
                    GC.KeepAlive(mutex);
                }
            }
            catch (Exception ex)
            {
                StartupErrorLogger.LogError("Fatal error during startup", ex);
                ShowStartupError("A fatal error occurred during application startup.", ex);
            }
        }

        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            StartupErrorLogger.LogError("Unhandled exception in AppDomain", ex);
            
            if (e.IsTerminating)
            {
                ShowStartupError("An unhandled error occurred and the application must close.", ex);
            }
        }

        private static void HandleThreadException(object sender, ThreadExceptionEventArgs e)
        {
            StartupErrorLogger.LogError("Unhandled thread exception", e.Exception);
            ShowStartupError("An unhandled error occurred in the application.", e.Exception);
        }

        private static void ShowStartupError(string message, Exception? exception)
        {
            try
            {
                using (var errorForm = new StartupErrorForm(message, exception))
                {
                    errorForm.ShowDialog();
                }
            }
            catch
            {
                // If even the error form fails, show basic message box
                var basicMessage = $"{message}\n\nError: {exception?.Message ?? "Unknown error"}\n\nLog file: {StartupErrorLogger.GetLogPath()}";
                MessageBox.Show(basicMessage, "MyDataHelper - Startup Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            Environment.Exit(1);
        }



        private static void InitializeAutoUpdater()
        {
            try
            {
                // Configure AutoUpdater - silent mode unless update is available
                AutoUpdater.RunUpdateAsAdmin = false;
                AutoUpdater.Synchronous = true; // Wait for update check to complete
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.ShowRemindLaterButton = true;
                AutoUpdater.Mandatory = false;
                AutoUpdater.UpdateMode = Mode.Normal; // Show UI only when update is available
                AutoUpdater.ReportErrors = false; // Don't show "no update available" messages
                
                // TODO: Update this URL to point to MyDataHelper update.xml
                // AutoUpdater.Start("https://raw.githubusercontent.com/yourusername/MyDataHelper/main/update.xml");
                
                // Check if we should minimize (started from Windows startup)
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1 && args[1] == "--minimized")
                {
                    // When started from Windows startup, check updates silently
                    AutoUpdater.UpdateMode = Mode.ForcedDownload;
                    AutoUpdater.ShowSkipButton = false;
                    AutoUpdater.ShowRemindLaterButton = false;
                }
            }
            catch (Exception ex)
            {
                // Don't crash the app if update check fails
                StartupErrorLogger.LogError("AutoUpdater initialization failed", ex);
            }
        }

    }
}