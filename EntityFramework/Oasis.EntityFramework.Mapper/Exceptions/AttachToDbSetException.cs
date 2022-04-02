namespace Oasis.EntityFramework.Mapper.Exceptions;

public class AttachToDbSetException : EfCoreMapperException
{
    public AttachToDbSetException(InvalidOperationException e)
        : base($"Failed to attach existing entity to DbSet (Maybe you forgot to eager load navigation entities when calling {nameof(IMappingToDatabaseSession.MapAsync)} method. If so, pass a proper value to includer parameter).", e)
    {
    }
}
