namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class SetterMissingException : EfCoreMapperException
{
    public SetterMissingException(string message)
        : base(message)
    {
    }
}
