using System.Collections.Concurrent;
using BusinessLayer.Concrete;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
namespace BusinessLayer.Tests;
public class CacheManagerTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now=DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow()=>Now;
    }
    private sealed class RedisDouble
    {
        public bool Down;
        public ConcurrentDictionary<string,byte[]> Data=new();
        public Mock<IDistributedCache> Mock=new();
        public RedisDouble()
        {
            Mock.Setup(c=>c.GetAsync(It.IsAny<string>(),It.IsAny<CancellationToken>())).Returns((string key,CancellationToken ct)=>
            {Check();return Task.FromResult(Data.TryGetValue(key,out var value)?value:null);});
            Mock.Setup(c=>c.SetAsync(It.IsAny<string>(),It.IsAny<byte[]>(),It.IsAny<DistributedCacheEntryOptions>(),It.IsAny<CancellationToken>())).Returns((string key,byte[] value,DistributedCacheEntryOptions options,CancellationToken ct)=>{Check();Data[key]=value;return Task.CompletedTask;});
            Mock.Setup(c=>c.RemoveAsync(It.IsAny<string>(),It.IsAny<CancellationToken>())).Returns((string key,CancellationToken ct)=>{Check();Data.TryRemove(key,out _);return Task.CompletedTask;});
        }
        private void Check(){if(Down)throw new RedisConnectionException(ConnectionFailureType.SocketFailure,"Test disconnect");}
    }
    [Fact]
    public async Task PrefixInvalidationWorksAcrossInstancesAndAllPagesWithoutAffectingOtherCategory()
    {
        var redis=new RedisDouble();var a=new CacheManager(redis.Mock.Object,new CacheFallbackStore(),NullLogger<CacheManager>.Instance);var b=new CacheManager(redis.Mock.Object,new CacheFallbackStore(),NullLogger<CacheManager>.Instance);
        await a.SetAsync("blogs_cat_1:p1","first");await a.SetAsync("blogs_cat_1:p2","second");await a.SetAsync("blogs_cat_10:p1","other");Assert.Equal("first",await b.GetAsync<string>("blogs_cat_1:p1"));
        await b.RemoveByPrefixAsync("blogs_cat_1");Assert.Null(await a.GetAsync<string>("blogs_cat_1:p1"));Assert.Null(await a.GetAsync<string>("blogs_cat_1:p2"));Assert.Equal("other",await a.GetAsync<string>("blogs_cat_10:p1"));
    }
    [Fact]
    public async Task RedisDownFallsBackToMemoryAndReplaysInvalidationOnRecovery()
    {
        var redis=new RedisDouble();var clock=new Clock();var cache=new CacheManager(redis.Mock.Object,new CacheFallbackStore(),NullLogger<CacheManager>.Instance,clock);
        await cache.SetAsync("blogs_all:p1","old");redis.Down=true;await cache.RemoveByPrefixAsync("blogs_all");await cache.SetAsync("blogs_all:p1","during outage");Assert.Equal("during outage",await cache.GetAsync<string>("blogs_all:p1"));
        redis.Down=false;clock.Now=clock.Now.AddSeconds(6);Assert.Null(await cache.GetAsync<string>("blogs_all:p1"));await cache.SetAsync("blogs_all:p1","fresh");Assert.Equal("fresh",await cache.GetAsync<string>("blogs_all:p1"));
    }
    [Fact]
    public async Task MissingRedisValueDoesNotResurrectOldLocalValue()
    {
        var redis=new RedisDouble();var cache=new CacheManager(redis.Mock.Object,new CacheFallbackStore(),NullLogger<CacheManager>.Instance);await cache.SetAsync("blogs_all:p1","cached");redis.Data.Clear();Assert.Null(await cache.GetAsync<string>("blogs_all:p1"));
    }
    [Fact]
    public async Task CorruptJsonIsACacheMissInsteadOfAnApplicationFailure()
    {
        var redis=new RedisDouble();var cache=new CacheManager(redis.Mock.Object,new CacheFallbackStore(),NullLogger<CacheManager>.Instance);await cache.SetAsync("blogs_all:p1","cached");var key=redis.Data.Keys.Single(k=>!k.StartsWith("__version:"));redis.Data[key]=System.Text.Encoding.UTF8.GetBytes("broken json");Assert.Null(await cache.GetAsync<string>("blogs_all:p1"));
    }
}
