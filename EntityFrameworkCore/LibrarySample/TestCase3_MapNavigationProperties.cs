namespace LibrarySample;

using Google.Protobuf;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using Oasis.EntityFrameworkCore.Mapper;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public sealed class TestCase3_MapNavigationProperties : TestBase
{
    [Fact]
    public async Task Test1_AddNewBorrower()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .WithConfiguration<Borrower>(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
            .WithConfiguration<NewBorrowerDTO>(nameof(NewBorrowerDTO.IdentityNumber))
            .WithConfiguration<UpdateBorrowerDTO>(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        // create new book
        const string BorrowerName = "Borrower 1";
        const string BorrowerAddress = "Dummy Address";
        Borrower borrower = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var borrowerDto = new NewBorrowerDTO { IdentityNumber = "Identity1", Name = BorrowerName, Contact = new NewContactDTO { PhoneNumber="12345678", Address = BorrowerAddress } };
            _ = await mapper.MapAsync<NewBorrowerDTO, Borrower>(borrowerDto, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
            Assert.Equal(BorrowerName, borrower.Name);
            Assert.Equal(BorrowerAddress, borrower.Contact.Address);
        });

        // update existint book dto
        const string UpdatedBorrowerName = "Updated Borrower 1";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        var updateBorrowerDto = mapper.Map<Borrower, UpdateBorrowerDTO>(borrower);
        updateBorrowerDto.Name = UpdatedBorrowerName;
        updateBorrowerDto.Contact.Address = UpdatedBorrowerAddress;

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(updateBorrowerDto, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
            Assert.Equal(UpdatedBorrowerName, borrower.Name);
            Assert.Equal(UpdatedBorrowerAddress, borrower.Contact.Address);
        });
    }
}
