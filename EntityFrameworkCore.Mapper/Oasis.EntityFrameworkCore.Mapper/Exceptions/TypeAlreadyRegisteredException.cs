namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class TypeAlreadyRegisteredException : Exception
{
    private readonly Type _type;

    public TypeAlreadyRegisteredException(Type type)
    {
        _type = type;
    }

    public override string Message => $"Type {_type} can't have customized configuration because its mapping has been registered.";
}
