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
        var customConfig = factory.MakeCustomTypeMapperBuilder<ToDatabaseEntity2, ToDatabaseEntity1>().SetMapToDatabaseType(MapToDatabaseType.Update).Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>(customConfig);
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(null, null, 1);

        // assert
        await Assert.ThrowsAsync<UpdateToDatabaseWithoutIdException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null, databaseContext);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task UpdateNotExisting_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var customBuilder = factory.MakeCustomTypeMapperBuilder<ToDatabaseEntity2, ToDatabaseEntity1>().SetMapToDatabaseType(MapToDatabaseType.Update).Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>(customBuilder);
        var mapper = mapperBuilder.Build();
        var instance = new ToDatabaseEntity2(1, new byte[] { 1, 2, 3 }, 1);

        // assert
        await Assert.ThrowsAsync<UpdateToDatabaseWithoutRecordException>(async () => await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null, databaseContext);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task UpdateDifferentConcurrencyToken_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
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
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null, databaseContext);
            await databaseContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task InsertExisting_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var customConfig = factory.MakeCustomTypeMapperBuilder<ToDatabaseEntity2, ToDatabaseEntity1>().SetMapToDatabaseType(MapToDatabaseType.Insert).Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.Register<ToDatabaseEntity2, ToDatabaseEntity1>(customConfig);
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
            var entity = await mapper.MapAsync<ToDatabaseEntity2, ToDatabaseEntity1>(instance, null, databaseContext);
            await databaseContext.SaveChangesAsync();
        }));
    }
}
