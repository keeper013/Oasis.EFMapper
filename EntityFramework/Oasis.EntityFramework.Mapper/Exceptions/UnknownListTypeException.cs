namespace Oasis.EntityFramework.Mapper.Exceptions;

public class UnknownListTypeException : EfCoreMapperException
{
    public UnknownListTypeException(Type type)
        : base($"No factory method for type {type} has been registered.")
    {
    }
}
