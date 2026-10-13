using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Concrete;

// Dedicated, bounded store: registering Redis must not overwrite the memory fallback.
public sealed class CacheFallbackStore
{
    public IDistributedCache Cache { get; } = new MemoryDistributedCache(
        Options.Create(new MemoryDistributedCacheOptions { SizeLimit = 50 * 1024 * 1024 }));
}
