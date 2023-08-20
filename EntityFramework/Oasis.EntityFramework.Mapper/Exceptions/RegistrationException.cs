namespace Oasis.EntityFramework.Mapper.Exceptions;

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