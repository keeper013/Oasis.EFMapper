namespace Oasis.EntityFrameworkCore.Mapper.Test;

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
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(GetType().Name)
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
        mapperBuilder.Register<ScalarEntity2, ScalarEntity1>();
        var mapper = mapperBuilder.Build();

        var instance = new ScalarEntity2(1, 1, 2, "3", new byte[] { 1, 2, 3 });
        _dbContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1));
        await _dbContext.SaveChangesAsync();

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.Map<ScalarEntity2, ScalarEntity1>(instance, x => x.AsNoTracking());

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
        mapperBuilder.Register<ScalarEntity3, ScalarEntity1>();
        var mapper = mapperBuilder.Build();

        var instance = new ScalarEntity3(1, 1, 2, "3", new char[] { 'a', 'b', 'c' });
        _dbContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1));
        await _dbContext.SaveChangesAsync();

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.Map<ScalarEntity3, ScalarEntity1>(instance, x => x.AsNoTracking());

        // assert
        Assert.Equal(0, result.IntProp);
        Assert.Null(result.LongNullableProp);
        Assert.Null(result.StringProp);
        Assert.Null(result.ByteArrayProp);
    }

    [Fact]
    public async Task MapListProperties_ICollection_NewElementShouldBeAdded()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<CollectionEntity2, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var sc2_1 = new ScalarEntity2(null, 1, 2, "3", new byte[] { 1 });
        var sc2_2 = new ScalarEntity2(null, 2, null, "4", new byte[] { 2, 3, 4 });
        var instance = new CollectionEntity2(1, 1, new List<ScalarEntity2> { sc2_1, sc2_2 });
        _dbContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1));
        await _dbContext.SaveChangesAsync();

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.Map<CollectionEntity2, CollectionEntity1>(instance, x => x.AsNoTracking().Include(x => x.Scs));

        // assert
        Assert.Equal(1, result.IntProp);
        Assert.NotNull(result.Scs);
        Assert.Equal(2, result.Scs!.Count);
        var item0 = result.Scs.ElementAt(0);
        Assert.Equal(1, item0.IntProp);
        Assert.Equal(2, item0.LongNullableProp);
        Assert.Equal("3", item0.StringProp);
        Assert.Equal(instance.Scs!.ElementAt(0).ByteArrayProp, result.Scs.ElementAt(0).ByteArrayProp);
        var item1 = result.Scs.ElementAt(1);
        Assert.Equal(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.Equal("4", item1.StringProp);
        Assert.Equal(instance.Scs!.ElementAt(1).ByteArrayProp, result.Scs.ElementAt(1).ByteArrayProp);
    }

    [Fact]
    public async Task MapListProperties_IList_ExistingElementShouldBeUpdated()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var instance = new ListIEntity1(1, 2, new List<SubScalarEntity1> { new SubScalarEntity1(1, 2, 3, "4", new byte[] { 2 }) });
        _dbContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, 1, new List<SubScalarEntity1>()));
        var subInstance = new SubScalarEntity1(1, 1, 2, "3", new byte[] { 1 });
        subInstance.CollectionEntityId = 1;
        _dbContext.Set<SubScalarEntity1>().Add(subInstance);
        await _dbContext.SaveChangesAsync();

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.Map<ListIEntity1, CollectionEntity1>(instance, x => x.AsNoTracking().Include(x => x.Scs));

        // assert
        Assert.Equal(2, result.IntProp);
        Assert.NotNull(result.Scs);
        Assert.Equal(1, result.Scs!.Count);
        var item0 = result.Scs.ElementAt(0);
        Assert.Equal(2, item0.IntProp);
        Assert.Equal(3, item0.LongNullableProp);
        Assert.Equal("4", item0.StringProp);
        Assert.Equal(instance.Scs!.ElementAt(0).ByteArrayProp, result.Scs.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public async Task MapListProperties_List_ExcludedElementShouldBeDeleted()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<ListEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var sc2 = new SubScalarEntity1(2, 2, 3, "4", new byte[] { 2 });
        var instance = new ListEntity1(1, 2, new List<SubScalarEntity1> { sc2 });
        var sc1_1 = new SubScalarEntity1(1, 1, 2, "3", new byte[] { 1 });
        sc1_1.CollectionEntityId = 1;
        var sc1_2 = new SubScalarEntity1(2, 1, 2, "3", new byte[] { 1 });
        sc1_2.CollectionEntityId = 1;
        _dbContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, 1, new List<SubScalarEntity1>()));
        _dbContext.Set<SubScalarEntity1>().Add(sc1_1);
        _dbContext.Set<SubScalarEntity1>().Add(sc1_2);
        await _dbContext.SaveChangesAsync();

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.Map<ListEntity1, CollectionEntity1>(instance, x => x.AsNoTracking().Include(x => x.Scs));

        Assert.Equal(2, result.IntProp);
        Assert.NotNull(result.Scs);
        Assert.Equal(1, result.Scs!.Count);
        var item0 = result.Scs.ElementAt(0);
        Assert.Equal(2, item0.Id);
        Assert.Equal(2, item0.IntProp);
        Assert.Equal(3, item0.LongNullableProp);
        Assert.Equal("4", item0.StringProp);
        Assert.Equal(instance.Scs!.ElementAt(0).ByteArrayProp, result.Scs.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public async Task MapDerivedEntities_ShouldMapDerivedAndBase()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<DerivedEntity2, DerivedEntity1>();

        var mapper = mapperBuilder.Build();

        var instance = new DerivedEntity2(null, "str2", 2, new List<ScalarEntity2> { new ScalarEntity2(null, 1, 2, "3", new byte[] { 1 }) });

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.Map<DerivedEntity2, DerivedEntity1>(instance, x => x.AsNoTracking().Include(x => x.Scs));

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

        var instance = new DerivedEntity2_2(null, 2, 2, new List<ScalarEntity2> { new ScalarEntity2(null, 1, 2, "3", new byte[] { 1 }) });

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.Map<DerivedEntity2_2, DerivedEntity1_1>(instance, x => x.AsNoTracking().Include(x => x.Scs));

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
    public async Task ConvertWithScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder
            .WithScalarMapper<ByteArrayWrapper, byte[]>(ByteArrayWrapper.ConvertStatic)
            .WithScalarMapper<byte[], ByteArrayWrapper>(ByteArrayWrapper.ConvertStatic)
            .Register<ScalarEntity4, ScalarEntity1>()
            .Register<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        var instance = new ScalarEntity4(1, new byte[] { 1, 2, 3 });
        _dbContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1));
        await _dbContext.SaveChangesAsync();

        // act
        var session1 = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result1 = await session1.Map<ScalarEntity4, ScalarEntity1>(instance, x => x.AsNoTracking());
        var session2 = mapper.CreateMappingFromEntitiesSession();
        var result2 = session2.Map<ScalarEntity1, ScalarEntity4>(result1);

        // assert
        Assert.True(Enumerable.SequenceEqual(result1.ByteArrayProp!, instance.ByteArrayProp!.Bytes));
        Assert.True(Enumerable.SequenceEqual(result2.ByteArrayProp!.Bytes, result1.ByteArrayProp!));
    }

    [Fact]
    public async Task MapRecursiveEntity_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.Register<RecursiveEntity2, RecursiveEntity1>();
        var mapper = mapperBuilder.Build();

        var instance = new RecursiveEntity2(null, 1);
        var additionalInstance = new RecursiveEntity2(null, 2);
        instance.SubItems = new List<RecursiveEntity2> { instance, additionalInstance };

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.Map<RecursiveEntity2, RecursiveEntity1>(instance, x => x.AsNoTracking());

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
