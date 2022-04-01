namespace Oasis.EntityFrameworkCore.Mapper.Test.KeyPropertyType;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

public class KeyPropertyTypeTests : TestBase
{
    [Fact]
    public async Task TestByte()
    {
        await Test<byte>();
    }

    [Fact]
    public async Task TestNByte()
    {
        await Test<byte?>();
    }

    [Fact]
    public async Task TestShort()
    {
        await Test<short>();
    }

    [Fact]
    public async Task TestNShort()
    {
        await Test<short?>();
    }

    [Fact]
    public async Task TestUShort()
    {
        await Test<ushort>();
    }

    [Fact]
    public async Task TestNUShort()
    {
        await Test<ushort?>();
    }

    [Fact]
    public async Task TestInt()
    {
        await Test<int>();
    }

    [Fact]
    public async Task TestNInt()
    {
        await Test<int?>();
    }

    [Fact]
    public async Task TestUInt()
    {
        await Test<uint>();
    }

    [Fact]
    public async Task TestNUInt()
    {
        await Test<uint?>();
    }

    [Fact]
    public async Task TestLong()
    {
        await Test<long>();
    }

    [Fact]
    public async Task TestNLong()
    {
        await Test<long?>();
    }

    [Fact]
    public async Task TestULong()
    {
        await Test<ulong>();
    }

    [Fact]
    public async Task TestNULong()
    {
        await Test<ulong?>();
    }

    [Fact]
    public async Task TestString()
    {
        await Test<string>();
    }

    [Fact]
    public async Task TestByteArray()
    {
        await Test<byte[]>();
    }

    [Fact]
    public async Task TestGuid()
    {
        await Test<Guid>();
    }

    [Fact]
    public async Task TestNGuid()
    {
        await Test<Guid?>();
    }

    private async Task Test<T>()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<SomeSourceEntity<T>, SomeTargetEntity<T>>();
        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<SomeSourceEntity<T>>().Add(new SomeSourceEntity<T>(2));
            await databaseContext.SaveChangesAsync();
        });

        // act
        SomeSourceEntity<T>? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<SomeSourceEntity<T>>().AsNoTracking().SingleAsync();
        });

        var session1 = mapper.CreateMappingSession();
        var instance = session1.Map<SomeSourceEntity<T>, SomeTargetEntity<T>>(entity!);
        Assert.NotEqual(default, instance.Id);
        Assert.NotEqual(default, instance.Timestamp);
        Assert.Equal(2, instance.SomeProperty);
    }
}
