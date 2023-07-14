namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class UnconstructableTypeException : EfCoreMapperException
{
    public UnconstructableTypeException(Type type)
        : base($"Type {type} doesn't have a parameterless constructor or a factory method defined.")
    {
    }
}
