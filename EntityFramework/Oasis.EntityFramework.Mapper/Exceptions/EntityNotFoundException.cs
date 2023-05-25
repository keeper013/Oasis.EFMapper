namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class EntityNotFoundException : EfMapperException
{
    public EntityNotFoundException(Type type, object id)
        : base($"Entity of (type: id) ({type}: {id}) is not found.")
    {
    }
}
