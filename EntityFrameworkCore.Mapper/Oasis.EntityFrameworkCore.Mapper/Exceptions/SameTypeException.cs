namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class SameTypeException : EfCoreMapperException
{
    private readonly Type _type;

    public SameTypeException(Type type)
    {
        _type = type;
    }

    public override string Message => $"Can't register mapping for type {_type} with itself two-way.";
}