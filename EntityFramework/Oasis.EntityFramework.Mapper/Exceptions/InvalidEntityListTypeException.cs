namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class InvalidEntityListTypeException : EfCoreMapperException
{
    public InvalidEntityListTypeException(Type type)
        : base($"Type {type} is invalid for an entity list type.")
    {
    }
}