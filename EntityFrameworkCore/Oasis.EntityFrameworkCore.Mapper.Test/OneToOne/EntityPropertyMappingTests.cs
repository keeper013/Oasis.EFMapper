namespace Oasis.EntityFrameworkCore.Mapper.Test.OneToOne;

using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

public sealed class EntityPropertyMappingTests : TestBase
{
    [Fact]
    public async Task AddOneToOneEntity_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.Register<PrincipalOptional1, PrincipalOptional2>();
        var mapper = mapperBuilder.Build();

        var principalOptional1 = new PrincipalOptional1(1);
        var inner1 = new DependentOptional1_1(1);
        var inner2 = new DependentOptional1_2("1");
        principalOptional1.Inner1 = inner1;
        principalOptional1.Inner2 = inner2;

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<PrincipalOptional1>().Add(principalOptional1);
            await databaseContext.SaveChangesAsync();
        });

        // act
        PrincipalOptional1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<PrincipalOptional1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        });

        var principalOptional2 = mapper.Map<PrincipalOptional1, PrincipalOptional2>(entity);

        // assert
        Assert.Equal(1, principalOptional2.IntProp);
        Assert.NotNull(principalOptional2.Inner1);
        Assert.Equal(1, principalOptional2.Inner1!.LongProp);
        Assert.NotNull(principalOptional2.Inner2);
        Assert.Equal("1", principalOptional2.Inner2!.StringProp);
    }

    [Fact]
    public async Task UpdateOneToOneEntityWithNew_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.RegisterTwoWay<PrincipalOptional1, PrincipalOptional2>();
        var mapper = mapperBuilder.Build();

        var principalOptional1 = new PrincipalOptional1(1);
        var inner1 = new DependentOptional1_1(1);
        var inner2 = new DependentOptional1_2("1");
        principalOptional1.Inner1 = inner1;
        principalOptional1.Inner2 = inner2;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<PrincipalOptional1>().Add(principalOptional1);
            await databaseContext.SaveChangesAsync();
        });

        // act
        PrincipalOptional1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<PrincipalOptional1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        });

        var principalOptional2 = mapper.Map<PrincipalOptional1, PrincipalOptional2>(entity);
        principalOptional2.Inner1 = new Dependent2_1(2);
        principalOptional2.Inner2 = new Dependent2_2("2");
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var result1 = await mapper.MapAsync<PrincipalOptional2, PrincipalOptional1>(principalOptional2, o => o.Include(o => o.Inner1).Include(o => o.Inner2), databaseContext);
            await databaseContext.SaveChangesAsync();
        });

        PrincipalOptional1 result2 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result2 = await databaseContext.Set<PrincipalOptional1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        });

        // assert
        Assert.Equal(1, result2.IntProp);
        Assert.NotNull(result2.Inner1);
        Assert.Equal(2, result2.Inner1!.LongProp);
        Assert.NotNull(result2.Inner2);
        Assert.Equal("2", result2.Inner2!.StringProp);
    }

    [Fact]
    public async Task UpdateOneToOneRequiredEntityWithExisting_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder
            .WithConfiguration<DependentRequired1_1>(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), null, false)
            .WithConfiguration<DependentRequired1_2>(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), null, false)
            .RegisterTwoWay<PrincipalRequired1, PrincipalRequired2>();
        var mapper = mapperBuilder.Build();

        var principalRequired1 = new PrincipalRequired1(1);
        var inner1 = new DependentRequired1_1(1);
        var inner2 = new DependentRequired1_2("1");
        principalRequired1.Inner1 = inner1;
        principalRequired1.Inner2 = inner2;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<PrincipalRequired1>().Add(principalRequired1);
            await databaseContext.SaveChangesAsync();
        });

        // act
        PrincipalRequired2 principalRequired2 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await databaseContext.Set<PrincipalRequired1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
            var session1 = mapper.CreateMappingSession();
            principalRequired2 = session1.Map<PrincipalRequired1, PrincipalRequired2>(entity);
            principalRequired2.Inner1 = session1.Map<DependentRequired1_1, Dependent2_1>(new DependentRequired1_1(2));
            principalRequired2.Inner2 = session1.Map<DependentRequired1_2, Dependent2_2>(new DependentRequired1_2("2"));
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var result1 = await mapper.MapAsync<PrincipalRequired2, PrincipalRequired1>(principalRequired2, o => o.Include(o => o.Inner1).Include(o => o.Inner2), databaseContext);
            await databaseContext.SaveChangesAsync();
        });

        PrincipalRequired1 result2 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result2 = await databaseContext.Set<PrincipalRequired1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();

            // assert
            Assert.Equal(1, await databaseContext.Set<DependentRequired1_1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<DependentRequired1_2>().CountAsync());
        });

        Assert.Equal(1, result2.IntProp);
        Assert.NotNull(result2.Inner1);
        Assert.Equal(2, result2.Inner1!.LongProp);
        Assert.NotNull(result2.Inner2);
        Assert.Equal("2", result2.Inner2!.StringProp);
    }

    [Fact]
    public async Task UpdateOptionalOneToOneEntityWithExisting_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.RegisterTwoWay<PrincipalOptional1, PrincipalOptional2>();
        var mapper = mapperBuilder.Build();

        await ReplaceOptinoalOneToOneMapping(mapper);

        PrincipalOptional1 result2 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result2 = await databaseContext.Set<PrincipalOptional1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();

            // assert
            Assert.Equal(2, await databaseContext.Set<DependentOptional1_1>().CountAsync());
            Assert.Equal(2, await databaseContext.Set<DependentOptional1_2>().CountAsync());
        });

        Assert.Equal(1, result2.IntProp);
        Assert.NotNull(result2.Inner1);
        Assert.Equal(2, result2.Inner1!.LongProp);
        Assert.NotNull(result2.Inner2);
        Assert.Equal("2", result2.Inner2!.StringProp);
    }

    [Fact]
    public async Task UpdateOneToOneEntityKeepEntityOnMappingRemoved_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.RegisterTwoWay<PrincipalOptional1, PrincipalOptional2>();
        var mapper = mapperBuilder.Build();

        await ReplaceOptinoalOneToOneMapping(mapper);

        // assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            Assert.Equal(2, await databaseContext.Set<DependentOptional1_1>().CountAsync());
            Assert.Equal(2, await databaseContext.Set<DependentOptional1_2>().CountAsync());
        });
    }

    [Fact]
    public async Task UpdateOneToOneEntity_WithDefaultKeepEntityOnMappingRemoved_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        mapperBuilder.RegisterTwoWay<PrincipalOptional1, PrincipalOptional2>();
        var mapper = mapperBuilder.Build();

        await ReplaceOptinoalOneToOneMapping(mapper);

        // assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            Assert.Equal(2, await databaseContext.Set<DependentOptional1_1>().CountAsync());
            Assert.Equal(2, await databaseContext.Set<DependentOptional1_2>().CountAsync());
        });
    }

    [Fact]
    public async Task AddRecursiveEntity_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
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
        RecursiveEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<RecursiveEntity1>().AsNoTracking().Include(o => o.Parent).Include(o => o.Child).FirstAsync(r => r.StringProperty == "parent");
        });

        var r2 = mapper.Map<RecursiveEntity1, RecursiveEntity2>(entity);

        Assert.Equal("parent", r2.StringProperty);
        Assert.Null(r2.Parent);
        Assert.NotNull(r2.Child);
        Assert.NotNull(r2.Child!.Parent);
        Assert.Equal(r2.Id, r2.Child.ParentId);

        r2.StringProperty = "parent 1";
        r2.Child.StringProperty = "child 1";

        RecursiveEntity1 r3 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            r3 = await mapper.MapAsync<RecursiveEntity2, RecursiveEntity1>(r2, r => r.Include(r => r.Parent).Include(r => r.Child), databaseContext);
        });

        Assert.Equal("parent 1", r3.StringProperty);
        Assert.Null(r3.Parent);
        Assert.NotNull(r3.Child);
        Assert.NotNull(r3.Child!.Parent);
        Assert.Equal(r3.Id, r3.Child.ParentId);
        Assert.Equal("child 1", r3.Child.StringProperty);
    }

    private async Task ReplaceOptinoalOneToOneMapping(IMapper mapper)
    {
        var principalOptional1 = new PrincipalOptional1(1);
        var inner1 = new DependentOptional1_1(1);
        var inner2 = new DependentOptional1_2("1");
        principalOptional1.Inner1 = inner1;
        principalOptional1.Inner2 = inner2;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<DependentOptional1_1>().Add(new DependentOptional1_1(2));
            databaseContext.Set<DependentOptional1_2>().Add(new DependentOptional1_2("2"));
            databaseContext.Set<PrincipalOptional1>().Add(principalOptional1);
            await databaseContext.SaveChangesAsync();
        });

        // act
        PrincipalOptional2 principalOptional2 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await databaseContext.Set<PrincipalOptional1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
            var session1 = mapper.CreateMappingSession();
            principalOptional2 = session1.Map<PrincipalOptional1, PrincipalOptional2>(entity);
            principalOptional2.Inner1 = session1.Map<DependentOptional1_1, Dependent2_1>(await databaseContext.Set<DependentOptional1_1>().FirstAsync(i => i.LongProp == 2));
            principalOptional2.Inner2 = session1.Map<DependentOptional1_2, Dependent2_2>(await databaseContext.Set<DependentOptional1_2>().FirstAsync(i => i.StringProp == "2"));
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var result1 = await mapper.MapAsync<PrincipalOptional2, PrincipalOptional1>(principalOptional2, o => o.Include(o => o.Inner1).Include(o => o.Inner2), databaseContext);
            await databaseContext.SaveChangesAsync();
        });
    }
}
