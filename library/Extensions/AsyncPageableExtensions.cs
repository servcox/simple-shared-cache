using Azure;

namespace ServcoX.SimpleSharedCache.Extensions;

public static class AsyncPageableExtensions
{
    public static async Task<List<T>> ToList<T>(this AsyncPageable<T> target) where T : notnull
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        
        var output = new List<T>();
        await foreach (var item in target) output.Add(item);
        return output;
    }
}