namespace Oasis.EntityFramework.Mapper;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public abstract class EntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [ConcurrencyCheck]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public long? Timestamp { get; set; }
}

public abstract class NullableIdEntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long? Id { get; set; }

    [ConcurrencyCheck]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public long? Timestamp { get; set; }
}

public abstract class ReversedEntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Timestamp { get; set; }

    [ConcurrencyCheck]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public long? Id { get; set; }
}

public abstract class EntityBaseNoTimeStamp
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

    [ConcurrencyCheck]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public long? Timestamp { get; set; }
}