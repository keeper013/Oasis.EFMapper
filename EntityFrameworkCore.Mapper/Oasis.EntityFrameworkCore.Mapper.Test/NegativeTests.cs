namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public sealed class NegativeTests : IDisposable
{
    private readonly DbContext _dbContext;

    public NegativeTests()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(connection)
            .Options;
        _dbContext = new DatabaseContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task MapListProperties_UpdateNonExistingNavitation_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.RegisterTwoWay<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var sub = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        _dbContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { sub }));
        await _dbContext.SaveChangesAsync();

        // act
        var entity = await _dbContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        var session1 = mapper.CreateMappingFromEntitiesSession();
        var result1 = session1.Map<CollectionEntity1, ListIEntity1>(entity);
        var item0 = result1.Scs![0];
        item0.Id = item0.Id + 1;
        item0.IntProp = 3;

        // assert
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
        {
            var session2 = mapper.CreateMappingToEntitiesSession(_dbContext);
            await session2.MapAsync<ListIEntity1, CollectionEntity1>(result1, x => x.Include(x => x.Scs));
        });
    }

    [Fact]
    public void ConvertWithDuplicatedScalarMapper_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        Assert.Throws<ScalarMapperExistsException>(() => mapperBuilder
            .WithScalarMapper<ByteArrayWrapper, byte[]>((wrapper) => ByteArrayWrapper.ConvertStatic(wrapper))
            .WithScalarMapper<ByteArrayWrapper, byte[]>((wrapper) => wrapper.Bytes));
    }

    [Fact]
    public async Task ConvertWithoutStaticScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.Register<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        _dbContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 }));
        await _dbContext.SaveChangesAsync();

        // act
        var entity = await _dbContext.Set<ScalarEntity1>().FirstAsync();
        var session = mapper.CreateMappingFromEntitiesSession();
        var result = session.Map<ScalarEntity1, ScalarEntity4>(entity);

        // assert
        Assert.Null(result.ByteArrayProp);
    }

    [Fact]
    public async Task MapListProperties_HasAsNoTrackingInIncluder_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.RegisterTwoWay<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var subInstance = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        _dbContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { subInstance }));
        await _dbContext.SaveChangesAsync();

        // act
        var entity = await _dbContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        var session1 = mapper.CreateMappingFromEntitiesSession();
        var result1 = session1.Map<CollectionEntity1, ListIEntity1>(entity);
        result1.IntProp = 2;
        var item0 = result1.Scs![0];
        item0.IntProp = 2;
        item0.LongNullableProp = 3;
        item0.StringProp = "4";
        item0.ByteArrayProp = new byte[] { 2 };
        var session2 = mapper.CreateMappingToEntitiesSession(_dbContext);
        await Assert.ThrowsAsync<AsNoTrackingNotAllowedException>(
            async () => await session2.MapAsync<ListIEntity1, CollectionEntity1>(result1, c => c.AsNoTracking().Include(c => c.Scs)));
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
