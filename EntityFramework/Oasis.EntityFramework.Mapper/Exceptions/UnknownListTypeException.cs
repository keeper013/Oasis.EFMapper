namespace Oasis.EntityFramework.Mapper.Exceptions;

public class UnknownListTypeException : EfMapperException
{
    public UnknownListTypeException(Type type)
        : base($"No factory method for type {type} has been registered.")
    {
    }
}
