namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class ScalarConverterMissingException : EfMapperException
{
    public ScalarConverterMissingException(Type sourceType, Type targetType)
        : base($"Scalar converter from {sourceType} to {targetType} doesn't exist.")
    {
    }
}
