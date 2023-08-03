namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class InvalidEntityTypeException : EfMapperException
{
    public InvalidEntityTypeException(Type type)
        : base($"Type {type.Name} is invalid for an entity type.")
    {
    }
}