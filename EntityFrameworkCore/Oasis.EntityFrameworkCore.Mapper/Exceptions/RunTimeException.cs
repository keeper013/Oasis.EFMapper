namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class UnregisteredMappingException : EfCoreMapperException
{
    public UnregisteredMappingException(Type sourceType, Type targetType)
        : base($"Mapping from {sourceType.Name} to {targetType.Name} isn't registered.")
    {
    }
}

public sealed class DbContextMissingException : EfCoreMapperException
{
    public DbContextMissingException()
        : base($"Database context is not set.")
    {
    }
}