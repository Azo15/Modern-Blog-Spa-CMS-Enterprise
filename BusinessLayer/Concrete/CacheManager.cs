using BusinessLayer.Abstract;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BusinessLayer.Concrete
{
    public class CacheManager : ICacheService
    {
        private readonly IDistributedCache _distributedCache;

        public CacheManager(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var cachedData = await _distributedCache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedData))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedData);
        }

        public async Task SetAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            });
            await _distributedCache.SetStringAsync(key, jsonData, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _distributedCache.RemoveAsync(key);
        }
    }
}
