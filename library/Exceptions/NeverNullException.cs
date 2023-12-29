namespace ServcoX.SimpleSharedCache.Exceptions;

public class NeverNullException : Exception
{
    public NeverNullException()
    {
    }

    public NeverNullException(String message) : base(message)
    {
    }

    public NeverNullException(String message, Exception innerException) : base(message, innerException)
    {
    }
}