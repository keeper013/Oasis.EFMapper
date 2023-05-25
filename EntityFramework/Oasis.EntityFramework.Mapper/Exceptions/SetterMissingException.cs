namespace Oasis.EntityFramework.Mapper.Exceptions;

public class SetterMissingException : EfMapperException
{
    public SetterMissingException(string message)
        : base(message)
    {
    }
}
