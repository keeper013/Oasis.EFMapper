namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class EntityNotFoundException : Exception
{
    private readonly Type _type;
    private readonly object _id;

    public EntityNotFoundException(Type type, object id)
    {
        _type = type;
        _id = id;
    }

    public override string Message => $"Entity of (type: id) ({_type}: {_id}) is not found.";
}
