namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class InvalidEntityBasePropertyException : Exception
{
    private readonly Type _type;
    private readonly string _columnName;
    private readonly string _propertyName;

    public InvalidEntityBasePropertyException(Type type, string columnName, string propertyName)
    {
        _type = type;
        _columnName = columnName;
        _propertyName = propertyName;
    }

    public override string Message => $"Type {_type} doesn't have a property for {_columnName} named \"{_propertyName}\".";
}
