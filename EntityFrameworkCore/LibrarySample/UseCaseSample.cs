namespace Oasis.EntityFrameworkCore.Mapper.Sample;

using Google.Protobuf;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class UseCaseSample
{
    private const string OldBorrowerName = "Borrower 1";
    private const string UpdatedBorrowerName = "Updated Borrower 1";
    private const string Book1Name = "Book 1";
    private const string Book2Name = "Book 2";
    private const string Book3Name = "Book 3";
    private readonly DbContextOptions _options;

    public UseCaseSample()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(connection)
            .Options;

        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.TimeStamp)));
        mapperBuilder
            .WithScalarConverter<byte[], ByteString>(array => ByteString.CopyFrom(array))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .RegisterTwoWay<Borrower, BorrowerDTO>()
            .Register<Book, BookDTO>();
        Mapper = mapperBuilder.Build();
    }

    /// <summary>
    /// Normally this could be just a field, it's made a property for the purpose:
    /// To emphasize that in real use cases the same mapper should be a singleton exists through different processes.
    /// </summary>
    private IMapper Mapper { get; }

    [Fact]
    public async Task UpdateBorrower_RemovingAndAdding_Test()
    {
        await InitializeDatabaseContent();

        // simulate retrieve information from server side
        var books = await LoadAllBookData();
        var borrower = await LoadBorrowerData(OldBorrowerName);

        // return book 2, borrow book 3 by removing and adding
        var updatedBorrower = ReturnBook2ThenBorrowBook3_ByRemovingAndAdding(books, borrower);

        // update to server
        await UpdateBorrowerInformation(updatedBorrower);

        // verify upddated result;
        await VerifyUpdatedBorrower();
    }

    [Fact]
    public async Task UpdateBorrower_Updating_Test()
    {
        await InitializeDatabaseContent();

        // simulate retrieve information from server side
        var books = await LoadAllBookData();
        var borrower = await LoadBorrowerData(OldBorrowerName);

        // return book 2, borrow book 3 by updating
        var updatedBorrower = ReturnBook2ThenBorrowBook3_ByUpdating(books, borrower);

        // update to server
        await UpdateBorrowerInformation(updatedBorrower);

        // verify upddated result;
        await VerifyUpdatedBorrower();
    }

    private static string ReturnBook2ThenBorrowBook3_ByRemovingAndAdding(string allBooksString, string borrowerString)
    {
        var allBooks = AllBooksDTO.Parser.ParseFrom(Convert.FromBase64String(allBooksString));
        var borrower = BorrowerDTO.Parser.ParseFrom(Convert.FromBase64String(borrowerString));
        var book2Id = allBooks.Books.Where(b => b.Name == Book2Name).Select(b => b.Id).First();
        var book3Id = allBooks.Books.Where(b => b.Name == Book3Name).Select(b => b.Id).First();
        var book2BorrowRecord = borrower.BorrowRecords.First(r => r.BookId == book2Id);
        borrower.BorrowRecords.Remove(book2BorrowRecord);
        borrower.BorrowRecords.Add(new BorrowRecordDTO { BookId = book3Id, BorrowerId = borrower.Id });
        borrower.Name = UpdatedBorrowerName;
        return Convert.ToBase64String(borrower.ToByteArray());
    }

    private static string ReturnBook2ThenBorrowBook3_ByUpdating(string allBooksString, string borrowerString)
    {
        var allBooks = AllBooksDTO.Parser.ParseFrom(Convert.FromBase64String(allBooksString));
        var borrower = BorrowerDTO.Parser.ParseFrom(Convert.FromBase64String(borrowerString));
        var book2Id = allBooks.Books.Where(b => b.Name == Book2Name).Select(b => b.Id).First();
        var book3Id = allBooks.Books.Where(b => b.Name == Book3Name).Select(b => b.Id).First();
        var book2BorrowRecord = borrower.BorrowRecords.First(r => r.BookId == book2Id);
        book2BorrowRecord.BookId = book3Id;
        borrower.Name = UpdatedBorrowerName;
        return Convert.ToBase64String(borrower.ToByteArray());
    }

    private async Task InitializeDatabaseContent()
    {
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var book1 = new Book { Name = Book1Name };
            var book2 = new Book { Name = Book2Name };
            var book3 = new Book { Name = Book3Name };
            var borrower = new Borrower { Name = OldBorrowerName };
            var borrowRecord1 = new BorrowRecord { Borrower = borrower, Book = book1 };
            var borrowRecord2 = new BorrowRecord { Borrower = borrower, Book = book2 };
            borrower.BorrowRecords = new List<BorrowRecord> { borrowRecord1, borrowRecord2 };
            databaseContext.Set<Book>().Add(book1);
            databaseContext.Set<Book>().Add(book2);
            databaseContext.Set<Book>().Add(book3);
            databaseContext.Set<Borrower>().Add(borrower);
            databaseContext.Set<BorrowRecord>().Add(borrowRecord1);
            databaseContext.Set<BorrowRecord>().Add(borrowRecord2);

            await databaseContext.SaveChangesAsync();
        });
    }

    private async Task<string> LoadBorrowerData(string name)
    {
        Borrower? borrower = default;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            borrower = await databaseContext.Set<Borrower>().Include(b => b.BorrowRecords).FirstAsync(b => b.Name == name);
        });

        var session = Mapper.CreateMappingSession();
        var dto = session.Map<Borrower, BorrowerDTO>(borrower!);
        return Convert.ToBase64String(dto.ToByteArray());
    }

    private async Task<string> LoadAllBookData()
    {
        List<Book>? books = default;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            books = await databaseContext.Set<Book>().ToListAsync();
        });

        var session = Mapper.CreateMappingSession();
        var allbooks = new AllBooksDTO();
        foreach (var book in books!)
        {
            allbooks.Books.Add(session.Map<Book, BookDTO>(book));
        }

        return Convert.ToBase64String(allbooks.ToByteArray());
    }

    private async Task UpdateBorrowerInformation(string updatedBorrowerString)
    {
        var dto = BorrowerDTO.Parser.ParseFrom(Convert.FromBase64String(updatedBorrowerString));
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var session = Mapper.CreateMappingToDatabaseSession(databaseContext);
            await session.MapAsync<BorrowerDTO, Borrower>(dto, qb => qb.Include(qb => qb.BorrowRecords));
            await databaseContext.SaveChangesAsync();
        });
    }

    private async Task VerifyUpdatedBorrower()
    {
        Borrower? borrower = default;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            borrower = await databaseContext.Set<Borrower>().FirstOrDefaultAsync(b => b.Name == OldBorrowerName);
            Assert.Null(borrower);
            borrower = await databaseContext.Set<Borrower>().Include(b => b.BorrowRecords)!.ThenInclude(r => r.Book).FirstAsync(b => b.Name == UpdatedBorrowerName);
        });

        Assert.NotNull(borrower);
        Assert.Equal(2, borrower!.BorrowRecords!.Count);
        Assert.NotNull(borrower.BorrowRecords.FirstOrDefault(b => b.Book!.Name == Book1Name));
        Assert.NotNull(borrower.BorrowRecords.FirstOrDefault(b => b.Book!.Name == Book3Name));
    }

    private DbContext CreateDatabaseContext()
    {
        var databaseContext = new DatabaseContext(_options);
        databaseContext.Database.EnsureCreated();

        return databaseContext;
    }

    private async Task ExecuteWithNewDatabaseContext(Func<DbContext, Task> action)
    {
        using (var databaseContext = CreateDatabaseContext())
        {
            await action(databaseContext);
        }
    }
}
