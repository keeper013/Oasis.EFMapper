namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class InvalidEntityListTypeException : EfCoreMapperException
{
    public InvalidEntityListTypeException(Type type)
        : base($"Type {type.Name} is invalid for an entity list type.")
    {
    }
}