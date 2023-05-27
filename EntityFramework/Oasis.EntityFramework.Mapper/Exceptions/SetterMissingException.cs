namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class SetterMissingException : EfMapperException
{
    public SetterMissingException(string message)
        : base(message)
    {
    }
}
