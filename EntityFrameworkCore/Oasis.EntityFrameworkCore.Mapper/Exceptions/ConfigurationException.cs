namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class RedundantConfigurationException : EfCoreMapperException
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

public sealed class EmptyConfigurationException : EfCoreMapperException
{
    public EmptyConfigurationException(Type type)
        : base($"Empty configuration: At least 1 configuration item of {type.Name} should not be null.")
    {
    }
}

public sealed class KeyTypeExcludedException : EfCoreMapperException
{
    public KeyTypeExcludedException(Type sourceType, Type targetType, string propertyName)
        : base($"Property {propertyName} is identity/concurrency token that can't be excluded when mapping from type {sourceType} to {targetType}.")
    {
    }
}

public sealed class CustomMappingPropertyExcludedException : EfCoreMapperException
{
    public CustomMappingPropertyExcludedException(Type sourceType, Type targetType, string propertyName)
        : base($"Property {propertyName} is customly mapped that can't be excluded when mapping from type {sourceType} to {targetType}.")
    {
    }
}

public sealed class KeepUnmatchedPropertyExcludedException : EfCoreMapperException
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

public sealed class CustomMappingPropertyKeepUnmatchedException : EfCoreMapperException
{
    public CustomMappingPropertyKeepUnmatchedException(Type sourceType, Type targetType, string propertyName)
        : base($"Property {propertyName} can't be configured to be both customly mappend and keep unmatched when mapping from type {sourceType} to {targetType}.")
    {
    }
}

public sealed class UselessExcludeException : EfCoreMapperException
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

public sealed class InvalidDependentException : EfCoreMapperException
{
    public InvalidDependentException(Type type, string propertyName)
        : base($"Type {type.Name} doesn't have a valid property of class type of list of class type named {propertyName} to be a dependent property.")
    {
    }
}

public sealed class InvalidEntityListTypeException : EfCoreMapperException
{
    public InvalidEntityListTypeException(Type type)
        : base($"Type {type.Name} is invalid for an entity list type.")
    {
    }
}

public sealed class InvalidEntityTypeException : EfCoreMapperException
{
    public InvalidEntityTypeException(Type type)
        : base($"Type {type.Name} is invalid for an entity type.")
    {
    }
}

public sealed class ScalarTypeMissingException : EfCoreMapperException
{
    public ScalarTypeMissingException(Type sourceType, Type targetType)
        : base($"At list one of {sourceType} and {targetType} should be a scalar type.")
    {
    }
}

public sealed class FactoryMethodExistsException : EfCoreMapperException
{
    public FactoryMethodExistsException(Type type)
        : base($"Type {type.Name} already has a factory method registered.")
    {
    }
}

public sealed class FactoryMethodException : EfCoreMapperException
{
    public FactoryMethodException(Type type, bool needed)
        : base($"Type {type.Name} {(needed ? "needs" : "doesn't need")} a factory method because it {(needed ? "doesn't have" : "has")} a parameterless constructor.")
    {
    }
}

public sealed class InvaildEntityListPropertyException : EfCoreMapperException
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