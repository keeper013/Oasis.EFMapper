namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class ScalarMapperExistsException : EfCoreMapperException
{
    public ScalarMapperExistsException(Type sourceType, Type targetType)
        : base($"Scalar type mapper from {sourceType} to {targetType} has been registered.")
    {
    }
}
