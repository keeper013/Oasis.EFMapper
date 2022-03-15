namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public sealed class PositiveTests : IDisposable
{
    private readonly DbContext _dbContext;

    public PositiveTests()
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
    public async Task MapScalarProperties_ValidProperties_ShouldBeMapped()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.RegisterTwoWay<ScalarEntity1, ScalarEntity2>();
        var mapper = mapperBuilder.Build();

        _dbContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", new byte[] { 2, 3, 4 }));
        await _dbContext.SaveChangesAsync();

        // act
        var entity = await _dbContext.Set<ScalarEntity1>().AsNoTracking().SingleAsync();
        var session1 = mapper.CreateMappingFromEntitiesSession();
        var instance = session1.Map<ScalarEntity1, ScalarEntity2>(entity);
        instance.IntProp = 1;
        instance.LongNullableProp = 2;
        instance.StringProp = "3";
        instance.ByteArrayProp = new byte[] { 1, 2, 3 };
        var session2 = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session2.MapAsync<ScalarEntity2, ScalarEntity1>(instance);

        // assert
        Assert.Equal(1, result.IntProp);
        Assert.Equal(2, result.LongNullableProp);
        Assert.Equal("3", result.StringProp);
        Assert.Equal(result.ByteArrayProp, instance.ByteArrayProp);
    }

    [Fact]
    public async Task MapScalarProperties_InvalidProperties_ShouldNotBeMapped()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.Register<ScalarEntity1, ScalarEntity3>();
        var mapper = mapperBuilder.Build();

        _dbContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 }));
        await _dbContext.SaveChangesAsync();

        // act
        var entity = await _dbContext.Set<ScalarEntity1>().AsNoTracking().SingleAsync();
        var session = mapper.CreateMappingFromEntitiesSession();
        var result = session.Map<ScalarEntity1, ScalarEntity3>(entity);

        // assert
        Assert.Null(result.IntProp);
        Assert.Equal(0, result.LongNullableProp);
        Assert.Null(result.StringProp1);
        Assert.Null(result.ByteArrayProp);
    }

    [Fact]
    public async Task MapListProperties_ICollection_NewElementShouldBeAdded()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<CollectionEntity1, CollectionEntity2>();

        var mapper = mapperBuilder.Build();

        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, null, "4", new byte[] { 2, 3, 4 });
        _dbContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { sc1_1, sc1_2 }));
        await _dbContext.SaveChangesAsync();

        // act
        var entity = await _dbContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        var session = mapper.CreateMappingFromEntitiesSession();
        var result = session.Map<CollectionEntity1, CollectionEntity2>(entity);

        // assert
        Assert.Equal(1, result.IntProp);
        Assert.NotNull(result.Scs);
        Assert.Equal(2, result.Scs!.Count);
        var item0 = result.Scs.ElementAt(0);
        Assert.Equal(1, item0.IntProp);
        Assert.Equal(2, item0.LongNullableProp);
        Assert.Equal("3", item0.StringProp);
        Assert.Equal(sc1_1.ByteArrayProp, item0.ByteArrayProp);
        var item1 = result.Scs.ElementAt(1);
        Assert.Equal(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.Equal("4", item1.StringProp);
        Assert.Equal(sc1_2.ByteArrayProp, item1.ByteArrayProp);
    }

    [Fact]
    public async Task MapListProperties_IList_ExistingElementShouldBeUpdated()
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
        var result2 = await session2.MapAsync<ListIEntity1, CollectionEntity1>(result1, c => c.Include(c => c.Scs));

        // assert
        Assert.Equal(2, result2.IntProp);
        Assert.NotNull(result2.Scs);
        Assert.Equal(1, result2.Scs!.Count);
        var item1 = result2.Scs.ElementAt(0);
        Assert.Equal(2, item1.IntProp);
        Assert.Equal(3, item1.LongNullableProp);
        Assert.Equal("4", item1.StringProp);
        Assert.Equal(result1.Scs!.ElementAt(0).ByteArrayProp, result2.Scs.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public async Task MapListProperties_List_ExcludedElementShouldBeDeleted()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.RegisterTwoWay<ListEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        _dbContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { sc1_1, sc1_2 }));
        await _dbContext.SaveChangesAsync();

        // act
        var entity = await _dbContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        var session1 = mapper.CreateMappingFromEntitiesSession();
        var result1 = session1.Map<CollectionEntity1, ListEntity1>(entity);
        result1.IntProp = 2;
        result1.Scs!.Remove(result1.Scs.ElementAt(1));
        var item0 = result1.Scs!.ElementAt(0);
        item0.IntProp = 2;
        item0.LongNullableProp = null;
        item0.StringProp = "4";
        item0.ByteArrayProp = new byte[] { 2 };
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result2 = await session.MapAsync<ListEntity1, CollectionEntity1>(result1, x => x.Include(x => x.Scs));

        // assert
        Assert.Equal(2, result2.IntProp);
        Assert.NotNull(result2.Scs);
        Assert.Equal(1, result2.Scs!.Count);
        var item1 = result2.Scs.ElementAt(0);
        Assert.Equal(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.Equal("4", item1.StringProp);
        Assert.Equal(result2.Scs!.ElementAt(0).ByteArrayProp, result1.Scs.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public async Task MapDerivedEntities_ShouldMapDerivedAndBase()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<DerivedEntity2, DerivedEntity1>();

        var mapper = mapperBuilder.Build();

        var instance = new DerivedEntity2("str2", 2, new List<ScalarEntity2> { new ScalarEntity2(1, 2, "3", new byte[] { 1 }) });

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.MapAsync<DerivedEntity2, DerivedEntity1>(instance, x => x.AsNoTracking().Include(x => x.Scs));

        // assert
        Assert.Equal("str2", result.StringProp);
        Assert.Equal(2, result.IntProp);
        Assert.NotNull(result.Scs);
        Assert.Single(result.Scs!);
        var item0 = result.Scs![0];
        Assert.Equal("3", item0.StringProp);
        Assert.Equal(1, item0.IntProp);
        Assert.Equal(2, item0.LongNullableProp);
        Assert.Equal(item0.ByteArrayProp, instance.Scs!.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public async Task HiddingPublicProperty_HiddenMemberIgnored()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<DerivedEntity2_2, DerivedEntity1_1>();

        var mapper = mapperBuilder.Build();

        var instance = new DerivedEntity2_2(2, 2, new List<ScalarEntity2> { new ScalarEntity2(1, 2, "3", new byte[] { 1 }) });

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.MapAsync<DerivedEntity2_2, DerivedEntity1_1>(instance, x => x.AsNoTracking().Include(x => x.Scs));

        // assert
        Assert.Equal(2, result.IntProp);
        Assert.Equal(0, ((BaseEntity1)result).IntProp);
        Assert.NotNull(result.Scs);
        Assert.Single(result.Scs!);
        var item0 = result.Scs![0];
        Assert.Equal("3", item0.StringProp);
        Assert.Equal(1, item0.IntProp);
        Assert.Equal(2, item0.LongNullableProp);
        Assert.Equal(item0.ByteArrayProp, instance.Scs!.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public async Task ConvertWithLambdaExpressionScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder
            .WithScalarMapper((ByteArrayWrapper wrapper) => wrapper.Bytes)
            .WithScalarMapper((byte[] array) => new ByteArrayWrapper(array))
            .RegisterTwoWay<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        _dbContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, null, "abc", new byte[] { 1, 2, 3 }));
        await _dbContext.SaveChangesAsync();

        // act
        var entity = await _dbContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        var session1 = mapper.CreateMappingFromEntitiesSession();
        var result1 = session1.Map<ScalarEntity1, ScalarEntity4>(entity);
        result1.ByteArrayProp = new ByteArrayWrapper(new byte[] { 2, 3, 4 });
        var session2 = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result2 = await session2.MapAsync<ScalarEntity4, ScalarEntity1>(result1);

        // assert
        Assert.True(Enumerable.SequenceEqual(result1.ByteArrayProp!.Bytes, result2.ByteArrayProp!));
    }

    [Fact]
    public async Task ConvertWithStaticScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder
            .WithScalarMapper((ByteArrayWrapper wrapper) => ByteArrayWrapper.ConvertStatic(wrapper))
            .WithScalarMapper((byte[] array) => ByteArrayWrapper.ConvertStatic(array))
            .RegisterTwoWay<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        _dbContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, null, "abc", new byte[] { 1, 2, 3 }));
        await _dbContext.SaveChangesAsync();

        // act
        var entity = await _dbContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        var session1 = mapper.CreateMappingFromEntitiesSession();
        var result1 = session1.Map<ScalarEntity1, ScalarEntity4>(entity);
        result1.ByteArrayProp = new ByteArrayWrapper(new byte[] { 2, 3, 4 });
        var session2 = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result2 = await session2.MapAsync<ScalarEntity4, ScalarEntity1>(result1);

        // assert
        Assert.True(Enumerable.SequenceEqual(result1.ByteArrayProp!.Bytes, result2.ByteArrayProp!));
    }

    [Fact]
    public async Task MapRecursiveEntity_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.Register<RecursiveEntity2, RecursiveEntity1>();
        var mapper = mapperBuilder.Build();

        var instance = new RecursiveEntity2(1);
        var additionalInstance = new RecursiveEntity2(2);
        instance.SubItems = new List<RecursiveEntity2> { instance, additionalInstance };

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.MapAsync<RecursiveEntity2, RecursiveEntity1>(instance, x => x.AsNoTracking());

        // assert
        Assert.Equal(1, result.IntProp);
        Assert.Equal(2, result.SubItems!.Count);
        Assert.Equal(1, result.SubItems[0].IntProp);
        Assert.Equal(2, result.SubItems[1].IntProp);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
