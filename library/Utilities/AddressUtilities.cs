using System.Collections.Concurrent;
using System.Security.Cryptography;
using ServcoX.Rfc7515C;

namespace ServcoX.SimpleSharedCache.Utilities;

public static class AddressUtilities
{
    private const Int32 HashLength = 8;
    private const Char Separator = '/';
    private static readonly ConcurrentDictionary<Type, String> ModelIdCache = new();

    public static String Compute<TRecord>(String key)
    {
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));
        if (key.Contains('/')) throw new ArgumentException("`key` cannot contain '/'", nameof(key));

        var prefix = ComputeModelPrefix<TRecord>();

        return $"{prefix}{key}";
    }

    public static String ComputeModelPrefix<TRecord>()
    {
        var type = typeof(TRecord);
        if (!ModelIdCache.TryGetValue(type, out var prefix))
        {
            var name = type.FullName;
            using var sha = SHA256.Create();
            //  Recommendation not supported in .NET Standard 2
#pragma warning disable CA1850
            var hash = sha.ComputeHash(type.GUID.ToByteArray())
#pragma warning restore CA1850
                .Take(HashLength)
                .ToArray()
                .ToRfc7515CString();
            prefix = ModelIdCache[type] = $"{name}{Separator}{hash}{Separator}";
        }

        return prefix;
    }
}