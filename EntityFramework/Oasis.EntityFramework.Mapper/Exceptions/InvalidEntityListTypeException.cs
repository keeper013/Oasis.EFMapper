namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class InvalidEntityListTypeException : EfMapperException
{
    public InvalidEntityListTypeException(Type type)
        : base($"Type {type} is invalid for an entity list type.")
    {
    }
}