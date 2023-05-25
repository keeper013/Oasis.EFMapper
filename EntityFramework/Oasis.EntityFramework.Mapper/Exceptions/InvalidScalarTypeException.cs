namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class InvalidScalarTypeException : EfMapperException
{
    public InvalidScalarTypeException(Type type)
        : base($"Type {type} is invalid for a scalar type.")
    {
    }
}