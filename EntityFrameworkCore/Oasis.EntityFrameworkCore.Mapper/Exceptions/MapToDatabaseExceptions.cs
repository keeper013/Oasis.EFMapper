namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class UpdateToDatabaseWithoutIdException : EfCoreMapperException
{
    public UpdateToDatabaseWithoutIdException()
        : base("The record to be updated only doesn't have an id property with valid value.")
    {
    }
}

public sealed class InsertToDatabaseWithExistingException : EfCoreMapperException
{
    public InsertToDatabaseWithExistingException()
        : base("The record to be inserted only already exists in database")
    {
    }
}

public sealed class UpdateToDatabaseWithoutRecordException : EfCoreMapperException
{
    public UpdateToDatabaseWithoutRecordException()
        : base("The record to be updated doesn't have a record in database.")
    {
    }
}

public sealed class MissingIdentityException : EfCoreMapperException
{
    public MissingIdentityException(Type type)
        : base($"Type {type} needs a valid identity property for KeepOnMappingRemoved configuration.")
    {
    }
}

public sealed class CustomTypePropertyEntityRemoverException : EfCoreMapperException
{
    public CustomTypePropertyEntityRemoverException(Type source, Type targetType, string propertyName)
        : base($"No property of entity type or entity list type with name(s) \"{propertyName}\" was found when mapping from ${source} to ${targetType}")
    {
    }
}