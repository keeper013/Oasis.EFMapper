namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public class ScalarTypeMissingException : Exception
{
    private readonly Type _sourceType;
    private readonly Type _targetType;

    public ScalarTypeMissingException(Type sourceType, Type targetType)
    {
        _sourceType = sourceType;
        _targetType = targetType;
    }

    public override string Message => $"At list one of {_sourceType} and {_targetType} should be a scalar type.";
}
