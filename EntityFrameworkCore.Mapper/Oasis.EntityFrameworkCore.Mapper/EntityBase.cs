namespace Oasis.EntityFrameworkCore.Mapper;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public abstract class EntityBase : IEntityBase
{
    [Key]
    public long? Id { get; set; }

    [Timestamp]
    [ConcurrencyCheck]
    public byte[]? Timestamp { get; set; }
}
