namespace LibrarySample;

using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using System.Threading.Tasks;
using Xunit;

public sealed class TestCase6_CustomMapping : TestBase
{
    [Fact]
    public async Task Test1_CustomMapping_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .Configure<Borrower>()
                .SetKeyPropertyNames(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
                .Finish()
            .Configure<NewBorrowerDTO>()
                .SetIdentityPropertyName(nameof(NewBorrowerDTO.IdentityNumber))
                .Finish()
            .Configure<BorrowerBriefDTO>()
                .SetIdentityPropertyName(nameof(BorrowerBriefDTO.Id))
                .Finish()
            .Configure<Borrower, BorrowerBriefDTO>()
                .MapProperty(brief => brief.Phone, borrower => borrower.Contact.PhoneNumber)
                .Finish()
            .Register<NewBorrowerDTO, Borrower>()
            .Build()
            .MakeMapper();

        // Act
        const string BorrowerName = "Borrower 1";
        const string BorrowerId = "BorrowerIdentityId1";
        const string Phone = "12345678";
        Borrower borrower = null!;

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            mapper.DatabaseContext = databaseContext;
            var borrowerDto = new NewBorrowerDTO
            {
                IdentityNumber = BorrowerId,
                Name = BorrowerName,
                Contact = new NewContactDTO { PhoneNumber = Phone, Address = "test address 1" }
            };

            _ = await mapper.MapAsync<NewBorrowerDTO, Borrower>(borrowerDto, null);
            _ = await databaseContext.SaveChangesAsync();
            borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
        });

        var brief = mapper.Map<Borrower, BorrowerBriefDTO>(borrower);
        Assert.Equal(BorrowerId, brief.Id);
        Assert.Equal(BorrowerName, brief.Name);
        Assert.Equal(Phone, brief.Phone);
    }
}
