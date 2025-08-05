using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface ISettingsService
    {
        Task<T?> GetSettingAsync<T>(string key);
        Task SetSettingAsync<T>(string key, T value);
        Task<bool> GetBoolSettingAsync(string key, bool defaultValue = false);
        Task<int> GetIntSettingAsync(string key, int defaultValue = 0);
        Task<string> GetStringSettingAsync(string key, string defaultValue = "");
        Task<DateTime?> GetDateTimeSettingAsync(string key);
        Task RemoveSettingAsync(string key);
        Task<bool> SettingExistsAsync(string key);
    }
}