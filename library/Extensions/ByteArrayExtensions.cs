using ServcoX.Rfc7515C;

namespace ServcoX.SimpleSharedCache.Extensions;

public static class ByteArrayExtensions
{
    public static String ToBase64String(this Byte[] target) => Convert.ToBase64String(target);
    public static String ToRfc7515CString(this Byte[] target) => Rfc7515CEncoder.Encode(target);
}