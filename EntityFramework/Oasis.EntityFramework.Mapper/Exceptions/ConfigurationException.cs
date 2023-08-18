namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class RedundantConfigurationException : EfMapperException
{
    public RedundantConfigurationException(Type type)
        : base($"Type {type.Name} has been configurated already.")
    {
    }

    public RedundantConfigurationException(Type sourceType, Type targetType)
        : base($"Custom configuration from {sourceType.Name} to {targetType.Name} already exists.")
    {
    }
}

public sealed class EmptyConfigurationException : EfMapperException
{
    public EmptyConfigurationException(Type type)
        : base($"Empty configuration: At least 1 configuration item of {type.Name} should not be null.")
    {
    }
}

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

public sealed class KeepUnmatchedPropertyExcludedException : EfMapperException
{
    public KeepUnmatchedPropertyExcludedException(Type sourceType, Type targetType, string propertyName)
        : base($"Property {propertyName} is configured to be keep unmatched that can't be excluded when mapping from type {sourceType} to {targetType}.")
    {
    }

    public KeepUnmatchedPropertyExcludedException(Type type, string propertyName)
        : base($"Property {propertyName} is configured to be keep unmatched that can't be excluded for type {type}.")
    {
    }
}

public sealed class CustomMappingPropertyKeepUnmatchedException : EfMapperException
{
    public CustomMappingPropertyKeepUnmatchedException(Type sourceType, Type targetType, string propertyName)
        : base($"Property {propertyName} can't be configured to be both customly mappend and keep unmatched when mapping from type {sourceType} to {targetType}.")
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

public sealed class InvalidEntityListTypeException : EfMapperException
{
    public InvalidEntityListTypeException(Type type)
        : base($"Type {type.Name} is invalid for an entity list type.")
    {
    }
}

public sealed class InvalidEntityTypeException : EfMapperException
{
    public InvalidEntityTypeException(Type type)
        : base($"Type {type.Name} is invalid for an entity type.")
    {
    }
}

public sealed class InvalidFactoryMethodEntityTypeException : EfMapperException
{
    public InvalidFactoryMethodEntityTypeException(Type type)
        : base($"Type {type.Name} is invalid for an entity or entity list type.")
    {
    }
}

public sealed class ScalarTypeMissingException : EfMapperException
{
    public ScalarTypeMissingException(Type sourceType, Type targetType)
        : base($"At list one of {sourceType} and {targetType} should be a scalar type.")
    {
    }
}

public sealed class FactoryMethodExistsException : EfMapperException
{
    public FactoryMethodExistsException(Type type)
        : base($"Type {type.Name} already has a factory method registered.")
    {
    }
}

public sealed class FactoryMethodException : EfMapperException
{
    public FactoryMethodException(Type type, bool needed)
        : base($"Type {type.Name} {(needed ? "needs" : "doesn't need")} a factory method because it {(needed ? "doesn't have" : "has")} a parameterless constructor.")
    {
    }
}

public sealed class InvaildEntityListPropertyException : EfMapperException
{
    public InvaildEntityListPropertyException(Type sourceType, Type targetType, string propertyName)
        : base($"No valid list of entity property named {propertyName} found for mapping from {sourceType.Name} to {targetType.Name}")
    {
    }

    public InvaildEntityListPropertyException(Type type, string propertyName)
        : base($"No valid list of entity property named {propertyName} found from type {type.Name}")
    {
    }
}