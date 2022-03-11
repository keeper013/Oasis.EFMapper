namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal class EntityIdNotMatchException : Exception
{
    private readonly long? _id1;
    private readonly long? _id2;

    public EntityIdNotMatchException(long? id1, long? id2)
    {
        _id1 = id1;
        _id2 = id2;
    }

    public override string Message => $"Id {(_id1.HasValue ? _id1.Value.ToString() : "null")} does not match {(_id2.HasValue ? _id2.Value.ToString() : "null")}.";
}
