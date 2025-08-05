using System;

namespace MyDataHelper.Services
{
    public interface IDatabaseChangeNotificationService
    {
        event EventHandler<DatabaseChangeEventArgs>? DatabaseChanged;
        
        void NotifyChange(string tableName, ChangeType changeType);
        void NotifyBulkChange(string tableName);
    }
    
    public enum ChangeType
    {
        Insert,
        Update,
        Delete,
        BulkOperation
    }
    
    public class DatabaseChangeEventArgs : EventArgs
    {
        public string TableName { get; set; } = string.Empty;
        public ChangeType ChangeType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}