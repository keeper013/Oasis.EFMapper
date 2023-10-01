namespace Oasis.EntityFramework.Mapper.Test.ToDatabase;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Threading.Tasks;
using NUnit.Framework;

public class MapTypeTests : TestBase
{
    [TestCase(MapType.Insert)]
    [TestCase(MapType.Update)]
    [TestCase(MapType.Upsert)]
    public void UpdateToMemoryOnly_ShouldFail(MapType mapType)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity1, ToDatabaseEntity2>()
                .SetMapType(mapType)
                .Finish()
            .Build()
            .MakeToMemoryMapper();
        var instance = new ToDatabaseEntity1(1, new byte[] { 1, 2, 3 }, 1);

        // assert
        Assert.Throws<UnregisteredMappingException>(() => mapper.Map<ToDatabaseEntity1, ToDatabaseEntity2>(instance));
    }

    [Test]
    public void MemoryMapToDatabaseOnly_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapType(MapType.Memory)
                .Finish()
            .Build()
            .MakeToDatabaseMapper();
        var instance = new ToDatabaseEntity2(null, new byte[] { 1, 2, 3 }, 1);

        // assert
        Assert.ThrowsAsync<UnregisteredMappingException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null);
        }));
    }

    [Test]
    public void UpdateNullId_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapType(MapType.Update)
                .Finish()
            .Build()
            .MakeToDatabaseMapper();
        var instance = new ToDatabaseEntity2(null, new byte[] { 1, 2, 3 }, 1);

        // assert
        Assert.ThrowsAsync<UpdateToDatabaseWithoutIdException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Test]
    public void UpdateNotExisting_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapType(MapType.Update)
                .Finish()
            .Build()
            .MakeToDatabaseMapper();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        // assert
        Assert.ThrowsAsync<UpdateToDatabaseWithoutRecordException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Test]
    public async Task UpdateDifferentConcurrencyToken_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<ToDatabaseEntity2, ToDatabaseEntity1>().Build().MakeToDatabaseMapper();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, new byte[] { 2, 3, 4 }, 1));
            await databaseContext.SaveChangesAsync();
        });

        // assert
        Assert.ThrowsAsync<ConcurrencyTokenException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Test]
    public async Task InsertExisting_ShouldFail()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ToDatabaseEntity2, ToDatabaseEntity1>()
                .SetMapType(MapType.Insert)
                .Finish()
            .Build()
            .MakeToDatabaseMapper();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, new byte[] { 1, 2, 3 }, 1));
            await databaseContext.SaveChangesAsync();
        });

        // assert
        Assert.ThrowsAsync<InsertToDatabaseWithExistingException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        }));
    }
}
