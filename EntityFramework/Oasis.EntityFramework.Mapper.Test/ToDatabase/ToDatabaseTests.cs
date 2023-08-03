namespace Oasis.EntityFramework.Mapper.Test.ToDatabase;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Threading.Tasks;
using NUnit.Framework;

public class ToDatabaseTests : TestBase
{
    [Test]
    public void UpdateNullId_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapToDatabaseType(MapToDatabaseType.Update)
                .Finish()
            .Build();
        var instance = new ToDatabaseEntity2(null, null, 1);

        // assert
        Assert.ThrowsAsync<UpdateToDatabaseWithoutIdException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null, databaseContext);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Test]
    public void UpdateNotExisting_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapToDatabaseType(MapToDatabaseType.Update)
                .Finish()
            .Build();
        var instance = new ToDatabaseEntity2(1, 1, 1);

        // assert
        Assert.ThrowsAsync<UpdateToDatabaseWithoutRecordException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null, databaseContext);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Test]
    [Ignore("Sqlite doesn't support concurrenty token, ignored here")]
    public async Task UpdateDifferentConcurrencyToken_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<ToDatabaseEntity2, ToDatabaseEntity1>().Build();
        var instance = new ToDatabaseEntity2(1, 1, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, 2, 1));
            await databaseContext.SaveChangesAsync();
        });

        // assert
        Assert.ThrowsAsync<ConcurrencyTokenException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null, databaseContext);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Test]
    public async Task InsertExisting_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapToDatabaseType(MapToDatabaseType.Insert)
                .Finish()
            .Build();
        var instance = new ToDatabaseEntity2(1, 1, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, null, 1));
            await databaseContext.SaveChangesAsync();
        });

        // assert
        Assert.ThrowsAsync<InsertToDatabaseWithExistingException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null, databaseContext);
            await databaseContext.SaveChangesAsync();
        }));
    }
}
