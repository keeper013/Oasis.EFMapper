namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class TypeAlreadyRegisteredException : EfMapperException
{
    public TypeAlreadyRegisteredException(Type type)
        : base($"Type {type} can't have customized configuration because its mapping has been registered.")
    {
    }
}
