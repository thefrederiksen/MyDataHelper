using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface IDatabaseInitializationService
    {
        Task<bool> InitializeDatabaseAsync(string connectionString);
        Task<bool> IsDatabaseInitializedAsync(string connectionString);
        Task<int> GetDatabaseVersionAsync(string connectionString);
        Task<bool> UpgradeDatabaseAsync(string connectionString);
    }
}