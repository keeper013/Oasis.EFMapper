namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class ScalarConverterMissingException : EfCoreMapperException
{
    public ScalarConverterMissingException(Type sourceType, Type targetType)
        : base($"Scalar converter from {sourceType} to {targetType} doesn't exist.")
    {
    }
}
