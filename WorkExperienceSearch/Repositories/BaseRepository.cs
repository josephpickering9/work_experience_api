using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Repositories;

public abstract class BaseRepository(Database context, IMemoryCache cache)
{
    protected bool SupportsILike() =>
        context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    protected void SetCache<T>(string key, T value, IChangeToken changeToken) =>
        cache.Set(key, value, new MemoryCacheEntryOptions().AddExpirationToken(changeToken));

    protected bool TryGetCache<T>(string key, out T? value) =>
        cache.TryGetValue(key, out value);
}
