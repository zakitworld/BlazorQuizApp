using Microsoft.Extensions.Caching.Memory;

namespace BlazorQuizApp.Services
{
    /// <summary>
    /// Service for caching frequently accessed data to improve performance
    /// </summary>
    public interface ICacheService
    {
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class;
        void Remove(string key);
        void Clear();
    }

    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class
        {
            if (_cache.TryGetValue(key, out T? cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}, fetching from source", key);
            var item = await getItem();

            if (item != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(key, item, cacheOptions);
            }

            return item;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed cache entry for key: {Key}", key);
        }

        public void Clear()
        {
            if (_cache is MemoryCache memoryCache)
            {
                // Note: IMemoryCache doesn't have a direct Clear method
                // In production, you might want to track keys or use a different cache implementation
                _logger.LogWarning("Clear() called but IMemoryCache doesn't support clearing all entries");
            }
        }
    }
}

