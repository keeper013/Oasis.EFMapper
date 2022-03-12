namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public class NonStaticScalarMapperException : Exception
{
    private readonly Type _sourceType;
    private readonly Type _targetType;

    public NonStaticScalarMapperException(Type sourceType, Type targetType)
    {
        _sourceType = sourceType;
        _targetType = targetType;
    }

    public override string Message => $"Any scalar mapper (specifically from {_sourceType} to {_targetType}) must be static.";
}
