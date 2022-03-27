namespace Oasis.EntityFrameworkCore.Mapper.Test.OneToMany;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using Oasis.EntityFrameworkCore.Mapper.Test.Scalar;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class ListPropertyMappingTests : TestBase
{
    [Fact]
    public async Task MapListProperties_ICollection_MappingShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<CollectionEntity1, CollectionEntity2>();
        var mapper = mapperBuilder.Build();

        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, null, "4", new byte[] { 2, 3, 4 });
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { sc1_1, sc1_2 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        CollectionEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var session = mapper.CreateMappingSession();
        var result = session.Map<CollectionEntity1, CollectionEntity2>(entity!);

        // assert
        Assert.Equal(1, result!.IntProp);
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
    public async Task MapListProperties_ICollection_MappingToDatabaseShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<CollectionEntity2, CollectionEntity1>();
        var mapper = mapperBuilder.Build();

        var sc1_1 = new ScalarEntity2(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new ScalarEntity2(2, null, "4", new byte[] { 2, 3, 4 });
        var collectionEntity2 = new CollectionEntity2(1, new List<ScalarEntity2> { sc1_1, sc1_2 });

        // act
        CollectionEntity1? result = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            result = await session.MapAsync<CollectionEntity2, CollectionEntity1>(collectionEntity2, c => c.Include(c => c.Scs));
        });

        // assert
        Assert.Equal(1, result!.IntProp);
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
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<ListIEntity1, CollectionEntity1>();
        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var subInstance = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
            databaseContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { subInstance }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        CollectionEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var session1 = mapper.CreateMappingSession();
        var result1 = session1.Map<CollectionEntity1, ListIEntity1>(entity!);

        result1!.IntProp = 2;
        var item0 = result1.Scs![0];
        item0.IntProp = 2;
        item0.LongNullableProp = 3;
        item0.StringProp = "4";
        item0.ByteArrayProp = new byte[] { 2 };
        CollectionEntity1? result2 = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session2 = mapper.CreateMappingToDatabaseSession(databaseContext);
            result2 = await session2.MapAsync<ListIEntity1, CollectionEntity1>(result1, c => c.Include(c => c.Scs));
        });

        // assert
        Assert.Equal(2, result2!.IntProp);
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
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<ListEntity1, CollectionEntity1>();
        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
            var sc1_2 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
            databaseContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { sc1_1, sc1_2 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        CollectionEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var session1 = mapper.CreateMappingSession();
        var result1 = session1.Map<CollectionEntity1, ListEntity1>(entity!);

        result1!.IntProp = 2;
        result1.Scs!.Remove(result1.Scs.ElementAt(1));
        var item0 = result1.Scs!.ElementAt(0);
        item0.IntProp = 2;
        item0.LongNullableProp = default;
        item0.StringProp = "4";
        item0.ByteArrayProp = new byte[] { 2 };

        CollectionEntity1? result2 = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            result2 = await session.MapAsync<ListEntity1, CollectionEntity1>(result1, x => x.Include(x => x.Scs));
        });

        // assert
        Assert.Equal(2, result2!.IntProp);
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
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        mapperBuilder.Register<DerivedEntity2, DerivedEntity1>();

        var mapper = mapperBuilder.Build();

        var instance = new DerivedEntity2("str2", 2, new List<ScalarEntity2> { new ScalarEntity2(1, 2, "3", new byte[] { 1 }) });

        // act
        DerivedEntity1? result = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            result = await session.MapAsync<DerivedEntity2, DerivedEntity1>(instance, x => x.AsNoTracking().Include(x => x.Scs));
        });

        // assert
        Assert.Equal("str2", result!.StringProp);
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
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        mapperBuilder.Register<DerivedEntity2_2, DerivedEntity1_1>();

        var mapper = mapperBuilder.Build();

        var instance = new DerivedEntity2_2(2, 2, new List<ScalarEntity2> { new ScalarEntity2(1, 2, "3", new byte[] { 1 }) });

        // act
        DerivedEntity1_1? result = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(CreateDatabaseContext());
            result = await session.MapAsync<DerivedEntity2_2, DerivedEntity1_1>(instance, x => x.AsNoTracking().Include(x => x.Scs));
        });

        // assert
        Assert.Equal(2, result!.IntProp);
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
    public async Task ChangeListEntityById_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<ListEntity2, ListEntity3>();
        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var listEntity2_1 = new ListEntity2(1);
            var listEntity2_2 = new ListEntity2(2);
            var subEntity2 = new SubEntity2("1");
            subEntity2.ListEntity = listEntity2_1;
            databaseContext.Set<ListEntity2>().AddRange(listEntity2_1, listEntity2_2);
            databaseContext.Set<SubEntity2>().Add(subEntity2);
            await databaseContext.SaveChangesAsync();
        });

        // act
        SubEntity3? result1 = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await databaseContext.Set<SubEntity2>().AsNoTracking().Include(s => s.ListEntity).FirstAsync();
            var session1 = mapper.CreateMappingSession();
            result1 = session1.Map<SubEntity2, SubEntity3>(entity);
            var listEntity2 = await databaseContext.Set<ListEntity2>().AsNoTracking().FirstAsync(l => l.IntProp == 2);
            result1.ListEntityId = listEntity2.Id;
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session2 = mapper.CreateMappingToDatabaseSession(databaseContext);
            var result2 = await session2.MapAsync<SubEntity3, SubEntity2>(result1!);
            await databaseContext.SaveChangesAsync();
        });

        // assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var l1 = await databaseContext.Set<ListEntity2>().AsNoTracking().Include(l => l.SubEntities).FirstAsync(l => l.IntProp == 1);
            Assert.Empty(l1.SubEntities);
            var l2 = await databaseContext.Set<ListEntity2>().AsNoTracking().Include(l => l.SubEntities).FirstAsync(l => l.IntProp == 2);
            Assert.Single(l2.SubEntities);
        });
    }

    [Fact]
    public async Task MapListProperties_UpdateNonExistingNavitation_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        mapperBuilder.RegisterTwoWay<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var sub = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { sub }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        CollectionEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var session1 = mapper.CreateMappingSession();
        var result1 = session1.Map<CollectionEntity1, ListIEntity1>(entity!);
        var item0 = result1.Scs![0];
        item0.Id++;
        item0.IntProp = 3;

        // assert
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
        {
            await ExecuteWithNewDatabaseContext(async (databaseContext) =>
            {
                var session2 = mapper.CreateMappingToDatabaseSession(databaseContext);
                await session2.MapAsync<ListIEntity1, CollectionEntity1>(result1, x => x.Include(x => x.Scs));
            });
        });
    }

    [Fact]
    public async Task MapListProperties_HasAsNoTrackingInIncluder_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        mapperBuilder.RegisterTwoWay<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var subInstance = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { subInstance }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        CollectionEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var session1 = mapper.CreateMappingSession();
        var result1 = session1.Map<CollectionEntity1, ListIEntity1>(entity!);
        result1.IntProp = 2;
        var item0 = result1.Scs![0];
        item0.IntProp = 2;
        item0.LongNullableProp = 3;
        item0.StringProp = "4";
        item0.ByteArrayProp = new byte[] { 2 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session2 = mapper.CreateMappingToDatabaseSession(databaseContext);
            await Assert.ThrowsAsync<AsNoTrackingNotAllowedException>(
                async () => await session2.MapAsync<ListIEntity1, CollectionEntity1>(result1, c => c.AsNoTracking().Include(c => c.Scs)));
        });
    }
}
