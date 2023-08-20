namespace LibrarySample;

using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public sealed class TestCase5_KeepUnmatched : TestBase
{
    [Fact]
    public async Task ReplaceDependentProperty_ShouldSucceed()
    {
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
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

        var borrowerDto = mapper.Map<Borrower, UpdateBorrowerDTO>(borrower);
        borrowerDto.Contact = new UpdateContactDTO { PhoneNumber = "23456789", Address = "test address 2" };
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(borrowerDto, b => b.Include(b => b.Contact), databaseContext);
            await databaseContext.SaveChangesAsync();
            var newContact = await databaseContext.Set<Contact>().ToListAsync();
            Assert.Single(newContact);
            Assert.Equal("23456789", newContact[0].PhoneNumber);
        });
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 5)]
    [InlineData(2, 5)]
    public async Task RemoveDependentProperty_ShouldSucceed(int keepUnmatchedCase, int number)
    {
        // initialize mapper
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
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
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

        // create new book
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

        var updateBookDto = mapper.Map<Book, UpdateBookDTO>(book);
        updateBookDto.Copies.RemoveAt(4);
        updateBookDto.Copies.RemoveAt(3);
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = mapper.MapAsync<UpdateBookDTO, Book>(updateBookDto, b => b.Include(b => b.Copies), databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            var copies = await databaseContext.Set<Copy>().ToListAsync();
            Assert.Equal(number, copies.Count);
            for (var i = 1; i <= number; i++)
            {
                Assert.Single(copies.Where(c => string.Equals(c.Number, $"Copy{i}") && c.BookId == updateBookDto.Id));
            }
        });
    }
}
