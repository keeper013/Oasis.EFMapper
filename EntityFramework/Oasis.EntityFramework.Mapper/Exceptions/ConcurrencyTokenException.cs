namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class ConcurrencyTokenException : EfMapperException
{
    public ConcurrencyTokenException(Type sourceType, Type targetType, object id)
        : base($"Concurrency token mismatch when mapping from ${sourceType.Name} to ${targetType.Name} of Id {id}.")
    {
    }
}
