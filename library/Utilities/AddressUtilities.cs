using System.Collections.Concurrent;
using System.Security.Cryptography;
using ServcoX.Rfc7515C;
using ServcoX.SimpleSharedCache.Exceptions;

namespace ServcoX.SimpleSharedCache.Utilities;

public static class AddressUtilities
{
    private const Int32 HashLength = 8;
    private static readonly ConcurrentDictionary<Type, String> ModelIdCache = new();

    public static String Compute<T>(String key) => Compute(typeof(T), key);

    public static String Compute(Type type, String key)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));

        if (key.Contains('/')) throw new ArgumentException("`key` cannot contain '/'", nameof(key));

        if (!ModelIdCache.TryGetValue(type, out var modelId))
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(type.GUID.ToByteArray())
                .Take(HashLength)
                .ToArray();
            modelId = ModelIdCache[type] = $"{type.FullName}/{Rfc7515CEncoder.Encode(hash)}";
        }

        return $"{modelId}/{key}";
    }
}