namespace LibrarySample;

using Google.Protobuf;
using Oasis.EntityFramework.Mapper.Sample;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Data.Entity;

public interface IBookCopyList : IList<Copy>
{
}

internal sealed class BookCopyList : List<Copy>, IBookCopyList
{
}

public interface IBook
{
    int Id { get; set; }
    byte[]? ConcurrencyToken { get; set; }
    string Name { get; set; }
    IBookCopyList Copies { get; set; }
}

internal sealed class BookImplementation : IBook
{
    public int Id { get; set; }
    public byte[]? ConcurrencyToken { get; set; }
    public string Name { get; set; } = null!;
    public IBookCopyList Copies { get; set; } = null!;
}

[TestFixture]
public sealed class TestCase4_CustomFactoryMethod : TestBase
{
    [Test]
    public async Task Test1_MapNewBookToDatabase()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithFactoryMethod<IBookCopyList>(() => new BookCopyList())
            .WithFactoryMethod<IBook>(() => new BookImplementation())
            .Register<NewBookDTO, Book>()
            .Register<Book, IBook>()
            .Build();

        // create new book
        const string BookName = "Book 1";
        const string Copy1Number = "copy 1";
        const string Copy2Number = "copy 2";
        Book book = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var bookDto = new NewBookDTO { Name = BookName };
            bookDto.Copies.Add(new NewCopyDTO { Number = Copy1Number });
            bookDto.Copies.Add(new NewCopyDTO { Number = Copy2Number });
            _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().FirstAsync();
            Assert.AreEqual(BookName, book.Name);
        });

        // map from book to dto
        var bookInterface = mapper.Map<Book, IBook>(book);
        Assert.AreEqual(BookName, bookInterface.Name);
        Assert.AreEqual(2, bookInterface.Copies.Count);
        Assert.NotNull(bookInterface.Copies.FirstOrDefault(c => string.Equals(Copy1Number, c.Number)));
        Assert.NotNull(bookInterface.Copies.FirstOrDefault(c => string.Equals(Copy2Number, c.Number)));
    }
}
