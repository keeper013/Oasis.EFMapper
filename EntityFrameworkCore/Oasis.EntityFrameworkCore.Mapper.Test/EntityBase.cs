namespace Oasis.EntityFrameworkCore.Mapper;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public abstract class EntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Timestamp]
    [ConcurrencyCheck]
    public byte[]? ConcurrencyToken { get; set; }
}

public abstract class NullableIdEntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long? Id { get; set; }

    [Timestamp]
    [ConcurrencyCheck]
    public byte[]? ConcurrencyToken { get; set; }
}

public abstract class ReversedEntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ConcurrencyToken { get; set; }

    [Timestamp]
    [ConcurrencyCheck]
    public byte[]? Id { get; set; }
}

public abstract class EntityBaseNoConcurrencyToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AnotherId { get; set; }
}

public abstract class EntityBase<T>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public T? Id { get; set; }

    [Timestamp]
    [ConcurrencyCheck]
    public byte[]? ConcurrencyToken { get; set; }
}