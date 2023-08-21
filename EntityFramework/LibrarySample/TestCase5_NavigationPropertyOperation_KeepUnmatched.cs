namespace LibrarySample;

using Google.Protobuf;
using Oasis.EntityFramework.Mapper.Sample;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Data.Entity;

[TestFixture]
public sealed class TestCase5_NavigationPropertyOperation_KeepUnmatched : TestBase
{
    [Test]
    [Ignore("EF6 doesn't seems to handle replacing one to one relation entity very well, it updates first, and cause a unique constraint problem, deleting should come first.")]
    public async Task Test1_ReplaceDependentProperty_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<UpdateBorrowerDTO>()
                .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
                .Finish()
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        // Act
        const string BorrowerName = "Borrower 1";
        Borrower borrower = null!;

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var borrowerDto = new NewBorrowerDTO
            {
                IdentityNumber = "Identity1",
                Name = BorrowerName,
                Contact = new NewContactDTO { PhoneNumber = "12345678", Address = "test address 1" }
            };

            _ = await mapper.MapAsync<NewBorrowerDTO, Borrower>(borrowerDto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
        });

        // Assert
        var borrowerDto = mapper.Map<Borrower, UpdateBorrowerDTO>(borrower);
        borrowerDto.Contact = new UpdateContactDTO { PhoneNumber = "23456789", Address = "test address 2" };
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(borrowerDto, b => b.Include(b => b.Contact), databaseContext);
            await databaseContext.SaveChangesAsync();
            var newContact = await databaseContext.Set<Contact>().ToListAsync();
            Assert.AreEqual(1, newContact.Count);
            Assert.AreEqual("23456789", newContact[0].PhoneNumber);
        });
    }

    [Test]
    public async Task Test2_ReplaceIndependentProperty_ShouldSucceed()
    {
        // Arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<UpdateBorrowerDTO>()
                .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
                .Finish()
            .Configure<NewCopyDTO>()
                .SetIdentityPropertyName(nameof(NewCopyDTO.Number))
                .Finish()
            .Configure<Copy>()
                .SetIdentityPropertyName(nameof(Copy.Number))
                .Finish()
            .Configure<CopyReferenceDTO>()
                .SetIdentityPropertyName(nameof(CopyReferenceDTO.Number))
                .Finish()
            .Register<NewBookDTO, Book>()
            .Register<Book, UpdateBookDTO>()
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        // Act
        UpdateBookDTO book = null!;
        UpdateBorrowerDTO borrower = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var bookDto = new NewBookDTO { Name = "Test" };
            for (var i = 0; i < 2; i++)
            {
                bookDto.Copies.Add(new NewCopyDTO { Number = $"Copy{i + 1}" });
            }

            var bk = await mapper.MapAsync<NewBookDTO, Book>(bookDto, null, databaseContext);

            var borrowerDto = new NewBorrowerDTO
            {
                IdentityNumber = "Identity1",
                Name = "borrower",
                Contact = new NewContactDTO { PhoneNumber = "12345678", Address = "test address 1" }
            };

            var br = await mapper.MapAsync<NewBorrowerDTO, Borrower>(borrowerDto, null, databaseContext);

            _ = await databaseContext.SaveChangesAsync();

            book = mapper.Map<Book, UpdateBookDTO>(bk);
            borrower = mapper.Map<Borrower, UpdateBorrowerDTO>(br);
        });

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            borrower.Reserved = book.Copies[0]!;
            var br = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(borrower, b => b.Include(b => b.Reserved), databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            borrower = mapper.Map<Borrower, UpdateBorrowerDTO>(br);
        });

        // Assert
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            borrower.Reserved = book.Copies[1]!;
            var br = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(borrower, b => b.Include(b => b.Reserved), databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            br = await databaseContext.Set<Borrower>().Include(b => b.Reserved).FirstOrDefaultAsync();
            Assert.NotNull(br);
            Assert.AreEqual(book.Copies[1].Number, br!.Reserved.Number);
            var copies = await databaseContext.Set<Copy>().ToListAsync();
            Assert.AreEqual(2, copies.Count);
            for (var i = 1; i <= 2; i++)
            {
                Assert.AreEqual(1, copies.Where(c => string.Equals(c.Number, $"Copy{i}")).Count());
            }
        });
    }

    [Ignore("EF6 doesn't seems to handle replacing one to one relation entity very well, it updates first, and cause a unique constraint problem, deleting should come first.")]
    [TestCase(0, 3)]
    [TestCase(1, 5)]
    [TestCase(2, 5)]
    public async Task Test3_RemoveDependentProperty_ShouldSucceed(int keepUnmatchedCase, int number)
    {
        // Arrange
        var mapperBuilder = MakeDefaultMapperBuilder();
        if (keepUnmatchedCase == 1)
        {
            mapperBuilder = mapperBuilder
                .Configure<Book>()
                .KeepUnmatched(nameof(Book.Copies))
                .Finish();
        }
        else if (keepUnmatchedCase == 2)
        {
            mapperBuilder = mapperBuilder
                .Configure<UpdateBookDTO, Book>()
                .KeepUnmatched(nameof(Book.Copies))
                .Finish();
        }

        var mapper = mapperBuilder
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<NewCopyDTO>()
                .SetIdentityPropertyName(nameof(NewCopyDTO.Number))
                .Finish()
            .Configure<Copy>()
                .SetIdentityPropertyName(nameof(Copy.Number))
                .Finish()
            .Configure<CopyReferenceDTO>()
                .SetIdentityPropertyName(nameof(CopyReferenceDTO.Number))
                .Finish()
            .Register<NewBookDTO, Book>()
            .RegisterTwoWay<Book, UpdateBookDTO>()
            .Build();

        // Act
        const string BookName = "Book 1";
        Book book = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var bookDto = new NewBookDTO { Name = BookName };
            for (var i = 0; i < 5; i++)
            {
                bookDto.Copies.Add(new NewCopyDTO { Number = $"Copy{i + 1}" });
            }

            _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().Include(b => b.Copies).FirstAsync();
        });

        // Assert
        var updateBookDto = mapper.Map<Book, UpdateBookDTO>(book);
        updateBookDto.Copies.RemoveAt(4);
        updateBookDto.Copies.RemoveAt(3);
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = mapper.MapAsync<UpdateBookDTO, Book>(updateBookDto, b => b.Include(b => b.Copies), databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            var copies = await databaseContext.Set<Copy>().ToListAsync();
            Assert.AreEqual(number, copies.Count);
            for (var i = 1; i <= number; i++)
            {
                Assert.AreEqual(1, copies.Where(c => string.Equals(c.Number, $"Copy{i}") && c.BookId == updateBookDto.Id).Count());
            }
        });
    }

    [Test]
    public async Task Test4_ReplaceIndependentProperty_ShouldSucceed()
    {
        // Arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<long, string>(l => l.ToString())
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<UpdateBorrowerDTO>()
                .SetKeyPropertyNames(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
                .Finish()
            .Configure<NewCopyDTO>()
                .SetIdentityPropertyName(nameof(NewCopyDTO.Number))
                .Finish()
            .Configure<Copy>()
                .SetIdentityPropertyName(nameof(Copy.Number))
                .Finish()
            .Configure<CopyReferenceDTO>()
                .SetIdentityPropertyName(nameof(CopyReferenceDTO.Number))
                .Finish()
            .Register<NewBookDTO, Book>()
            .Register<Book, UpdateBookDTO>()
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        // Act
        UpdateBookDTO book = null!;
        UpdateBorrowerDTO borrower = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var bookDto = new NewBookDTO { Name = "Test" };
            for (var i = 0; i < 2; i++)
            {
                bookDto.Copies.Add(new NewCopyDTO { Number = $"Copy{i + 1}" });
            }

            var bk = await mapper.MapAsync<NewBookDTO, Book>(bookDto, null, databaseContext);

            var borrowerDto = new NewBorrowerDTO
            {
                IdentityNumber = "Identity1",
                Name = "borrower",
                Contact = new NewContactDTO { PhoneNumber = "12345678", Address = "test address 1" }
            };

            var br = await mapper.MapAsync<NewBorrowerDTO, Borrower>(borrowerDto, null, databaseContext);

            _ = await databaseContext.SaveChangesAsync();

            book = mapper.Map<Book, UpdateBookDTO>(bk);
            borrower = mapper.Map<Borrower, UpdateBorrowerDTO>(br);
        });

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            borrower.Borrowed.Add(book.Copies[0]!);
            var br = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(borrower, b => b.Include(b => b.Borrowed), databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            borrower = mapper.Map<Borrower, UpdateBorrowerDTO>(br);
        });

        // Assert
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            borrower.Borrowed.Clear();
            borrower.Borrowed.Add(book.Copies[1]!);
            var br = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(borrower, b => b.Include(b => b.Borrowed), databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            br = await databaseContext.Set<Borrower>().Include(b => b.Borrowed).FirstOrDefaultAsync();
            Assert.NotNull(br);
            Assert.AreEqual(1, br!.Borrowed.Count);
            Assert.AreEqual(book.Copies[1].Number, br.Borrowed[0].Number);
            var copies = await databaseContext.Set<Copy>().ToListAsync();
            Assert.AreEqual(2, copies.Count);
            for (var i = 1; i <= 2; i++)
            {
                Assert.AreEqual(1, copies.Where(c => string.Equals(c.Number, $"Copy{i}")).Count());
            }
        });
    }
}
