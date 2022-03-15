namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public class ScalarConverterMissingException : Exception
{
    private readonly Type _sourceType;
    private readonly Type _targetType;

    public ScalarConverterMissingException(Type sourceType, Type targetType)
    {
        _sourceType = sourceType;
        _targetType = targetType;
    }

    public override string Message => $"Scalar converter from {_sourceType} to {_targetType} doesn't exist.";
}
