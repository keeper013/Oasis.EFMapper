namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public class MapperMissingException : Exception
{
    private readonly Type _sourceType;
    private readonly Type _targetType;

    public MapperMissingException(Type sourceType, Type targetType)
    {
        _sourceType = sourceType;
        _targetType = targetType;
    }

    public override string Message => $"Entity mapper from type {_sourceType} to {_targetType} hasn't been registered yet.";
}
