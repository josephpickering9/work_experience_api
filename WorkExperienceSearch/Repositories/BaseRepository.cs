using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Work_Experience_Search.Repositories;

public abstract class BaseRepository(IMemoryCache cache)
{
    protected void SetCache<T>(string key, T value, IChangeToken changeToken) =>
        cache.Set(key, value, new MemoryCacheEntryOptions().AddExpirationToken(changeToken));

    protected bool TryGetCache<T>(string key, out T? value) =>
        cache.TryGetValue(key, out value);
}
