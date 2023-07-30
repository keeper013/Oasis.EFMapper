namespace Oasis.EntityFramework.Mapper.Test.ToDatabase;

using NUnit.Framework;
using Oasis.EntityFramework.Mapper.Exceptions;
using System.Threading.Tasks;

[TestFixture]
public class ToDatabaseTests : TestBase
{
    [Test]
    public void UpdateNullId_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(DefaultConfiguration);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>();
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(null, null, 1);

        // assert
        Assert.ThrowsAsync<UpdateToDatabaseWithoutIdException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, databaseContext, null, MapToDatabaseType.Update);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Ignore("Sqlite doesn't support concurrenty token, ignored here")]
    [TestCase(MapToDatabaseType.Update)]
    [TestCase(MapToDatabaseType.Upsert)]
    public async Task UpdateDifferentConcurrencyToken_ShouldFail(MapToDatabaseType mapToDatabaseType)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(DefaultConfiguration);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>();
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(1, 1, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, 2, 1));
            await databaseContext.SaveChangesAsync();
        });

        // assert
        Assert.ThrowsAsync<ConcurrencyTokenException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, databaseContext, null, mapToDatabaseType);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Test]
    public void UpdateNotExisting_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(DefaultConfiguration);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>();
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(1, 1, 1);

        // act
        Assert.ThrowsAsync<UpdateToDatabaseWithoutRecordException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, databaseContext, null, MapToDatabaseType.Update);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Test]
    public async Task InsertExisting_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(DefaultConfiguration);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>();
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(1, 1, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, null, 1));
            await databaseContext.SaveChangesAsync();
        });

        // act
        Assert.ThrowsAsync<InsertToDatabaseWithExistingException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, databaseContext, null, MapToDatabaseType.Insert);
            await databaseContext.SaveChangesAsync();
        }));
    }
}
