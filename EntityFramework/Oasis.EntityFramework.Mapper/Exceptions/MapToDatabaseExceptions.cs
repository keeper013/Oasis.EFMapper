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
    public MissingIdentityException(Type type)
        : base($"Type {type} needs a valid identity property for KeepOnMappingRemoved configuration.")
    {
    }
}

public sealed class CustomTypePropertyEntityRemoverException : EfMapperException
{
    public CustomTypePropertyEntityRemoverException(Type source, Type targetType, string propertyName)
        : base($"No property of entity type or entity list type with name(s) \"{propertyName}\" was found when mapping from ${source} to ${targetType}")
    {
    }
}