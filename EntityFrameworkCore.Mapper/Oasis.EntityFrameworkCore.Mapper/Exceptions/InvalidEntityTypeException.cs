namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class InvalidEntityTypeException : EfCoreMapperException
{
    public InvalidEntityTypeException(Type type)
        : base($"Type {type} is invalid to be registered as an entity type.")
    {
    }
}