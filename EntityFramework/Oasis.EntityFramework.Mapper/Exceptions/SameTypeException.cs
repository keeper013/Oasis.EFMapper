namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class SameTypeException : EfCoreMapperException
{
    public SameTypeException(Type type)
        : base($"Can't register mapping for type {type} with itself.")
    {
    }
}