namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class FactoryMethodException : EfCoreMapperException
{
    public FactoryMethodException(Type type, bool needed)
        : base($"Type {type} {(needed ? "needs" : "doesn't need")} a factory method because it {(needed ? "doesn't have" : "has")} a parameterless constructor.")
    {
    }
}
