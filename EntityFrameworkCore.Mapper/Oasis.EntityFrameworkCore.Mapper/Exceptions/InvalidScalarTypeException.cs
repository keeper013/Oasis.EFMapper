namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class InvalidScalarTypeException : EfCoreMapperException
{
    public InvalidScalarTypeException(Type type)
        : base($"Type {type} is invalid for a scalar type.")
    {
    }
}