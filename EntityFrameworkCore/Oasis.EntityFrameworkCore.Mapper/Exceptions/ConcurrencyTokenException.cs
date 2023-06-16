namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class ConcurrencyTokenException : EfCoreMapperException
{
    public ConcurrencyTokenException(Type sourceType, Type targetType, object id)
        : base($"Concurrency token mismatch when mapping from ${sourceType.Name} to ${targetType.Name} of Id {id}.")
    {
    }
}
