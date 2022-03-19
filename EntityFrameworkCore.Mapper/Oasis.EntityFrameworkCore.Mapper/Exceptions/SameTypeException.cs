namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public class SameTypeException : Exception
{
    private readonly Type _type;

    public SameTypeException(Type type)
    {
        _type = type;
    }

    public override string Message => $"Can't register mapping for type {_type} with itself two-way.";
}
