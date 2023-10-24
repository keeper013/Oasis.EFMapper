namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class InvalidKeyTypeException : EfMapperException
{
    public InvalidKeyTypeException(Type type)
        : base($"{type.Name} is invalid for identity or concurrency token.")
    {
    }
}

public sealed class InitializeOnlyPropertyException : EfMapperException
{
    public InitializeOnlyPropertyException(Type type, string propertyName)
        : base($"{propertyName} of type {type.Name} doesn't have a setter and is not initialized when being mapped.")
    {
    }
}

public sealed class MissingFactoryMethodException : EfMapperException
{
    public MissingFactoryMethodException(Type type)
        : base($"Type {type.Name} doesn't have custom either factory method or a generated construct method.")
    {
    }
}

public sealed class ScalarMapperExistsException : EfMapperException
{
    public ScalarMapperExistsException(Type sourceType, Type targetType)
        : base($"Scalar type mapper from {sourceType} to {targetType} has been registered.")
    {
    }
}

public sealed class InvalidPopertyExpressionException : EfMapperException
{
    public InvalidPopertyExpressionException(string expression)
        : base($"{expression} is not a valid direct property expression.")
    {
    }
}