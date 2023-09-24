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
    public async Task TestByteNByte()
    {
        await Test<byte, byte?>(i => i);
    }

    [Fact]
    public async Task TestNByteByte()
    {
        await Test<byte?, byte>(i => i ?? 0);
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
    public async Task TestShortNShort()
    {
        await Test<short, short?>(i => i);
    }

    [Fact]
    public async Task TestNShortShort()
    {
        await Test<short?, short>(i => i ?? 0);
    }

    [Fact]
    public async Task TestByteShort()
    {
        await Test<byte, short>(i => i);
    }

    [Fact]
    public async Task TestByteNShort()
    {
        await Test<byte, short?>(i => i);
    }

    [Fact]
    public async Task TestNByteNShort()
    {
        await Test<byte?, short?>(i => i);
    }

    [Fact]
    public async Task TestNByteShort()
    {
        await Test<byte?, short>(i => i ?? 0);
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
    public async Task TestByteUShort()
    {
        await Test<byte, ushort>(i => i);
    }

    [Fact]
    public async Task TestByteNUShort()
    {
        await Test<byte, ushort?>(i => i);
    }

    [Fact]
    public async Task TestNByteUShort()
    {
        await Test<byte?, ushort>(i => i ?? 0);
    }

    [Fact]
    public async Task TestNByteNUShort()
    {
        await Test<byte?, ushort?>(i => i);
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
        var mapper = MakeDefaultMapperBuilder().Register<SomeSourceEntity<T>, SomeTargetEntity<T>>().Build().MakeToMemoryMapper();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<SomeSourceEntity<T>>().Add(new SomeSourceEntity<T>(2));
            await databaseContext.SaveChangesAsync();
        });

        // act
        SomeSourceEntity<T> entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<SomeSourceEntity<T>>().AsNoTracking().FirstAsync();
        });

        var instance = mapper.Map<SomeSourceEntity<T>, SomeTargetEntity<T>>(entity);
        Assert.NotEqual(default, instance.Id);
        Assert.NotEqual(default, instance.ConcurrencyToken);
        Assert.Equal(2, instance.SomeProperty);
    }

    private async Task Test<TSourceIdentity, TTargetIdentity>(Func<TSourceIdentity, TTargetIdentity> func)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter(func)
            .Register<SomeSourceEntity<TSourceIdentity>, SomeTargetEntity<TTargetIdentity>>()
            .Build()
            .MakeToMemoryMapper();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<SomeSourceEntity<TSourceIdentity>>().Add(new SomeSourceEntity<TSourceIdentity>(2));
            await databaseContext.SaveChangesAsync();
        });

        // act
        SomeSourceEntity<TSourceIdentity> entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<SomeSourceEntity<TSourceIdentity>>().AsNoTracking().FirstAsync();
        });

        var instance = mapper.Map<SomeSourceEntity<TSourceIdentity>, SomeTargetEntity<TTargetIdentity>>(entity);
        Assert.NotEqual(default, instance.Id);
        Assert.NotEqual(default, instance.ConcurrencyToken);
        Assert.Equal(2, instance.SomeProperty);
    }
}
