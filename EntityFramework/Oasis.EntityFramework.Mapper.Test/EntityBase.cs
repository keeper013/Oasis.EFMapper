namespace Oasis.EntityFramework.Mapper;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public interface IEntityWithConcurrencyToken
{
    byte[] ConcurrencyToken { get; set; }
}


public abstract class EntityBase : IEntityWithConcurrencyToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public byte[] ConcurrencyToken { get; set; } = null!;
}

public abstract class NullableIdEntityBase : IEntityWithConcurrencyToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long? Id { get; set; }

    public byte[] ConcurrencyToken { get; set; } = null!;
}

public abstract class ReversedEntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ConcurrencyToken { get; set; }

    [ConcurrencyCheck]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public byte[] Id { get; set; } = null!;
}

public abstract class EntityBaseNoConcurrencyToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AnotherId { get; set; }
}

public abstract class EntityBase<T> : IEntityWithConcurrencyToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public T? Id { get; set; }

    public byte[] ConcurrencyToken { get; set; } = null!;
}