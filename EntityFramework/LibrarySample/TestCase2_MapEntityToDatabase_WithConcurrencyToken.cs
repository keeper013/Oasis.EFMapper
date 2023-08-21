namespace LibrarySample;

using Google.Protobuf;
using Oasis.EntityFramework.Mapper.Sample;
using System.Threading.Tasks;
using NUnit.Framework;
using Oasis.EntityFramework.Mapper.Exceptions;
using System.Data.Entity;
using Oasis.EntityFramework.Mapper;

[TestFixture]
public sealed class TestCase2_MapEntityToDatabase_WithConcurrencyToken : TestBase
{
    [Test]
    public async Task Test1_MapNewBookToDatabase()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .Register<NewBookDTO, Book>()
            .Register<Book, BookDTO>()
            .Build();

        // create new book
        const string BookName = "Book 1";
        Book book = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var bookDto = new NewBookDTO { Name = BookName };
            _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().FirstAsync();
            Assert.AreEqual(BookName, book.Name);
        });

        // map from book to dto
        var bookDto = mapper.Map<Book, BookDTO>(book);
        Assert.NotNull(bookDto.ConcurrencyToken);
        Assert.IsNotEmpty(bookDto.ConcurrencyToken);
    }

    [Test]
    public async Task Test2_UpdateExistingBookToDatabase()
    {
        _ = await UpdateExistingBookToDatabase();
    }

    [Test]
    public async Task Test3_UpdateExistingBookToDatabase_WithConcurrencyTokenException()
    {
        var tuple = await UpdateExistingBookToDatabase();

        Assert.ThrowsAsync<ConcurrencyTokenException>(async () =>
        {
            await ExecuteWithNewDatabaseContext(async databaseContext =>
            {
                _ = await tuple.Item1.MapAsync<UpdateBookDTO, Book>(tuple.Item2, null, databaseContext);
            });
        });
    }

    private async Task<(IMapper, UpdateBookDTO)> UpdateExistingBookToDatabase()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .Register<NewBookDTO, Book>()
            .RegisterTwoWay<Book, UpdateBookDTO>()
            .Build();

        // create new book
        const string BookName = "Book 1";
        Book book = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var bookDto = new NewBookDTO { Name = BookName };
            _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().FirstAsync();
            Assert.AreEqual(BookName, book.Name);
        });

        // update existint book dto
        const string UpdatedBookName = "Updated Book 1";
        var updateBookDto = mapper.Map<Book, UpdateBookDTO>(book);
        Assert.NotNull(updateBookDto.ConcurrencyToken);
        Assert.IsNotEmpty(updateBookDto.ConcurrencyToken);
        updateBookDto.Name = UpdatedBookName;

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = await mapper.MapAsync<UpdateBookDTO, Book>(updateBookDto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().FirstAsync();
            Assert.AreEqual(UpdatedBookName, book.Name);
        });

        return (mapper, updateBookDto);
    }
}
