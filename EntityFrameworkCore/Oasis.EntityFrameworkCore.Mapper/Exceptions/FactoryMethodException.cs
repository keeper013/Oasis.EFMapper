namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class FactoryMethodExistsException : EfCoreMapperException
{
    public FactoryMethodExistsException(Type type)
        : base($"Type {type.Name} already has a factory method registered.")
    {
    }
}
