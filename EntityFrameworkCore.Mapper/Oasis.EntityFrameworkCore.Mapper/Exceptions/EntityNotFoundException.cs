namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class EntityNotFoundException : EfCoreMapperException
{
    public EntityNotFoundException(Type type, object id)
        : base($"Entity of (type: id) ({type}: {id}) is not found.")
    {
    }
}
