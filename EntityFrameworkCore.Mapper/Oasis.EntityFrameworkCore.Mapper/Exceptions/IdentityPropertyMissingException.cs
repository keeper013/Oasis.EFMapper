namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class IdentityPropertyMissingException : EfCoreMapperException
{
    private readonly Type _sourceType;
    private readonly Type? _targetType;

    public IdentityPropertyMissingException(Type type)
    {
        _sourceType = type;
        _targetType = default;
    }

    public IdentityPropertyMissingException(Type sourceType, Type targetType)
    {
        _sourceType = sourceType;
        _targetType = targetType;
    }

    public override string Message => $"{(_targetType == default ? $"Type {_sourceType}" : $"Either type {_sourceType} or {_targetType}")} doesn't have a proper property for identity.";
}
