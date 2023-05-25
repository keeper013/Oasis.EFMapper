namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class InvalidEntityTypeException : EfMapperException
{
    public InvalidEntityTypeException(Type type)
        : base($"Type {type} is invalid for an entity type.")
    {
    }
}