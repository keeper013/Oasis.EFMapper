namespace Oasis.EntityFrameworkCore.Mapper.Test.ToDatabase;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Threading.Tasks;
using Xunit;

public class ToDatabaseTests : TestBase
{
    [Fact]
    public async Task UpdateNullId_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapToDatabaseType(MapToDatabaseType.Update)
                .Finish()
            .Build()
            .MakeToDatabaseMapper();
        var instance = new ToDatabaseEntity2(null, null, 1);

        // assert
        await Assert.ThrowsAsync<UpdateToDatabaseWithoutIdException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task UpdateNotExisting_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapToDatabaseType(MapToDatabaseType.Update)
                .Finish()
            .Build()
            .MakeToDatabaseMapper();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        // assert
        await Assert.ThrowsAsync<UpdateToDatabaseWithoutRecordException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task UpdateDifferentConcurrencyToken_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<ToDatabaseEntity2, ToDatabaseEntity1>().Build().MakeToDatabaseMapper();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, new byte[] { 1, 2 }, 1));
            await databaseContext.SaveChangesAsync();
        });

        // assert
        await Assert.ThrowsAsync<ConcurrencyTokenException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task InsertExisting_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapToDatabaseType(MapToDatabaseType.Insert)
                .Finish()
            .Build()
            .MakeToDatabaseMapper();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, null, 1));
            await databaseContext.SaveChangesAsync();
        });

        // assert
        await Assert.ThrowsAsync<InsertToDatabaseWithExistingException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        }));
    }
}
