namespace Oasis.EntityFrameworkCore.Mapper.Sample;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public abstract class EntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Timestamp]
    [ConcurrencyCheck]
    public byte[]? TimeStamp { get; set; }
}

public sealed class Borrower : EntityBase
{
    public string? Name { get; set; }

    public ICollection<BorrowRecord>? BorrowRecords { get; set; }
}

public sealed class Book : EntityBase
{
    public string? Name { get; set; }

    public BorrowRecord? BorrowRecord { get; set; }
}

public sealed class BorrowRecord : EntityBase
{
    public int BorrowerId { get; set; }

    public int BookId { get; set; }

    public Borrower? Borrower { get; set; }

    public Book? Book { get; set; }
}
