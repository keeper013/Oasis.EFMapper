namespace Oasis.EntityFramework.Mapper.Test.OneToOne;

using System.Data.Entity;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public sealed class EntityPropertyMappingTests : TestBase
{
    [Test]
    public async Task AddOneToOneEntity_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<Outer1, Outer2>();
        var mapper = mapperBuilder.Build();

        var outer1 = new Outer1(1);
        var inner1 = new Inner1_1(1);
        var inner2 = new Inner1_2("1");
        outer1.Inner1 = inner1;
        outer1.Inner2 = inner2;

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<Outer1>().Add(outer1);
            await databaseContext.SaveChangesAsync();
        });

        // act
        Outer1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        });

        var outer2 = mapper.Map<Outer1, Outer2>(entity!);

        // assert
        Assert.AreEqual(1, outer2.IntProp);
        Assert.NotNull(outer2.Inner1);
        Assert.AreEqual(1, outer2.Inner1!.LongProp);
        Assert.NotNull(outer2.Inner2);
        Assert.AreEqual("1", outer2.Inner2!.StringProp);
    }

    [Test]
    public async Task UpdateOneToOneEntityWithNew_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<Outer1, Outer2>();
        var mapper = mapperBuilder.Build();

        var outer1 = new Outer1(1);
        var inner1 = new Inner1_1(1);
        var inner2 = new Inner1_2("1");
        outer1.Inner1 = inner1;
        outer1.Inner2 = inner2;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<Outer1>().Add(outer1);
            await databaseContext.SaveChangesAsync();
        });

        // act
        Outer1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        });

        var outer2 = mapper.Map<Outer1, Outer2>(entity!);
        outer2.Inner1 = new Inner2_1(2);
        outer2.Inner2 = new Inner2_2("2");
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var result1 = await mapper.MapAsync<Outer2, Outer1>(outer2, databaseContext, o => o.Include(o => o.Inner1).Include(o => o.Inner2));
            await databaseContext.SaveChangesAsync();
        });

        Outer1? result2 = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result2 = await databaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        });

        // assert
        Assert.AreEqual(1, result2!.IntProp);
        Assert.NotNull(result2.Inner1);
        Assert.AreEqual(2, result2.Inner1!.LongProp);
        Assert.NotNull(result2.Inner2);
        Assert.AreEqual("2", result2.Inner2!.StringProp);
    }

    [Test]
    [Ignore("EF6 doesn't seems to handle replacing one to one relation entity very well, it updates first, and cause a unique constraint problem, deleting should come first.")]
    public async Task UpdateOneToOneEntityWithExisting_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<Outer1, Outer2>();
        var mapper = mapperBuilder.Build();

        await ReplaceOneToOneMapping(mapper);

        Outer1? result2 = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result2 = await databaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();

            // assert
            Assert.AreEqual(1, await databaseContext.Set<Inner1_1>().CountAsync());
            Assert.AreEqual(1, await databaseContext.Set<Inner1_2>().CountAsync());
        });

        Assert.AreEqual(1, result2!.IntProp);
        Assert.NotNull(result2.Inner1);
        Assert.AreEqual(2, result2.Inner1!.LongProp);
        Assert.NotNull(result2.Inner2);
        Assert.AreEqual("2", result2.Inner2!.StringProp);
    }

    [Test]
    [Ignore("EF6 doesn't seems to handle replacing one to one relation entity very well, it updates first, and cause a unique constraint problem, deleting should come first.")]
    public async Task UpdateOneToOneEntityKeepEntityOnMappingRemoved_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .WithConfiguration<Inner1_1>(new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), true))
            .WithConfiguration<Inner1_2>(new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), true))
            .RegisterTwoWay<Outer1, Outer2>();
        var mapper = mapperBuilder.Build();

        await ReplaceOneToOneMapping(mapper);

        // assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            Assert.AreEqual(2, await databaseContext.Set<Inner1_1>().CountAsync());
            Assert.AreEqual(2, await databaseContext.Set<Inner1_2>().CountAsync());
        });
    }

    [Test]
    [Ignore("EF6 doesn't seems to handle replacing one to one relation entity very well, it updates first, and cause a unique constraint problem, deleting should come first.")]
    public async Task UpdateOneToOneEntity_WithDefaultKeepEntityOnMappingRemoved_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), true));
        mapperBuilder
            .WithConfiguration<Inner1_1>(new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), false))
            .WithConfiguration<Inner1_2>(new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), false))
            .RegisterTwoWay<Outer1, Outer2>();
        var mapper = mapperBuilder.Build();

        await ReplaceOneToOneMapping(mapper);

        // assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            Assert.AreEqual(1, await databaseContext.Set<Inner1_1>().CountAsync());
            Assert.AreEqual(2, await databaseContext.Set<Inner1_2>().CountAsync());
        });
    }

    [Test]
    public async Task AddRecursiveEntity_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<RecursiveEntity1, RecursiveEntity2>();
        var mapper = mapperBuilder.Build();

        var parent = new RecursiveEntity1("parent");
        var child1 = new RecursiveEntity1("child");
        parent.Child = child1;

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<RecursiveEntity1>().Add(parent);
            await databaseContext.SaveChangesAsync();
        });

        // act
        RecursiveEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<RecursiveEntity1>().AsNoTracking().Include(o => o.Parent).Include(o => o.Child).FirstAsync(r => r.StringProperty == "parent");
        });

        var r2 = mapper.Map<RecursiveEntity1, RecursiveEntity2>(entity!);

        Assert.AreEqual("parent", r2.StringProperty);
        Assert.Null(r2.Parent);
        Assert.NotNull(r2.Child);
        Assert.NotNull(r2.Child!.Parent);
        Assert.AreEqual(r2.Id, r2.Child.Parent!.Id);

        r2.StringProperty = "parent 1";
        r2.Child.StringProperty = "child 1";

        RecursiveEntity1? r3 = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            r3 = await mapper.MapAsync<RecursiveEntity2, RecursiveEntity1>(r2, databaseContext, r => r.Include(r => r.Parent).Include(r => r.Child));
        });

        Assert.AreEqual("parent 1", r3!.StringProperty);
        Assert.Null(r3.Parent);
        Assert.NotNull(r3.Child);
        Assert.NotNull(r3.Child!.Parent);
        Assert.AreEqual(r3.Id, r3.Child.Parent!.Id);
        Assert.AreEqual("child 1", r3.Child.StringProperty);
    }

    private async Task ReplaceOneToOneMapping(IMapper mapper)
    {
        var outer1 = new Outer1(1);
        var inner1 = new Inner1_1(1);
        var inner2 = new Inner1_2("1");
        outer1.Inner1 = inner1;
        outer1.Inner2 = inner2;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<Inner1_1>().Add(new Inner1_1(2));
            databaseContext.Set<Inner1_2>().Add(new Inner1_2("2"));
            databaseContext.Set<Outer1>().Add(outer1);
            await databaseContext.SaveChangesAsync();
        });

        // act
        Outer2? outer2 = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await databaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
            var session1 = mapper.CreateMappingSession();
            outer2 = session1.Map<Outer1, Outer2>(entity);
            outer2.Inner1 = session1.Map<Inner1_1, Inner2_1>(await databaseContext.Set<Inner1_1>().FirstAsync(i => i.LongProp == 2));
            outer2.Inner2 = session1.Map<Inner1_2, Inner2_2>(await databaseContext.Set<Inner1_2>().FirstAsync(i => i.StringProp == "2"));
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var result1 = await mapper.MapAsync<Outer2, Outer1>(outer2!, databaseContext, o => o.Include(o => o.Inner1).Include(o => o.Inner2));
            await databaseContext.SaveChangesAsync();
        });
    }
}
