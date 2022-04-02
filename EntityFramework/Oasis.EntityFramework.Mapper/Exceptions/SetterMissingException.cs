namespace Oasis.EntityFramework.Mapper.Exceptions;

public class SetterMissingException : EfCoreMapperException
{
    public SetterMissingException(string message)
        : base(message)
    {
    }
}
