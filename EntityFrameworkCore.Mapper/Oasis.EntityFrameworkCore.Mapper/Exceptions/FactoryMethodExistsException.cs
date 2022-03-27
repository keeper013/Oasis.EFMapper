namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class FactoryMethodException : EfCoreMapperException
{
    private readonly Type _type;
    private readonly bool _needed;

    public FactoryMethodException(Type type, bool needed)
    {
        _type = type;
        _needed = needed;
    }

    public override string Message => $"Type {_type} {(_needed ? "needs" : "doesn't need")} a factory method because it {(_needed ? "doesn't have" : "has")} a parameterless constructor.";
}
