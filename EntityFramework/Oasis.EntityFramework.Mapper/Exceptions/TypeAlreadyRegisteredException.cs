namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class TypeAlreadyRegisteredException : EfCoreMapperException
{
    public TypeAlreadyRegisteredException(Type type)
        : base($"Type {type} can't have customized configuration because its mapping has been registered.")
    {
    }
}
