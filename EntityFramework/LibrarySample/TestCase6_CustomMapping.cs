namespace LibrarySample;

using System.Data.Entity;
using System.Threading.Tasks;
using NUnit.Framework;
using Oasis.EntityFramework.Mapper.Sample;

[TestFixture]
public sealed class TestCase6_CustomMapping : TestBase
{
    [Test]
    public async Task Test1_CustomMapping_ShouldSucceed()
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
            .Configure<BorrowerBriefDTO>()
                .SetIdentityPropertyName(nameof(BorrowerBriefDTO.Id))
                .Finish()
            .Configure<Borrower, BorrowerBriefDTO>()
                .MapProperty(brief => brief.Phone, borrower => borrower.Contact.PhoneNumber)
                .Finish()
            .Register<NewBorrowerDTO, Borrower>()
            .Build();

        // Act
        const string BorrowerName = "Borrower 1";
        const string BorrowerId = "BorrowerIdentityId1";
        const string Phone = "12345678";
        Borrower borrower = null!;

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var borrowerDto = new NewBorrowerDTO
            {
                IdentityNumber = BorrowerId,
                Name = BorrowerName,
                Contact = new NewContactDTO { PhoneNumber = Phone, Address = "test address 1" }
            };

            _ = await mapper.MapAsync<NewBorrowerDTO, Borrower>(borrowerDto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
        });

        var brief = mapper.Map<Borrower, BorrowerBriefDTO>(borrower);
        Assert.AreEqual(BorrowerId, brief.Id);
        Assert.AreEqual(BorrowerName, brief.Name);
        Assert.AreEqual(Phone, brief.Phone);
    }
}
