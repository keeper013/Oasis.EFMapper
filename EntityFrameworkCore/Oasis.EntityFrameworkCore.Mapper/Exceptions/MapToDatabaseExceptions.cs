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

public sealed class MissingKeyPropertyException : EfCoreMapperException
{
    public MissingKeyPropertyException(Type type, string keyName, string keyPropertyName)
        : base($"Type {type.Name} needs a valid {keyName} property of name {keyPropertyName}.")
    {
    }
}

public sealed class MapToDatabaseTypeException : EfCoreMapperException
{
    public MapToDatabaseTypeException(Type sourceType, Type targetType, MapToDatabaseType mapType)
        : base($"Mapping from {sourceType.Name} to {targetType.Name}, {mapType} is configured as not allowed.")
    {
    }
}

public sealed class ConcurrencyTokenException : EfCoreMapperException
{
    public ConcurrencyTokenException(Type sourceType, Type targetType)
        : base($"Concurrency token mismatch when mapping from {sourceType.Name} to {targetType.Name}.")
    {
    }
}

public sealed class AsNoTrackingNotAllowedException : EfCoreMapperException
{
    public AsNoTrackingNotAllowedException(string includerString)
        : base($"{includerString}: Call of AsNoTracking() method is not allowed when mapping to entities to avoid sub entity deletion failures.")
    {
    }
}