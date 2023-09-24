namespace LibrarySample;

using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using System.Threading.Tasks;
using Xunit;

public sealed class TestCase8_InsertUpdateLimit : TestBase
{
    [Fact]
    public async Task Test1_NewEntityInsertedWithEmptyId()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .Register<UpdateBookDTO, Book>()
            .Build()
            .MakeToDatabaseMapper();

        var updateBookDto = new UpdateBookDTO { Name = "Test Book 1" };
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            mapper.DatabaseContext = databaseContext;
            _ = await mapper.MapAsync<UpdateBookDTO, Book>(updateBookDto, null);
            _ = await databaseContext.SaveChangesAsync();
            Assert.Single(await databaseContext.Set<Book>().ToListAsync());
        });
    }

    [Fact]
    public async Task Test2_ExcetionThrownWhenUsageLimited()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .Configure<UpdateBookDTO, Book>()
                .SetMapToDatabaseType(MapToDatabaseType.Update)
                .Finish()
            .Build()
            .MakeToDatabaseMapper();

        var updateBookDto = new UpdateBookDTO { Name = "Test Book 1" };
        await Assert.ThrowsAsync<UpdateToDatabaseWithoutIdException>(async () =>
        {
            await ExecuteWithNewDatabaseContext(async databaseContext =>
            {
                mapper.DatabaseContext = databaseContext;
                _ = await mapper.MapAsync<UpdateBookDTO, Book>(updateBookDto, null);
            });
        });
    }
}
