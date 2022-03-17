namespace Oasis.EntityFrameworkCore.Mapper;

public interface IEntityBase
{
    // TODO: try not nullable id
    public long? Id { get; }

    public byte[]? Timestamp { get; }
}
