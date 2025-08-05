using System;
using System.IO;
using System.Text;

namespace MyDataHelper.Services
{
    public static class StartupErrorLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyDataHelper",
            "startup_errors.log");
            
        static StartupErrorLogger()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            }
            catch { /* Ignore if we can't create directory */ }
        }
        
        public static void LogError(string message, Exception? exception)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{timestamp}] {message}");
                
                if (exception != null)
                {
                    logEntry.AppendLine($"Exception: {exception.GetType().Name}");
                    logEntry.AppendLine($"Message: {exception.Message}");
                    logEntry.AppendLine($"StackTrace: {exception.StackTrace}");
                    
                    if (exception.InnerException != null)
                    {
                        logEntry.AppendLine($"Inner Exception: {exception.InnerException.GetType().Name}");
                        logEntry.AppendLine($"Inner Message: {exception.InnerException.Message}");
                        logEntry.AppendLine($"Inner StackTrace: {exception.InnerException.StackTrace}");
                    }
                }
                
                logEntry.AppendLine(new string('-', 80));
                
                File.AppendAllText(LogPath, logEntry.ToString());
            }
            catch { /* If we can't log, don't crash */ }
        }
        
        public static string GetLogPath() => LogPath;
        
        public static void ClearLog()
        {
            try
            {
                if (File.Exists(LogPath))
                {
                    File.Delete(LogPath);
                }
            }
            catch { /* Ignore errors */ }
        }
    }
}