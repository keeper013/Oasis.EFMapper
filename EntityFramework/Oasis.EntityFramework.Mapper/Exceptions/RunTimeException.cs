namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class UnregisteredMappingException : EfMapperException
{
    public UnregisteredMappingException(Type sourceType, Type targetType)
        : base($"Mapping from {sourceType.Name} to {targetType.Name} isn't registered.")
    {
    }
}

public sealed class DbContextMissingException : EfMapperException
{
    public DbContextMissingException()
        : base($"Database context is not set.")
    {
    }
}