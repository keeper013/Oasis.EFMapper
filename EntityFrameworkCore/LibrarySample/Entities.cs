namespace Oasis.EntityFrameworkCore.Mapper.Sample;

using System.Collections.Generic;

public interface IEntityBaseWithId
{
    public int Id { get; set; }
}

public interface IEntityBaseWithConcurrencyToken
{
    public byte[]? ConcurrencyToken { get; set; }
}

public sealed class Borrower : IEntityBaseWithConcurrencyToken
{
    public string IdentityNumber { get; set; } = null!;
    public byte[]? ConcurrencyToken { get; set; }
    public string Name { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
    public Copy Reserved { get; set; } = null!;
    public List<Copy> Borrowed { get; set; } = new List<Copy>();
}

public sealed class Contact : IEntityBaseWithId, IEntityBaseWithConcurrencyToken
{
    public int Id { get; set; }
    public byte[]? ConcurrencyToken { get; set; }
    public string Borrower { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Address { get; set; } = null!;
}

public sealed class Book : IEntityBaseWithId, IEntityBaseWithConcurrencyToken
{
    public int Id { get; set; }
    public byte[]? ConcurrencyToken { get; set; }
    public string Name { get; set; } = null!;
    public List<Copy> Copies { get; set; } = new List<Copy>();
    public List<Tag> Tags { get; set; } = new List<Tag>();
}

public sealed class Copy : IEntityBaseWithConcurrencyToken
{
    public string Number { get; set; } = null!;
    public byte[]? ConcurrencyToken { get; set; }
    public string? Reserver { get; set; }
    public string? Borrower { get; set; }
    public int BookId { get; set; }
}

public sealed class Tag : IEntityBaseWithId
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<Book> Books { get; set; } = new List<Book>();
}
