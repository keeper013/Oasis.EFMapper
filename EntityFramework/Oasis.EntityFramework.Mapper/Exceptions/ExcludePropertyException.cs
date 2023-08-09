namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class KeyTypeExcludedException : EfMapperException
{
    public KeyTypeExcludedException(Type sourceType, Type targetType, string propertyName)
        : base($"Property {propertyName} is identity/concurrency token that can't be excluded when mapping from type {sourceType} to {targetType}.")
    {
    }
}

public sealed class CustomMappingPropertyExcludedException : EfMapperException
{
    public CustomMappingPropertyExcludedException(Type sourceType, Type targetType, string propertyName)
        : base($"Property {propertyName} is customly mapped that can't be excluded when mapping from type {sourceType} to {targetType}.")
    {
    }
}

public sealed class UselessExcludeException : EfMapperException
{
    public UselessExcludeException(Type type, string propertyName)
        : base($"Type {type.Name} doesn't have a valid property named {propertyName} to be excluded.")
    {
    }

    public UselessExcludeException(Type sourceType, Type targetType, string propertyName)
        : base($"When mapping from {sourceType} to {targetType}, there isn't a valid property named {propertyName} to be mapped.")
    {
    }
}

public sealed class InvalidDependentException : EfMapperException
{
    public InvalidDependentException(Type type, string propertyName)
        : base($"Type {type.Name} doesn't have a valid property of class type of list of class type named {propertyName} to be a dependent property.")
    {
    }
}