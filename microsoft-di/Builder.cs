using Microsoft.Extensions.DependencyInjection;

namespace ServcoX.SimpleSharedCache.DependencyInjection;

public static class Builder
{
    public static IServiceCollection AddSimpleSharedCache(this IServiceCollection target, String connectionString, Action<Configuration>? configure = null)
    {
        target.AddSingleton<ISimpleSharedCacheClient>(new SimpleSharedCacheClient(connectionString, configure));
        return target;
    }
}