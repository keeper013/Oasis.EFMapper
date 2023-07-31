namespace LibrarySample;

using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using Oasis.EntityFrameworkCore.Mapper;
using System.Threading.Tasks;
using Xunit;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class TestCase2_MapEntityToDatabase_WithConcurrencyToken : TestBase
{
    [Fact]
    public async Task Test1_MapNewBookToDatabase()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
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
            _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().FirstAsync();
            Assert.Equal(BookName, book.Name);
        });

        // map from book to dto
        var bookDto = mapper.Map<Book, BookDTO>(book);
        Assert.NotNull(bookDto.ConcurrencyToken);
        Assert.NotEmpty(bookDto.ConcurrencyToken);
    }

    [Fact]
    public async Task Test2_UpdateExistingBookToDatabase()
    {
        _ = await UpdateExistingBookToDatabase();
    }

    [Fact]
    public async Task Test3_UpdateExistingBookToDatabase_WithConcurrencyTokenException()
    {
        var tuple = await UpdateExistingBookToDatabase();

        await Assert.ThrowsAsync<ConcurrencyTokenException>(async () =>
        {
            await ExecuteWithNewDatabaseContext(async databaseContext =>
            {
                _ = await tuple.Item1.MapAsync<UpdateBookDTO, Book>(tuple.Item2, databaseContext);
            });
        });
    }

    private async Task<(IMapper, UpdateBookDTO)> UpdateExistingBookToDatabase()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
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
            _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().FirstAsync();
            Assert.Equal(BookName, book.Name);
        });

        // update existint book dto
        const string UpdatedBookName = "Updated Book 1";
        var updateBookDto = mapper.Map<Book, UpdateBookDTO>(book);
        Assert.NotNull(updateBookDto.ConcurrencyToken);
        Assert.NotEmpty(updateBookDto.ConcurrencyToken);
        updateBookDto.Name = UpdatedBookName;

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = await mapper.MapAsync<UpdateBookDTO, Book>(updateBookDto, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().FirstAsync();
            Assert.Equal(UpdatedBookName, book.Name);
        });

        return (mapper, updateBookDto);
    }
}
