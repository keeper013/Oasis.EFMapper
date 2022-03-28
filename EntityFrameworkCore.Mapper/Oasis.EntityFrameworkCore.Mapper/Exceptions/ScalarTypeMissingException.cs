namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class ScalarTypeMissingException : EfCoreMapperException
{
    public ScalarTypeMissingException(Type sourceType, Type targetType)
        : base($"At list one of {sourceType} and {targetType} should be a scalar type.")
    {
    }
}
