namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class IdentityPropertyMissingException : EfCoreMapperException
{
    public IdentityPropertyMissingException(Type type)
        : base($"Type {type} doesn't have a proper property for identity.")
    {
    }

    public IdentityPropertyMissingException(Type sourceType, Type targetType)
        : base($"Either type {sourceType} or {targetType} doesn't have a proper property for identity.")
    {
    }
}