using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyDataHelper.Data;
using MyDataHelper.Models;

namespace MyDataHelper.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IDbContextFactory<MyDataHelperDbContext> _contextFactory;
        
        public SettingsService(IDbContextFactory<MyDataHelperDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        
        public async Task<T?> GetSettingAsync<T>(string key)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var setting = await context.tbl_app_settings
                .FirstOrDefaultAsync(s => s.setting_key == key);
                
            if (setting == null || string.IsNullOrEmpty(setting.setting_value))
                return default;
                
            try
            {
                if (setting.data_type == "json")
                {
                    return JsonSerializer.Deserialize<T>(setting.setting_value);
                }
                else
                {
                    return (T)Convert.ChangeType(setting.setting_value, typeof(T));
                }
            }
            catch
            {
                return default;
            }
        }
        
        public async Task SetSettingAsync<T>(string key, T value)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var setting = await context.tbl_app_settings
                .FirstOrDefaultAsync(s => s.setting_key == key);
                
            string stringValue;
            string dataType;
            
            if (value is bool)
            {
                stringValue = value.ToString()!.ToLower();
                dataType = "bool";
            }
            else if (value is int || value is long)
            {
                stringValue = value.ToString()!;
                dataType = "int";
            }
            else if (value is string)
            {
                stringValue = value.ToString()!;
                dataType = "string";
            }
            else if (value is DateTime)
            {
                stringValue = ((DateTime)(object)value).ToString("O");
                dataType = "datetime";
            }
            else
            {
                stringValue = JsonSerializer.Serialize(value);
                dataType = "json";
            }
            
            if (setting == null)
            {
                setting = new tbl_app_settings
                {
                    setting_key = key,
                    setting_value = stringValue,
                    data_type = dataType,
                    last_modified = DateTime.UtcNow
                };
                context.tbl_app_settings.Add(setting);
            }
            else
            {
                setting.setting_value = stringValue;
                setting.data_type = dataType;
                setting.last_modified = DateTime.UtcNow;
            }
            
            await context.SaveChangesAsync();
        }
        
        public async Task<bool> GetBoolSettingAsync(string key, bool defaultValue = false)
        {
            var value = await GetSettingAsync<string>(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }
        
        public async Task<int> GetIntSettingAsync(string key, int defaultValue = 0)
        {
            var value = await GetSettingAsync<string>(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
        
        public async Task<string> GetStringSettingAsync(string key, string defaultValue = "")
        {
            return await GetSettingAsync<string>(key) ?? defaultValue;
        }
        
        public async Task<DateTime?> GetDateTimeSettingAsync(string key)
        {
            var value = await GetSettingAsync<string>(key);
            if (string.IsNullOrEmpty(value))
                return null;
                
            return DateTime.TryParse(value, out var result) ? result : null;
        }
        
        public async Task RemoveSettingAsync(string key)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var setting = await context.tbl_app_settings
                .FirstOrDefaultAsync(s => s.setting_key == key);
                
            if (setting != null)
            {
                context.tbl_app_settings.Remove(setting);
                await context.SaveChangesAsync();
            }
        }
        
        public async Task<bool> SettingExistsAsync(string key)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.tbl_app_settings
                .AnyAsync(s => s.setting_key == key);
        }
    }
}