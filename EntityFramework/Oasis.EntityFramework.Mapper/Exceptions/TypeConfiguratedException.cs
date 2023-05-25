namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class TypeConfiguratedException : EfMapperException
{
    public TypeConfiguratedException(Type type)
        : base($"Type {type} has been configurated already.")
    {
    }
}
