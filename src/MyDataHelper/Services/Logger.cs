using System;
using System.IO;
using System.Text;
using System.Threading;

namespace MyDataHelper.Services
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
    
    public static class Logger
    {
        private static string? _logDirectory;
        private static LogLevel _logLevel = LogLevel.Info;
        private static readonly object _lock = new object();
        private static StreamWriter? _logWriter;
        private static System.Threading.Timer? _flushTimer;
        
        public static void Initialize(string logDirectory, LogLevel logLevel)
        {
            _logDirectory = logDirectory;
            _logLevel = logLevel;
            
            Directory.CreateDirectory(logDirectory);
            
            var logFile = Path.Combine(logDirectory, $"mydatahelper_{DateTime.Now:yyyyMMdd}.log");
            
            try
            {
                _logWriter = new StreamWriter(logFile, append: true, Encoding.UTF8)
                {
                    AutoFlush = false
                };
                
                // Flush logs every 5 seconds
                _flushTimer = new System.Threading.Timer(_ => FlushLogs(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                
                Info("Logger initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }
        
        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }
        
        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }
        
        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }
        
        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }
        
        public static void LogException(Exception ex, string message)
        {
            var errorMessage = $"{message}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\nInner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
            }
            Error(errorMessage);
        }
        
        private static void Log(LogLevel level, string message)
        {
            if (level < _logLevel)
                return;
                
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";
            
            lock (_lock)
            {
                try
                {
                    _logWriter?.WriteLine(logEntry);
                    
                    // Also write to console in debug mode
                    #if DEBUG
                    Console.WriteLine(logEntry);
                    #endif
                }
                catch
                {
                    // If logging fails, don't crash the application
                }
            }
        }
        
        private static void FlushLogs()
        {
            lock (_lock)
            {
                try
                {
                    _logWriter?.Flush();
                }
                catch
                {
                    // Ignore flush errors
                }
            }
        }
        
        public static void Shutdown()
        {
            _flushTimer?.Dispose();
            
            lock (_lock)
            {
                _logWriter?.Flush();
                _logWriter?.Dispose();
                _logWriter = null;
            }
        }
    }
}