namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class KeyPropertyMissingException : EfCoreMapperException
{
    public KeyPropertyMissingException(Type type, string propertyName)
        : base($"Type {type.Name} doesn't have a proper property for {propertyName}.")
    {
    }

    public KeyPropertyMissingException(Type sourceType, Type targetType, string propertyName)
        : base($"Either type {sourceType.Name} or {targetType.Name} doesn't have a proper property for {propertyName}.")
    {
    }
}

public sealed class UnregisteredMappingException : EfCoreMapperException
{
    public UnregisteredMappingException(Type sourceType, Type targetType)
        : base($"Mapping from {sourceType.Name} to {targetType.Name} isn't registered.")
    {
    }
}