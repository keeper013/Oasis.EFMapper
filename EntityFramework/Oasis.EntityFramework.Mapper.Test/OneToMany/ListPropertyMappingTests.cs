namespace Oasis.EntityFramework.Mapper.Test.OneToMany;

using Oasis.EntityFramework.Mapper.Exceptions;
using Oasis.EntityFramework.Mapper.Test.Scalar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Data.Entity;

[TestFixture]
public class ListPropertyMappingTests : TestBase
{
    [Test]
    public async Task MapListProperties_ICollection_MappingShouldSucceed()
    {
        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, default, "4", new byte[] { 2, 3, 4 });
        var result = await MapListProperties_ICollection<CollectionEntity2>(new List<SubScalarEntity1> { sc1_1, sc1_2 });

        // assert
        Assert.AreEqual(1, result!.IntProp);
        Assert.NotNull(result.Scs);
        Assert.AreEqual(2, result.Scs!.Count);
        var item0 = result.Scs.ElementAt(0);
        Assert.AreEqual(1, item0.IntProp);
        Assert.AreEqual(2, item0.LongNullableProp);
        Assert.AreEqual("3", item0.StringProp);
        Assert.AreEqual(sc1_1.ByteArrayProp, item0.ByteArrayProp);
        var item1 = result.Scs.ElementAt(1);
        Assert.AreEqual(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.AreEqual("4", item1.StringProp);
        Assert.AreEqual(sc1_2.ByteArrayProp, item1.ByteArrayProp);
    }

    [Test]
    public async Task MapListProperties_ListWrapper_MappingShouldSucceed()
    {
        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, default, "4", new byte[] { 2, 3, 4 });
        var result = await MapListProperties_ICollection<CollectionEntity2WithWrapper>(new List<SubScalarEntity1> { sc1_1, sc1_2 });

        // assert
        Assert.AreEqual(1, result!.IntProp);
        Assert.NotNull(result.Scs);
        Assert.AreEqual(2, result.Scs!.Count);
        var item0 = result.Scs.ElementAt(0);
        Assert.AreEqual(1, item0.IntProp);
        Assert.AreEqual(2, item0.LongNullableProp);
        Assert.AreEqual("3", item0.StringProp);
        Assert.AreEqual(sc1_1.ByteArrayProp, item0.ByteArrayProp);
        var item1 = result.Scs.ElementAt(1);
        Assert.AreEqual(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.AreEqual("4", item1.StringProp);
        Assert.AreEqual(sc1_2.ByteArrayProp, item1.ByteArrayProp);
    }

    [Test]
    public async Task MapListProperties_ListWrapperWithCustomizedFactoryMethod_ShouldSucceed()
    {
        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, default, "4", new byte[] { 2, 3, 4 });
        var result = await MapListProperties_ICollection<CollectionEntity3WithWrapper>(
            new List<SubScalarEntity1> { sc1_1, sc1_2 },
            (builder) => builder.WithFactoryMethod<ScalarEntity2NoDefaultConstructorListWrapper, ScalarEntity2Item>(() => new ScalarEntity2NoDefaultConstructorListWrapper(0)));

        // assert
        Assert.AreEqual(1, result!.IntProp);
        Assert.NotNull(result.Scs);
        Assert.AreEqual(2, result.Scs!.Count);
        var item0 = result.Scs.ElementAt(0);
        Assert.AreEqual(1, item0.IntProp);
        Assert.AreEqual(2, item0.LongNullableProp);
        Assert.AreEqual("3", item0.StringProp);
        Assert.AreEqual(sc1_1.ByteArrayProp, item0.ByteArrayProp);
        var item1 = result.Scs.ElementAt(1);
        Assert.AreEqual(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.AreEqual("4", item1.StringProp);
        Assert.AreEqual(sc1_2.ByteArrayProp, item1.ByteArrayProp);
    }

    [Test]
    public async Task MapListProperties_ICollection_MappingToDatabaseShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<CollectionEntity2, CollectionEntity1>();
        var mapper = mapperBuilder.Build();

        var sc1_1 = new ScalarEntity2Item(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new ScalarEntity2Item(2, null, "4", new byte[] { 2, 3, 4 });
        var collectionEntity2 = new CollectionEntity2(1, new List<ScalarEntity2Item> { sc1_1, sc1_2 });

        // act
        CollectionEntity1? result = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            result = await session.MapAsync<CollectionEntity2, CollectionEntity1>(collectionEntity2, c => c.Include(c => c.Scs));
        });

        // assert
        Assert.AreEqual(1, result!.IntProp);
        Assert.NotNull(result.Scs);
        Assert.AreEqual(2, result.Scs!.Count);
        var item0 = result.Scs.ElementAt(0);
        Assert.AreEqual(1, item0.IntProp);
        Assert.AreEqual(2, item0.LongNullableProp);
        Assert.AreEqual("3", item0.StringProp);
        Assert.AreEqual(sc1_1.ByteArrayProp, item0.ByteArrayProp);
        var item1 = result.Scs.ElementAt(1);
        Assert.AreEqual(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.AreEqual("4", item1.StringProp);
        Assert.AreEqual(sc1_2.ByteArrayProp, item1.ByteArrayProp);
    }

    [Test]
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
        Assert.AreEqual(2, result2!.IntProp);
        Assert.NotNull(result2.Scs);
        Assert.AreEqual(1, result2.Scs!.Count);
        var item1 = result2.Scs.ElementAt(0);
        Assert.AreEqual(2, item1.IntProp);
        Assert.AreEqual(3, item1.LongNullableProp);
        Assert.AreEqual("4", item1.StringProp);
        Assert.AreEqual(result1.Scs!.ElementAt(0).ByteArrayProp, result2.Scs.ElementAt(0).ByteArrayProp);
    }

    [Test]
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
        Assert.AreEqual(2, result2!.IntProp);
        Assert.NotNull(result2.Scs);
        Assert.AreEqual(1, result2.Scs!.Count);
        var item1 = result2.Scs.ElementAt(0);
        Assert.AreEqual(2, item1.IntProp);
        Assert.Null(item1.LongNullableProp);
        Assert.AreEqual("4", item1.StringProp);
        Assert.AreEqual(result2.Scs!.ElementAt(0).ByteArrayProp, result1.Scs.ElementAt(0).ByteArrayProp);
    }

    [Test]
    public async Task MapDerivedEntities_ShouldMapDerivedAndBase()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        mapperBuilder.Register<DerivedEntity2, DerivedEntity1>();

        var mapper = mapperBuilder.Build();

        var instance = new DerivedEntity2("str2", 2, new List<ScalarEntity2Item> { new ScalarEntity2Item(1, 2, "3", new byte[] { 1 }) });

        // act
        DerivedEntity1? result = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            result = await session.MapAsync<DerivedEntity2, DerivedEntity1>(instance, x => x.AsNoTracking().Include(x => x.Scs));
        });

        // assert
        Assert.AreEqual("str2", result!.StringProp);
        Assert.AreEqual(2, result.IntProp);
        Assert.NotNull(result.Scs);
        Assert.AreEqual(1, result.Scs!.Count);
        var item0 = result.Scs![0];
        Assert.AreEqual("3", item0.StringProp);
        Assert.AreEqual(1, item0.IntProp);
        Assert.AreEqual(2, item0.LongNullableProp);
        Assert.AreEqual(item0.ByteArrayProp, instance.Scs!.ElementAt(0).ByteArrayProp);
    }

    [Test]
    public async Task HiddingPublicProperty_HiddenMemberIgnored()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        mapperBuilder.Register<DerivedEntity2_2, DerivedEntity1_1>();

        var mapper = mapperBuilder.Build();

        var instance = new DerivedEntity2_2(2, 2, new List<ScalarEntity2Item> { new ScalarEntity2Item(1, 2, "3", new byte[] { 1 }) });

        // act
        DerivedEntity1_1? result = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(CreateDatabaseContext());
            result = await session.MapAsync<DerivedEntity2_2, DerivedEntity1_1>(instance, x => x.AsNoTracking().Include(x => x.Scs));
        });

        // assert
        Assert.AreEqual(2, result!.IntProp);
        Assert.AreEqual(0, ((BaseEntity1)result).IntProp);
        Assert.NotNull(result.Scs);
        Assert.AreEqual(1, result.Scs!.Count);
        var item0 = result.Scs![0];
        Assert.AreEqual("3", item0.StringProp);
        Assert.AreEqual(1, item0.IntProp);
        Assert.AreEqual(2, item0.LongNullableProp);
        Assert.AreEqual(item0.ByteArrayProp, instance.Scs!.ElementAt(0).ByteArrayProp);
    }

    [Test]
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
            databaseContext.Set<ListEntity2>().AddRange(new[] { listEntity2_1, listEntity2_2 });
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
            Assert.IsEmpty(l1.SubEntities!);
            var l2 = await databaseContext.Set<ListEntity2>().AsNoTracking().Include(l => l.SubEntities).FirstAsync(l => l.IntProp == 2);
            Assert.AreEqual(1, l2.SubEntities!.Count);
        });
    }

    [Test]
    public async Task SessionTest_SameSessionAvoidDuplicatedNewEntity()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
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
            Assert.AreEqual(1, await databaseContext.Set<ScalarItem1>().CountAsync());
        });
    }

    [Test]
    public async Task SessionTest_DifferentSessionCreatesDuplicatedNewEntity()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
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
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            await session.MapAsync<SessionTestingList2, SessionTestingList1_2>(l2);
            await databaseContext.SaveChangesAsync();
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            Assert.AreEqual(2, await databaseContext.Set<ScalarItem1>().CountAsync());
        });
    }

    [Test]
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
        Assert.ThrowsAsync<EntityNotFoundException>(async () =>
        {
            await ExecuteWithNewDatabaseContext(async (databaseContext) =>
            {
                var session2 = mapper.CreateMappingToDatabaseSession(databaseContext);
                await session2.MapAsync<ListIEntity1, CollectionEntity1>(result1, x => x.Include(x => x.Scs));
            });
        });
    }

    [Test]
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
        using (var databaseContext = CreateDatabaseContext())
        {
            var session2 = mapper.CreateMappingToDatabaseSession(databaseContext);
            Assert.ThrowsAsync<AsNoTrackingNotAllowedException>(
                async () => await session2.MapAsync<ListIEntity1, CollectionEntity1>(result1, c => c.AsNoTracking().Include(c => c.Scs)));
        }
    }

    [Test]
    public void ListWrapperWithNoDefaultConstructor_ShouldFail()
    {
        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, default, "4", new byte[] { 2, 3, 4 });
        Assert.ThrowsAsync<UnknownListTypeException>(async () => await MapListProperties_ICollection<CollectionEntity3WithWrapper>(new List<SubScalarEntity1> { sc1_1, sc1_2 }));
    }

    [Test]
    public void ListWrapperWithNoSetter_ShouldFail()
    {
        var sc1_1 = new SubScalarEntity1(1, 2, "3", new byte[] { 1 });
        var sc1_2 = new SubScalarEntity1(2, default, "4", new byte[] { 2, 3, 4 });
        Assert.ThrowsAsync<SetterMissingException>(async () => await MapListProperties_ICollection<CollectionEntity4WithWrapper>(new List<SubScalarEntity1> { sc1_1, sc1_2 }));
    }

    [Test]
    public void ScalarAsEntity_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidEntityTypeException>(() =>
        {
            mapperBuilder
                .WithScalarConverter<ListEntity2, int>(e => 1)
                .Register<ListEntity2, ListEntity3>();
        });
    }

    [Test]
    public void EntityAsScalar_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidScalarTypeException>(() =>
        {
            mapperBuilder
                .Register<ListEntity2, ListEntity3>()
                .WithScalarConverter<ListEntity2, int>(e => 1);
        });
    }

    [Test]
    public void CustomFactoryMethodForClassWithDefaultConstructor_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<FactoryMethodException>(() =>
        {
            mapperBuilder
                .WithFactoryMethod<ScalarEntity2ListWrapper>(() => new ScalarEntity2ListWrapper());
        });
    }

    [Test]
    public void CustomFactoryMethodForScalarList_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidEntityListTypeException>(() =>
        {
            mapperBuilder
                .WithFactoryMethod<StringListNoDefaultConstructor, string>(() => new StringListNoDefaultConstructor(1));
        });
    }

    [Test]
    public void CustomFactoryMethodForNonEntityList_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidEntityListTypeException>(() =>
        {
            mapperBuilder
                .WithScalarConverter<ScalarEntity2Item, int>(s => 1)
                .WithFactoryMethod<ScalarEntity2NoDefaultConstructorListWrapper, ScalarEntity2Item>(() => new ScalarEntity2NoDefaultConstructorListWrapper(1));
        });
    }

    [Test]
    public void EntityInListForScalar_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidScalarTypeException>(() =>
        {
            mapperBuilder
                .WithFactoryMethod<ScalarEntity2NoDefaultConstructorListWrapper, ScalarEntity2Item>(() => new ScalarEntity2NoDefaultConstructorListWrapper(1))
                .WithScalarConverter<ScalarEntity2Item, int>(s => 1);
        });
    }

    [Test]
    public void EntityListForScalar_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidScalarTypeException>(() =>
        {
            mapperBuilder.WithScalarConverter<ScalarEntity2ListWrapper, int>(s => 1);
        });
    }

    private async Task<T> MapListProperties_ICollection<T>(List<SubScalarEntity1> list, Action<IMapperBuilder>? action = default)
        where T : class
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
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
        CollectionEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<CollectionEntity1>().AsNoTracking().Include(c => c.Scs).FirstAsync();
        });

        var session = mapper.CreateMappingSession();
        return session.Map<CollectionEntity1, T>(entity!);
    }
}
