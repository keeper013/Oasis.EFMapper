namespace Oasis.EntityFrameworkCore.Mapper;

// TODO: make id and timestamp time generic
public interface IEntityBase
{
    public long? Id { get; }

    // TODO: mapping between different types
    public byte[]? Timestamp { get; }
}
