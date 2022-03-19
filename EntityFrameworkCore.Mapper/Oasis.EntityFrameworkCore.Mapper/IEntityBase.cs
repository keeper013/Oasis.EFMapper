namespace Oasis.EntityFrameworkCore.Mapper;

public interface IEntityBase
{
    public long Id { get; }

    public byte[]? Timestamp { get; }
}
