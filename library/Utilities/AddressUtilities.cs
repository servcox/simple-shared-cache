using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ServcoX.SimpleSharedCache.Utilities;

public static class AddressUtilities
{
    private const Char Separator = '/';
    private static readonly ConcurrentDictionary<Type, String> ModelIdCache = new();

    public static String Compute<TRecord>(String key)
    {
        if (String.IsNullOrEmpty(key)) throw new ArgumentException("Cannot be null or empty", nameof(key));
        if (key.Contains(Separator)) throw new ArgumentException($"Cannot contain '{Separator}'", nameof(key));

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
                .ToArray()
                .ToRfc7515CString();
            prefix = ModelIdCache[type] = $"{name}{Separator}{hash}{Separator}";
        }

        return prefix;
    }

    public static String ExtractKeyFromAddress(String address)
    {
        if (address is null) throw new ArgumentNullException(nameof(address));

        var pos = address.LastIndexOf(Separator);
        if (pos < 0) throw new ArgumentException($"Must contain '{Separator}'");

        // Recommendation not supported in .NET Standard 2.0
        // ReSharper disable ReplaceSubstringWithRangeIndexer 
        return address.Substring(pos + 1);
    }
}