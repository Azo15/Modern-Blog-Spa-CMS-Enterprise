using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using BusinessLayer.Abstract;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BusinessLayer.Concrete;

public sealed class CacheManager(IDistributedCache primary, CacheFallbackStore fallback, ILogger<CacheManager> logger) : ICacheService
{
    private readonly ConcurrentDictionary<string, PendingMutation> pending = new();
    private readonly SemaphoreSlim recovery = new(1, 1);
    private long unavailableUntil;
    private sealed record PendingMutation(byte[]? Value);
    private static readonly DistributedCacheEntryOptions VersionOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) };
    private static bool CacheFailure(Exception ex) => ex is RedisException or TimeoutException or System.IO.IOException or System.Net.Sockets.SocketException;

    private async Task<(bool Success, T? Value)> TryPrimary<T>(Func<Task<T>> operation)
    {
        if (DateTime.UtcNow.Ticks < Interlocked.Read(ref unavailableUntil)) return (false, default);
        try
        {
            await recovery.WaitAsync();
            try
            {
                foreach (var mutation in pending.ToArray())
                {
                    if (mutation.Value.Value is null) await primary.RemoveAsync(mutation.Key);
                    else await primary.SetAsync(mutation.Key, mutation.Value.Value, VersionOptions);
                    ((ICollection<KeyValuePair<string, PendingMutation>>)pending).Remove(mutation);
                }
            }
            finally { recovery.Release(); }
            return (true, await operation());
        }
        catch (Exception ex) when (CacheFailure(ex))
        {
            Interlocked.Exchange(ref unavailableUntil, DateTime.UtcNow.AddSeconds(5).Ticks);
            // Log the type, not Redis configuration/credentials contained in exception details.
            logger.LogWarning("Distributed cache unavailable ({ExceptionType}); using memory fallback.", ex.GetType().Name);
            return (false, default);
        }
    }

    private async Task<byte[]?> ReadRaw(string key)
    {
        var result = await TryPrimary(() => primary.GetAsync(key));
        if (!result.Success) return await fallback.Cache.GetAsync(key);
        // Never resurrect an old fallback value when Redis is reachable and returns a miss.
        if (result.Value is null) await fallback.Cache.RemoveAsync(key);
        return result.Value;
    }

    private async Task<string> VersionedKey(string key)
    {
        var prefix = key.Split(':', 2)[0];
        var version = await ReadRaw("__version:" + prefix);
        return prefix + ":v:" + (version is null ? "0" : Encoding.UTF8.GetString(version)) + ":" + key;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var bytes = await ReadRaw(await VersionedKey(key));
        if (bytes is null) return default;
        try { return JsonSerializer.Deserialize<T>(bytes); }
        catch (JsonException) { await RemoveAsync(key); return default; }
    }

    public async Task SetAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null)
    {
        var actualKey = await VersionedKey(key);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(data);
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? TimeSpan.FromMinutes(10) };
        await fallback.Cache.SetAsync(actualKey, bytes, options);
        await TryPrimary(async () => { await primary.SetAsync(actualKey, bytes, options); return true; });
    }

    public async Task RemoveAsync(string key)
    {
        var actualKey = await VersionedKey(key);
        await fallback.Cache.RemoveAsync(actualKey);
        pending[actualKey] = new PendingMutation(null);
        await TryPrimary(async () => { await primary.RemoveAsync(actualKey); return true; });
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Contains(':')) throw new ArgumentException("Use an exact logical cache namespace.", nameof(prefix));
        var key = "__version:" + prefix;
        var version = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N"));
        await fallback.Cache.SetAsync(key, version, VersionOptions);
        pending[key] = new PendingMutation(version);
        await TryPrimary(async () => { await primary.SetAsync(key, version, VersionOptions); return true; });
        // Old entries are unreachable and expire by TTL; no Redis KEYS/SCAN or per-process key registry.
    }
}
