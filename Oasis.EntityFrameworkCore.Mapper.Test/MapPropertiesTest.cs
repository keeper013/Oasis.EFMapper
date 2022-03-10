namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class MapPropertiesTest
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
        mapperBuilder.Register<CollectionClass1, CollectionClass2>();

        // TODO: shouldn't need to call this when cascade register is implemented
        // TODO: should work for ScalarClass1 to map ScalarClass2
        mapperBuilder.Register<ScalarClass1, ScalarClass1>();
        var mapper = mapperBuilder.Build();

        var cc1 = new CollectionClass1(1, 1,
            new List<ScalarClass1>
            {
                new ScalarClass1(null, 1, 2, "3", new byte[] { 1 }),
                new ScalarClass1(null, 2, null, "4", new byte[] { 2, 3, 4 })
            });
        var cc2 = new CollectionClass2(1);

        mapper.Map(cc1, cc2, _dbContext);

        Assert.Equal(1, cc2.IntProp);
        Assert.NotNull(cc2.Scs1);
        Assert.Equal(2, cc2.Scs1!.Count);
        var item0 = cc2.Scs1.ElementAt(0);
        Assert.Equal(1, item0.IntProp);
        Assert.Equal(2, item0.LongNullableProp);
        Assert.Equal("3", item0.StringProp);
        Assert.Equal(cc1.Scs1!.ElementAt(0).ByteArrayProp, cc2.Scs1.ElementAt(0).ByteArrayProp);
        var item1 = cc2.Scs1.ElementAt(1);
        Assert.Equal(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.Equal("4", item1.StringProp);
        Assert.Equal(cc1.Scs1!.ElementAt(1).ByteArrayProp, cc2.Scs1.ElementAt(1).ByteArrayProp);
    }
}

public sealed class ScalarClass1 : EntityBase
{
    public ScalarClass1()
    {
    }

    public ScalarClass1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarClass1(long? id, int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        LongNullableProp = longNullableProp;
        StringProp = stringProp;
        ByteArrayProp = byteArrayProp;
    }

    public int IntProp { get; set; }

    public long? LongNullableProp { get; set; }

    public string? StringProp { get; set; }

    public byte[]? ByteArrayProp { get; set; }
}

public sealed class ScalarClass2 : EntityBase
{
    public ScalarClass2()
    {
    }

    public ScalarClass2(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarClass2(long? id, int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        LongNullableProp = longNullableProp;
        StringProp = stringProp;
        ByteArrayProp = byteArrayProp;
    }

    public int IntProp { get; set; }

    public long? LongNullableProp { get; set; }

    public string? StringProp { get; set; }

    public byte[]? ByteArrayProp { get; set; }
}

public sealed class ScalarClass3 : EntityBase
{
    public ScalarClass3()
    {
    }

    public ScalarClass3(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarClass3(long? id, int? intProp, long longNullableProp, string stringProp, char[] byteArrayProp)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        LongNullableProp = longNullableProp;
        StringProp1 = stringProp;
        ByteArrayProp = byteArrayProp;
    }

    public int? IntProp { get; set; }

    public long LongNullableProp { get; set; }

    public string? StringProp1 { get; set; }

    public char[]? ByteArrayProp { get; set; }
}

public sealed class CollectionClass1 : EntityBase
{
    public CollectionClass1()
    {
    }

    public CollectionClass1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public CollectionClass1(long? id, int intProp, ICollection<ScalarClass1> scs1)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs1 = scs1;
    }

    public int IntProp { get; set; }

    public ICollection<ScalarClass1>? Scs1 { get; set; }
}

public sealed class CollectionClass2 : EntityBase
{
    public CollectionClass2()
    {
    }

    public CollectionClass2(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public CollectionClass2(long? id, int intProp, ICollection<ScalarClass1> scs1)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs1 = scs1;
    }

    public int IntProp { get; set; }

    public ICollection<ScalarClass1>? Scs1 { get; set; }
}
