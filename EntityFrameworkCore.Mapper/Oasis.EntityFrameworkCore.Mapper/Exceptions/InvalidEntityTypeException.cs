namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class InvalidEntityTypeException : Exception
{
    private readonly Type _type;

    public InvalidEntityTypeException(Type type)
    {
        _type = type;
    }

    public override string Message => $"Type {_type} is invalid to be registered as an entity type.";
}