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
            .WithScalarConverter<long, string>(l => l.ToString())
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
        Assert.AreNotEqual(0, bookDto.ConcurrencyToken);
    }

    [Test]
    public async Task Test2_UpdateExistingBookToDatabase()
    {
        _ = await UpdateExistingBookToDatabase();
    }

    [Test]
    [Ignore("EF6 doesn't seems to handle replacing one to one relation entity very well, it updates first, and cause a unique constraint problem, deleting should come first.")]
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
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
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
        Assert.AreNotEqual(0, updateBookDto.ConcurrencyToken);
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
