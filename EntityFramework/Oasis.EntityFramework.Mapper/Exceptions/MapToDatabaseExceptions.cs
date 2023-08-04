namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class UpdateToDatabaseWithoutIdException : EfMapperException
{
    public UpdateToDatabaseWithoutIdException()
        : base("The record to be updated only doesn't have an id property with valid value.")
    {
    }
}

public sealed class InsertToDatabaseWithExistingException : EfMapperException
{
    public InsertToDatabaseWithExistingException()
        : base("The record to be inserted only already exists in database")
    {
    }
}

public sealed class UpdateToDatabaseWithoutRecordException : EfMapperException
{
    public UpdateToDatabaseWithoutRecordException()
        : base("The record to be updated doesn't have a record in database.")
    {
    }
}

public sealed class MissingIdentityException : EfMapperException
{
    public MissingIdentityException(Type type, string identityPropertyName)
        : base($"Type {type.Name} needs a valid identity property of name ${identityPropertyName}.")
    {
    }
}

public sealed class MapToDatabaseTypeException : EfMapperException
{
    public MapToDatabaseTypeException(Type sourceType, Type targetType, MapToDatabaseType mapType)
        : base($"Mapping from {sourceType.Name} to {targetType.Name}, {mapType} is configured as not allowed.")
    {
    }
}