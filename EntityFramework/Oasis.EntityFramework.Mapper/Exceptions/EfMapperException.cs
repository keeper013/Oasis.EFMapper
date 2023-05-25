namespace Oasis.EntityFramework.Mapper.Exceptions;

/// <summary>
/// Just in case user want to easily catch all exceptions thrown from this assembly.
/// </summary>
public abstract class EfMapperException : Exception
{
    protected EfMapperException(string message)
        : base(message)
    {
    }

    protected EfMapperException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
