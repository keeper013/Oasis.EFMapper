namespace Oasis.EntityFrameworkCore.Mapper.Test.OneToMany;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class ListPropertyMappingTests : TestBase
{
    [Fact]
    public void MapListProperties_ToMemory_SameInstance_ShouldThrowException()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.Register<CollectionEntity1, CollectionEntity2>();
        var mapper = mapperBuilder.Build();
        var sc1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var entity1 = new CollectionEntity1(1, new[] { sc1, sc1 });

        // Assert
        Assert.Throws<DuplicatedListItemException>(() =>
        {
            // Act
            mapper.Map<CollectionEntity1, CollectionEntity2>(entity1);
        });
    }

    [Fact]
    public async Task MapListProperties_ToDatabase_SameInstance_ShouldThrowException()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.Register<CollectionEntity2, CollectionEntity1>();
        var mapper = mapperBuilder.Build();
        var sc2 = new ScalarEntity2Item(1, 2, "3", new byte[] { 1 });
        var entity2 = new CollectionEntity2(1, new[] { sc2, sc2 });

        // Assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            await Assert.ThrowsAsync<DuplicatedListItemException>(async () =>
            {
                // Act
                await mapper.MapAsync<CollectionEntity2, CollectionEntity1>(entity2, databaseContext);
            });
        });
    }

    [Fact]
    public async Task MapListProperties_ICollection_MappingShouldSucceed()
    {
        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, default, "4", new byte[] { 2, 3, 4 });
        var result = await MapListProperties_ICollection<CollectionEntity2>(new List<SubScalarEntity1> { sc1_1, sc1_2 });

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
    public async Task MapListProperties_ListWrapper_MappingShouldSucceed()
    {
        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, default, "4", new byte[] { 2, 3, 4 });
        var result = await MapListProperties_ICollection<CollectionEntity2WithWrapper>(new List<SubScalarEntity1> { sc1_1, sc1_2 });

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
    public async Task MapListProperties_ListWrapperWithCustomizedFactoryMethod_ShouldSucceed()
    {
        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, default, "4", new byte[] { 2, 3, 4 });
        var result = await MapListProperties_ICollection<CollectionEntity3WithWrapper>(
            new List<SubScalarEntity1> { sc1_1, sc1_2 },
            (builder) => builder.WithFactoryMethod<ScalarEntity2NoDefaultConstructorListWrapper, ScalarEntity2Item>(() => new ScalarEntity2NoDefaultConstructorListWrapper(0)));

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
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.Register<CollectionEntity2, CollectionEntity1>();
        var mapper = mapperBuilder.Build();

        var sc1_1 = new ScalarEntity2Item(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new ScalarEntity2Item(2, null, "4", new byte[] { 2, 3, 4 });
        var collectionEntity2 = new CollectionEntity2(1, new List<ScalarEntity2Item> { sc1_1, sc1_2 });

        // act
        CollectionEntity1 result = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result = await mapper.MapAsync<CollectionEntity2, CollectionEntity1>(collectionEntity2, databaseContext, c => c.Include(c => c.Scs));
        });

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
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.RegisterTwoWay<ListIEntity1, CollectionEntity1>();
        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var subInstance = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
            databaseContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { subInstance }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        CollectionEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var result1 = mapper.Map<CollectionEntity1, ListIEntity1>(entity);

        result1!.IntProp = 2;
        var item0 = result1.Scs![0];
        item0.IntProp = 2;
        item0.LongNullableProp = 3;
        item0.StringProp = "4";
        item0.ByteArrayProp = new byte[] { 2 };
        CollectionEntity1 result2 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result2 = await mapper.MapAsync<ListIEntity1, CollectionEntity1>(result1, databaseContext, c => c.Include(c => c.Scs));
        });

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
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
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
        CollectionEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var result1 = mapper.Map<CollectionEntity1, ListEntity1>(entity);

        result1.IntProp = 2;
        result1.Scs!.Remove(result1.Scs.ElementAt(1));
        var item0 = result1.Scs!.ElementAt(0);
        item0.IntProp = 2;
        item0.LongNullableProp = default;
        item0.StringProp = "4";
        item0.ByteArrayProp = new byte[] { 2 };

        CollectionEntity1 result2 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result2 = await mapper.MapAsync<ListEntity1, CollectionEntity1>(result1, databaseContext, x => x.Include(x => x.Scs));
        });

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
        var mapperBuilder = MakeDefaultMapperBuilder(factory);

        mapperBuilder.Register<DerivedEntity2, DerivedEntity1>();

        var mapper = mapperBuilder.Build();

        var instance = new DerivedEntity2("str2", 2, new List<ScalarEntity2Item> { new ScalarEntity2Item(1, 2, "3", new byte[] { 1 }) });

        // act
        DerivedEntity1 result = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result = await mapper.MapAsync<DerivedEntity2, DerivedEntity1>(instance, databaseContext, x => x.AsNoTracking().Include(x => x.Scs));
        });

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
        var mapperBuilder = MakeDefaultMapperBuilder(factory);

        mapperBuilder.Register<DerivedEntity2_2, DerivedEntity1_1>();

        var mapper = mapperBuilder.Build();

        var instance = new DerivedEntity2_2(2, 2, new List<ScalarEntity2Item> { new ScalarEntity2Item(1, 2, "3", new byte[] { 1 }) });

        // act
        DerivedEntity1_1 result = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result = await mapper.MapAsync<DerivedEntity2_2, DerivedEntity1_1>(instance, databaseContext, x => x.AsNoTracking().Include(x => x.Scs));
        });

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
    public async Task ChangeListEntityById_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
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
        SubEntity3 result1 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await databaseContext.Set<SubEntity2>().AsNoTracking().Include(s => s.ListEntity).FirstAsync();
            result1 = mapper.Map<SubEntity2, SubEntity3>(entity);
            var listEntity2 = await databaseContext.Set<ListEntity2>().AsNoTracking().FirstAsync(l => l.IntProp == 2);
            result1.ListEntityId = listEntity2.Id;
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var result2 = await mapper.MapAsync<SubEntity3, SubEntity2>(result1, databaseContext);
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
    public async Task SessionTest_SameSessionAvoidDuplicatedNewEntity1()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder
            .Register<SessionTestingList2, SessionTestingList1_1>()
            .Register<SessionTestingList2, SessionTestingList1_2>();
        var mapper = mapperBuilder.Build();

        var item = new ScalarItem2("abc");
        var l2 = new SessionTestingList2(new List<ScalarItem2> { item });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            await session.MapAsync<SessionTestingList2, SessionTestingList1_1>(l2);
            await databaseContext.SaveChangesAsync();
            await session.MapAsync<SessionTestingList2, SessionTestingList1_2>(l2);
            await databaseContext.SaveChangesAsync();
        });
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            Assert.Equal(1, await databaseContext.Set<ScalarItem1>().CountAsync());
        });
    }

    [Fact]
    public async Task SessionTest_SameSessionAvoidDuplicatedNewEntity2()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.Register<SessionTestingList2, SessionTestingList1_1>();
        var mapper = mapperBuilder.Build();

        var item = new ScalarItem2("abc");
        var l2_1 = new SessionTestingList2(new List<ScalarItem2> { item });
        var l2_2 = new SessionTestingList2(new List<ScalarItem2> { item });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            await session.MapAsync<SessionTestingList2, SessionTestingList1_1>(l2_1);
            await databaseContext.SaveChangesAsync();
            await session.MapAsync<SessionTestingList2, SessionTestingList1_1>(l2_2);
            await databaseContext.SaveChangesAsync();
        });
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            Assert.Equal(2, await databaseContext.Set<SessionTestingList1_1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<ScalarItem1>().CountAsync());
        });
    }

    [Fact]
    public async Task SessionTest_DifferentSessionCreatesDuplicatedNewEntity1()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder
            .Register<SessionTestingList2, SessionTestingList1_1>()
            .Register<SessionTestingList2, SessionTestingList1_2>();
        var mapper = mapperBuilder.Build();

        var item = new ScalarItem2("abc");
        var l2 = new SessionTestingList2(new List<ScalarItem2> { item });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            await mapper.MapAsync<SessionTestingList2, SessionTestingList1_1>(l2, databaseContext);
            await mapper.MapAsync<SessionTestingList2, SessionTestingList1_2>(l2, databaseContext);
            await databaseContext.SaveChangesAsync();
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            Assert.Equal(2, await databaseContext.Set<ScalarItem1>().CountAsync());
        });
    }

    [Fact]
    public async Task SessionTest_DifferentSessionCreatesDuplicatedNewEntity2()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder
            .Register<SessionTestingList2, SessionTestingList1_1>()
            .Register<SessionTestingList2, SessionTestingList1_2>();
        var mapper = mapperBuilder.Build();

        var item = new ScalarItem2("abc");
        var l2 = new SessionTestingList2(new List<ScalarItem2> { item });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            await mapper.MapAsync<SessionTestingList2, SessionTestingList1_1>(l2, databaseContext);
            await mapper.MapAsync<SessionTestingList2, SessionTestingList1_1>(l2, databaseContext);
            await databaseContext.SaveChangesAsync();
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            Assert.Equal(2, await databaseContext.Set<SessionTestingList1_1>().CountAsync());
            Assert.Equal(2, await databaseContext.Set<ScalarItem1>().CountAsync());
        });
    }

    [Fact]
    public async Task MapListProperties_UpdateExistingNavitationIdentity_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);

        mapperBuilder.RegisterTwoWay<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var sub = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { sub }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        CollectionEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var result1 = mapper.Map<CollectionEntity1, ListIEntity1>(entity);
        var item0 = result1.Scs![0];
        item0.Id++;
        item0.IntProp = 3;

        // assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            await mapper.MapAsync<ListIEntity1, CollectionEntity1>(result1, databaseContext, x => x.Include(x => x.Scs));
            await databaseContext.SaveChangesAsync();
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
            Assert.Equal(1, entity.Scs!.Count);
            Assert.Equal(3, entity.Scs.ElementAt(0).IntProp);
            Assert.Equal(2, await databaseContext.Set<SubScalarEntity1>().CountAsync());
        });
    }

    [Fact]
    public async Task MapListProperties_HasAsNoTrackingInIncluder_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);

        mapperBuilder.RegisterTwoWay<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var subInstance = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, new List<SubScalarEntity1> { subInstance }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        CollectionEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var result1 = mapper.Map<CollectionEntity1, ListIEntity1>(entity);
        result1.IntProp = 2;
        var item0 = result1.Scs![0];
        item0.IntProp = 2;
        item0.LongNullableProp = 3;
        item0.StringProp = "4";
        item0.ByteArrayProp = new byte[] { 2 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            await Assert.ThrowsAsync<AsNoTrackingNotAllowedException>(
                async () => await mapper.MapAsync<ListIEntity1, CollectionEntity1>(result1, databaseContext, c => c.AsNoTracking().Include(c => c.Scs)));
        });
    }

    [Fact]
    public async Task ListWrapperWithNoDefaultConstructor_ShouldFail()
    {
        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, default, "4", new byte[] { 2, 3, 4 });
        await Assert.ThrowsAsync<UnconstructableTypeException>(async () => await MapListProperties_ICollection<CollectionEntity3WithWrapper>(new List<SubScalarEntity1> { sc1_1, sc1_2 }));
    }

    [Fact]
    public void CustomFactoryMethodForClassWithDefaultConstructor_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);

        // act and assert
        Assert.Throws<FactoryMethodException>(() =>
        {
            mapperBuilder
                .WithFactoryMethod<ScalarEntity2ListWrapper>(() => new ScalarEntity2ListWrapper());
        });
    }

    [Fact]
    public void CustomFactoryMethodForScalarList_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);

        // act and assert
        Assert.Throws<InvalidEntityListTypeException>(() =>
        {
            mapperBuilder
                .WithFactoryMethod<StringListNoDefaultConstructor, string>(() => new StringListNoDefaultConstructor(1));
        });
    }

    private async Task<T> MapListProperties_ICollection<T>(List<SubScalarEntity1> list, Action<IMapperBuilder>? action = default)
        where T : class
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.Register<CollectionEntity1, T>();
        if (action != default)
        {
            action(mapperBuilder);
        }

        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, list));
            await databaseContext.SaveChangesAsync();
        });

        // act
        CollectionEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        return mapper.Map<CollectionEntity1, T>(entity);
    }
}
