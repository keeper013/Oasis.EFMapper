namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class TypeConfiguratedException : EfCoreMapperException
{
    public TypeConfiguratedException(Type type)
        : base($"Type {type} has been configurated already.")
    {
    }
}
