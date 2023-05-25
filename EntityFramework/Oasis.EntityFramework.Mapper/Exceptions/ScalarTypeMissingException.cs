namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class ScalarTypeMissingException : EfMapperException
{
    public ScalarTypeMissingException(Type sourceType, Type targetType)
        : base($"At list one of {sourceType} and {targetType} should be a scalar type.")
    {
    }
}
