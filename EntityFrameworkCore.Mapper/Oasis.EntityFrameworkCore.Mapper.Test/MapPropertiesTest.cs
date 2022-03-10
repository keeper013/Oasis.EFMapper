﻿namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class MapPropertiesTest : IDisposable
{
    private readonly DbContext _dbContext;

    public MapPropertiesTest()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(this.GetType().Name)
            .Options;
        _dbContext = new DatabaseContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void MapScalarProperties_ValidProperties_ShouldBeMapped()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(this.GetType().Name);
        mapperBuilder.Register<ScalarClass1, ScalarClass2>();
        var mapper = mapperBuilder.Build();

        var instance1 = new ScalarClass1(1, 1, 2, "3", new byte[] { 1, 2, 3 });
        var instance2 = new ScalarClass2(1);

        mapper.Map(instance1, instance2, _dbContext);

        // assert
        Assert.Equal(1, instance2.IntProp);
        Assert.Equal(2, instance2.LongNullableProp);
        Assert.Equal("3", instance2.StringProp);
        Assert.Equal(instance1.ByteArrayProp, instance2.ByteArrayProp);
    }

    [Fact]
    public void MapScalarProperties_InvalidProperties_ShouldNotBeMapped()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(this.GetType().Name);
        mapperBuilder.Register<ScalarClass3, ScalarClass2>();
        var mapper = mapperBuilder.Build();

        var instance3 = new ScalarClass3(1, 1, 2, "3", new char[] { 'a', 'b', 'c' });
        var instance2 = new ScalarClass2(1);

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
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(this.GetType().Name);

        // TODO: shouldn't need to call this when cascade register is implemented
        mapperBuilder.Register<ScalarClass2, ScalarClass1>();
        mapperBuilder.Register<CollectionClass2, CollectionClass1>();

        var mapper = mapperBuilder.Build();

        var cc1 = new CollectionClass1(1);
        var cc2 = new CollectionClass2(
            1,
            1,
            new List<ScalarClass2>
            {
                new ScalarClass2(null, 1, 2, "3", new byte[] { 1 }),
                new ScalarClass2(null, 2, null, "4", new byte[] { 2, 3, 4 }),
            });

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
        var mapperBuilder = factory.Make(this.GetType().Name);

        // TODO: shouldn't need to call this when cascade register is implemented
        mapperBuilder.Register<ScalarClass2, ScalarClass1>();
        mapperBuilder.Register<ListIClass1, CollectionClass1>();

        var mapper = mapperBuilder.Build();

        var cc1 = new CollectionClass1(
            1,
            1,
            new List<ScalarClass1> { new ScalarClass1(1, 1, 2, "3", new byte[] { 1 }), });
        var lic1 = new ListIClass1(
            1,
            2,
            new List<ScalarClass2> { new ScalarClass2(1, 2, 3, "4", new byte[] { 2 }), });

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
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(this.GetType().Name);

        // TODO: shouldn't need to call this when cascade register is implemented
        mapperBuilder.Register<ScalarClass2, ScalarClass1>();
        mapperBuilder.Register<ListClass1, CollectionClass1>();

        var mapper = mapperBuilder.Build();

        var sc1_1 = new ScalarClass1(1, 1, 2, "3", new byte[] { 1 });
        var sc1_2 = new ScalarClass1(2, 1, 2, "3", new byte[] { 1 });
        var cc1 = new CollectionClass1(1, 1, new List<ScalarClass1> { sc1_1, sc1_2 });
        var sc2 = new ScalarClass2(2, 2, 3, "4", new byte[] { 2 });
        var lc1 = new ListClass1(1, 2, new List<ScalarClass2> { sc2 });

        _dbContext.Set<ScalarClass1>().Add(sc1_1);
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

    public void Dispose() => _dbContext.Dispose();
}
