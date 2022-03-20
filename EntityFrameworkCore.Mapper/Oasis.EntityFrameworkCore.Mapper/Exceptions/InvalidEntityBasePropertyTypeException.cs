namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class InvalidEntityBasePropertyTypeException : Exception
{
    private readonly Type _type;
    private readonly string _propertyName;

    public InvalidEntityBasePropertyTypeException(Type type, string propertyName)
    {
        _type = type;
        _propertyName = propertyName;
    }

    public override string Message => $"Type {_type} is not valid for {_propertyName}.";
}
