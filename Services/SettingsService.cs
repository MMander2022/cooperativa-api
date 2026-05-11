using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data;
using CooperativaApp.Data;
namespace CooperativaApp.Services
{
    public interface ISettingsService
    {
        Task<string> GetSettingAsync(string key);
        Task<bool> UpdateSettingAsync(string key, string value, int userId);
        Task ClearCache(); // Para cuando actualicemos un valor
    }

    public class SettingsService : ISettingsService
    {
        private readonly CooperativaContext _context;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "GlobalSettingsCache";

        public SettingsService(CooperativaContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<string> GetSettingAsync(string key)
        {
            // 🛡️ Intentamos obtener el diccionario completo del caché
            if (!_cache.TryGetValue(CacheKey, out Dictionary<string, string> settings))
            {
                // Si no está en caché, vamos a SQL
                settings = await _context.GlobalSettings
                    .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

                // Guardamos en caché por 24 horas (o hasta que se limpie manualmente)
                _cache.Set(CacheKey, settings, TimeSpan.FromHours(24));
            }

            return settings.TryGetValue(key, out var value) ? value : null;
        }

        public async Task<bool> UpdateSettingAsync(string key, string value, int userId)
        {
            var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
            if (setting == null) return false;

            setting.SettingValue = value;
            setting.UpdatedBy = userId;
            setting.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            await ClearCache(); // ⚡ ¡Vital! Limpiamos caché para que el cambio se vea al instante
            return true;
        }

        public async Task ClearCache() => _cache.Remove(CacheKey);
    }
}
