namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class FactoryMethodExistsException : Exception
{
    private readonly Type _type;

    public FactoryMethodExistsException(Type type)
    {
        _type = type;
    }

    public override string Message => $"Type {_type} already has a factory method registered.";
}
