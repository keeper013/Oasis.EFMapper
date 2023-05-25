namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class ScalarMapperExistsException : EfMapperException
{
    public ScalarMapperExistsException(Type sourceType, Type targetType)
        : base($"Scalar type mapper from {sourceType} to {targetType} has been registered.")
    {
    }
}
