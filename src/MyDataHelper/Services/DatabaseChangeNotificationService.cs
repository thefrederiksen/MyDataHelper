using System;

namespace MyDataHelper.Services
{
    public class DatabaseChangeNotificationService : IDatabaseChangeNotificationService
    {
        public event EventHandler<DatabaseChangeEventArgs>? DatabaseChanged;
        
        public void NotifyChange(string tableName, ChangeType changeType)
        {
            DatabaseChanged?.Invoke(this, new DatabaseChangeEventArgs
            {
                TableName = tableName,
                ChangeType = changeType,
                Timestamp = DateTime.UtcNow
            });
            
            Logger.Debug($"Database change notified: {tableName} - {changeType}");
        }
        
        public void NotifyBulkChange(string tableName)
        {
            NotifyChange(tableName, ChangeType.BulkOperation);
        }
    }
}