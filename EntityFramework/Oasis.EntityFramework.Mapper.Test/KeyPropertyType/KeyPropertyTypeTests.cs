namespace Oasis.EntityFramework.Mapper.Test.KeyPropertyType;

using System;
using System.Data.Entity;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class KeyPropertyTypeTests : TestBase
{
    [Test]
    public async Task TestByte()
    {
        await Test<ByteSourceEntity, byte>(databaseContext => databaseContext.Set<ByteSourceEntity>().Add(new ByteSourceEntity(2)));
    }

    [Test]
    public async Task TestNByte()
    {
        await Test<NByteSourceEntity, byte?>(databaseContext => databaseContext.Set<NByteSourceEntity>().Add(new NByteSourceEntity(2)));
    }

    [Test]
    public async Task TestShort()
    {
        await Test<ShortSourceEntity, short>(databaseContext => databaseContext.Set<ShortSourceEntity>().Add(new ShortSourceEntity(2)));
    }

    [Test]
    public async Task TestNShort()
    {
        await Test<NShortSourceEntity, short?>(databaseContext => databaseContext.Set<NShortSourceEntity>().Add(new NShortSourceEntity(2)));
    }

    [Test]
    public async Task TestInt()
    {
        await Test<IntSourceEntity, int>(databaseContext => databaseContext.Set<IntSourceEntity>().Add(new IntSourceEntity(2)));
    }

    [Test]
    public async Task TestNInt()
    {
        await Test<NIntSourceEntity, int?>(databaseContext => databaseContext.Set<NIntSourceEntity>().Add(new NIntSourceEntity(2)));
    }

    [Test]
    public async Task TestLong()
    {
        await Test<LongSourceEntity, long>(databaseContext => databaseContext.Set<LongSourceEntity>().Add(new LongSourceEntity(2)));
    }

    [Test]
    public async Task TestNLong()
    {
        await Test<NLongSourceEntity, long?>(databaseContext => databaseContext.Set<NLongSourceEntity>().Add(new NLongSourceEntity(2)));
    }

    private async Task Test<TSourceEntity, T>(Action<DbContext> action)
        where TSourceEntity : SomeSourceEntity<T>
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<TSourceEntity, SomeTargetEntity<T>>().Build().MakeToMemoryMapper();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            action(databaseContext);
            await databaseContext.SaveChangesAsync();
        });

        // act
        TSourceEntity entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<TSourceEntity>().AsNoTracking().FirstAsync();
        });

        var instance = mapper.Map<TSourceEntity, SomeTargetEntity<T>>(entity);
        Assert.AreNotEqual(default, instance.Id);
        
        // sqlite doesn't handle concurrenty token, so we ignore checking for concurrency token.
        Assert.AreEqual(2, instance.SomeProperty);
    }
}
