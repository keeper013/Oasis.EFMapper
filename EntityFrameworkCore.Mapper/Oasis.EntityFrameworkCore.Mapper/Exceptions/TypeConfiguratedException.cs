namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class TypeConfiguratedException : EfCoreMapperException
{
    private readonly Type _type;

    public TypeConfiguratedException(Type type)
    {
        _type = type;
    }

    public override string Message => $"Type {_type} has been configurated already.";
}
