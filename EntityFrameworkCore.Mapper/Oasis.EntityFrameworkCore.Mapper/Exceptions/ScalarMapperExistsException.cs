namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public class ScalarMapperExistsException : Exception
{
    private readonly Type _sourceType;
    private readonly Type _targetType;

    public ScalarMapperExistsException(Type sourceType, Type targetType)
    {
        _sourceType = sourceType;
        _targetType = targetType;
    }

    public override string Message => $"Scalar type mapper from {_sourceType} to {_targetType} has been registered.";
}
