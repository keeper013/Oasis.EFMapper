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
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>();
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(null, null, 1);

        // assert
        await Assert.ThrowsAsync<UpdateToDatabaseWithoutIdException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, databaseContext, null, MapToDatabaseType.Update);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task UpdateNotExisting_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>();
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        // assert
        await Assert.ThrowsAsync<UpdateToDatabaseWithoutRecordException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, databaseContext, null, MapToDatabaseType.Update);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Theory]
    [InlineData(MapToDatabaseType.Update)]
    [InlineData(MapToDatabaseType.Upsert)]
    public async Task UpdateDifferentConcurrencyToken_ShouldFail(MapToDatabaseType mapToDatabaseType)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>();
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, new byte[] { 1, 2 }, 1));
            await databaseContext.SaveChangesAsync();
        });

        // assert
        await Assert.ThrowsAsync<ConcurrencyTokenException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, databaseContext, null, mapToDatabaseType);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task InsertExisting_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>();
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ToDatabaseEntity1>().Add(new ToDatabaseEntity1(1, null, 1));
            await databaseContext.SaveChangesAsync();
        });

        // assert
        await Assert.ThrowsAsync<InsertToDatabaseWithExistingException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, databaseContext, null, MapToDatabaseType.Insert);
            await databaseContext.SaveChangesAsync();
        }));
    }
}
