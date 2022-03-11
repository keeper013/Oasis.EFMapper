namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public void MapScalarProperties_ValidProperties_ShouldBeMapped()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.Register<ScalarClass2, ScalarClass1>();
        var mapper = mapperBuilder.Build();

        var instance1 = new ScalarClass1(1);
        var instance2 = new ScalarClass2(1, 1, 2, "3", new byte[] { 1, 2, 3 });

        // act
        mapper.Map(instance2, instance1, _dbContext);

        // assert
        Assert.Equal(1, instance1.IntProp);
        Assert.Equal(2, instance1.LongNullableProp);
        Assert.Equal("3", instance1.StringProp);
        Assert.Equal(instance1.ByteArrayProp, instance2.ByteArrayProp);
    }

    [Fact]
    public void MapScalarProperties_InvalidProperties_ShouldNotBeMapped()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.Register<ScalarClass3, ScalarClass2>();
        var mapper = mapperBuilder.Build();

        var instance3 = new ScalarClass3(1, 1, 2, "3", new char[] { 'a', 'b', 'c' });
        var instance2 = new ScalarClass2(1);

        // act
        mapper.Map(instance3, instance2, _dbContext);

        // assert
        Assert.Equal(0, instance2.IntProp);
        Assert.Null(instance2.LongNullableProp);
        Assert.Null(instance2.StringProp);
        Assert.Null(instance2.ByteArrayProp);
    }

    [Fact]
    public void MapListProperties_ICollection_NewElementShouldBeAdded()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        // TODO: shouldn't need to call this when cascade register is implemented
        mapperBuilder
            .Register<ScalarClass2, ScalarClass1>()
            .Register<CollectionClass2, CollectionClass1>();

        var mapper = mapperBuilder.Build();

        var cc1 = new CollectionClass1(1);
        var sc2_1 = new ScalarClass2(null, 1, 2, "3", new byte[] { 1 });
        var sc2_2 = new ScalarClass2(null, 2, null, "4", new byte[] { 2, 3, 4 });
        var cc2 = new CollectionClass2(1, 1, new List<ScalarClass2> { sc2_1, sc2_2 });

        // act
        mapper.Map(cc2, cc1, _dbContext);

        Assert.Equal(1, cc1.IntProp);
        Assert.NotNull(cc1.Scs);
        Assert.Equal(2, cc1.Scs!.Count);
        var item0 = cc1.Scs.ElementAt(0);
        Assert.Equal(1, item0.IntProp);
        Assert.Equal(2, item0.LongNullableProp);
        Assert.Equal("3", item0.StringProp);
        Assert.Equal(cc2.Scs!.ElementAt(0).ByteArrayProp, cc1.Scs.ElementAt(0).ByteArrayProp);
        var item1 = cc1.Scs.ElementAt(1);
        Assert.Equal(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.Equal("4", item1.StringProp);
        Assert.Equal(cc2.Scs!.ElementAt(1).ByteArrayProp, cc1.Scs.ElementAt(1).ByteArrayProp);
    }

    [Fact]
    public void MapListProperties_IList_ExistingElementShouldBeUpdated()
    {
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        // TODO: shouldn't need to call this when cascade register is implemented
        mapperBuilder
            .Register<ScalarClass2, ScalarClass1>()
            .Register<ListIClass1, CollectionClass1>();

        var mapper = mapperBuilder.Build();

        var cc1 = new CollectionClass1(1, 1, new List<ScalarClass1> { new ScalarClass1(1, 1, 2, "3", new byte[] { 1 }) });
        var lic1 = new ListIClass1(1, 2, new List<ScalarClass2> { new ScalarClass2(1, 2, 3, "4", new byte[] { 2 }) });

        mapper.Map(lic1, cc1, _dbContext);

        Assert.Equal(2, cc1.IntProp);
        Assert.NotNull(cc1.Scs);
        Assert.Equal(1, cc1.Scs!.Count);
        var item0 = cc1.Scs.ElementAt(0);
        Assert.Equal(2, item0.IntProp);
        Assert.Equal(3, item0.LongNullableProp);
        Assert.Equal("4", item0.StringProp);
        Assert.Equal(lic1.Scs!.ElementAt(0).ByteArrayProp, cc1.Scs.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public void MapListProperties_List_ExcludedElementShouldBeDeleted()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        // TODO: shouldn't need to call this when cascade register is implemented
        mapperBuilder
            .Register<ScalarClass2, ScalarClass1>()
            .Register<ListClass1, CollectionClass1>();

        var mapper = mapperBuilder.Build();

        var sc1_1 = new ScalarClass1(1, 1, 2, "3", new byte[] { 1 });
        var sc1_2 = new ScalarClass1(2, 1, 2, "3", new byte[] { 1 });
        var cc1 = new CollectionClass1(1, 1, new List<ScalarClass1> { sc1_1, sc1_2 });
        var sc2 = new ScalarClass2(2, 2, 3, "4", new byte[] { 2 });
        var lc1 = new ListClass1(1, 2, new List<ScalarClass2> { sc2 });

        // act
        mapper.Map(lc1, cc1, _dbContext);

        Assert.Equal(2, cc1.IntProp);
        Assert.NotNull(cc1.Scs);
        Assert.Equal(1, cc1.Scs!.Count);
        var item0 = cc1.Scs.ElementAt(0);
        Assert.Equal(2, item0.Id);
        Assert.Equal(2, item0.IntProp);
        Assert.Equal(3, item0.LongNullableProp);
        Assert.Equal("4", item0.StringProp);
        Assert.Equal(lc1.Scs!.ElementAt(0).ByteArrayProp, cc1.Scs.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public void MapDerivedEntities_ShouldMapDerivedAndBase()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        // TODO: shouldn't need to call this when cascade register is implemented
        mapperBuilder
            .Register<ScalarClass2, ScalarClass1>()
            .Register<DerivedEntity2, DerivedEntity1>();

        var mapper = mapperBuilder.Build();

        var de1 = new DerivedEntity1();
        var de2 = new DerivedEntity2(null, "str2", 2, new List<ScalarClass2> { new ScalarClass2(null, 1, 2, "3", new byte[] { 1 }) });

        // act
        mapper.Map(de2, de1, _dbContext);

        // assert
        Assert.Equal("str2", de1.StringProp);
        Assert.Equal(2, de1.IntProp);
        Assert.NotNull(de1.Scs);
        Assert.Single(de1.Scs!);
        var item0 = de1.Scs![0];
        Assert.Equal("3", item0.StringProp);
        Assert.Equal(1, item0.IntProp);
        Assert.Equal(2, item0.LongNullableProp);
        Assert.Equal(item0.ByteArrayProp, de2.Scs!.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public void HiddingPublicProperty_HiddenMemberIgnored()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        // TODO: shouldn't need to call this when cascade register is implemented
        mapperBuilder
            .Register<ScalarClass2, ScalarClass1>()
            .Register<DerivedEntity2_2, DerivedEntity1_1>();

        var mapper = mapperBuilder.Build();

        var de1 = new DerivedEntity1_1();
        var de2 = new DerivedEntity2_2(null, 2, 2, new List<ScalarClass2> { new ScalarClass2(null, 1, 2, "3", new byte[] { 1 }) });

        // act
        mapper.Map(de2, de1, _dbContext);

        // assert
        Assert.Equal(2, de1.IntProp);
        Assert.Equal(0, ((BaseEntity1)de1).IntProp);
        Assert.NotNull(de1.Scs);
        Assert.Single(de1.Scs!);
        var item0 = de1.Scs![0];
        Assert.Equal("3", item0.StringProp);
        Assert.Equal(1, item0.IntProp);
        Assert.Equal(2, item0.LongNullableProp);
        Assert.Equal(item0.ByteArrayProp, de2.Scs!.ElementAt(0).ByteArrayProp);
    }

    [Fact]
    public void ConvertWithScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder
            .WithScalarMapper<ByteArrayWrapper, byte[]>(ByteArrayWrapper.ConvertStatic)
            .WithScalarMapper<byte[], ByteArrayWrapper>(ByteArrayWrapper.ConvertStatic)
            .Register<ScalarClass4, ScalarClass1>()
            .Register<ScalarClass1, ScalarClass4>();

        var mapper = mapperBuilder.Build();

        var instance1 = new ScalarClass1(1);
        var instance2 = new ScalarClass4(new byte[] { 1, 2, 3 });
        var instance3 = new ScalarClass4();

        // act
        mapper.Map(instance2, instance1, _dbContext);
        mapper.Map(instance1, instance3, _dbContext);

        // assert
        Assert.True(Enumerable.SequenceEqual(instance1.ByteArrayProp!, instance2.ByteArrayProp!.Bytes));
        Assert.True(Enumerable.SequenceEqual(instance3.ByteArrayProp!.Bytes, instance1.ByteArrayProp!));
    }

    public void Dispose() => _dbContext.Dispose();
}
