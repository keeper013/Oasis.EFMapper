namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class MapperMissingException : EfCoreMapperException
{
    public MapperMissingException(Type sourceType, Type targetType)
        : base($"Entity mapper from type {sourceType} to {targetType} hasn't been registered yet.")
    {
    }
}
