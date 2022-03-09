namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class StaleEntityException : Exception
{
    private readonly Type _type;
    private readonly long _id;

    public StaleEntityException(Type type, long id)
    {
        _type = type;
        _id = id;
    }

    public override string Message => $"Entity data of (type: id) ({_type}: {_id}) is stale.";
}
