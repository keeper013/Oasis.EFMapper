namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class RedundantConfiguratedException : EfCoreMapperException
{
    public RedundantConfiguratedException(Type type)
        : base($"Type {type.Name} has been configurated already.")
    {
    }

    public RedundantConfiguratedException(Type sourceType, Type targetType)
        : base($"Custom configuration from {sourceType.Name} to {targetType.Name} already exists.")
    {
    }
}

public sealed class EmptyConfiguratedException : EfCoreMapperException
{
    public EmptyConfiguratedException(Type type)
        : base($"Empty configuration: At least 1 configuration item of {type.Name} should not be null.")
    {
    }
}
