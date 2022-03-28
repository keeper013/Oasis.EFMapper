namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

/// <summary>
/// Just in case user want to easily catch all exceptions thrown from this assembly.
/// </summary>
public abstract class EfCoreMapperException : Exception
{
    protected EfCoreMapperException(string message)
        : base(message)
    {
    }

    protected EfCoreMapperException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
