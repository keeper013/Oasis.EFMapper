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
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<Outer1, Outer2>();
        var mapper = mapperBuilder.Build();

        var outer1 = new Outer1(1);
        var inner1 = new Inner1_1(1);
        var inner2 = new Inner1_2("1");
        outer1.Inner1 = inner1;
        outer1.Inner2 = inner2;
        DatabaseContext.Set<Outer1>().Add(outer1);
        await DatabaseContext.SaveChangesAsync();

        // act
        var entity = await DatabaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        var session = mapper.CreateMappingSession();
        var outer2 = session.Map<Outer1, Outer2>(entity);

        // assert
        Assert.Equal(1, outer2.IntProp);
        Assert.NotNull(outer2.Inner1);
        Assert.Equal(1, outer2.Inner1!.LongProp);
        Assert.NotNull(outer2.Inner2);
        Assert.Equal("1", outer2.Inner2!.StringProp);
    }

    [Fact]
    public async Task UpdateOneToOneEntityWithNew_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<Outer1, Outer2>();
        var mapper = mapperBuilder.Build();

        var outer1 = new Outer1(1);
        var inner1 = new Inner1_1(1);
        var inner2 = new Inner1_2("1");
        outer1.Inner1 = inner1;
        outer1.Inner2 = inner2;
        DatabaseContext.Set<Outer1>().Add(outer1);
        await DatabaseContext.SaveChangesAsync();

        // act
        var entity = await DatabaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        var session1 = mapper.CreateMappingSession();
        var outer2 = session1.Map<Outer1, Outer2>(entity);
        outer2.Inner1 = new Inner2_1(2);
        outer2.Inner2 = new Inner2_2("2");
        var session2 = mapper.CreateMappingToDatabaseSession(DatabaseContext);
        var result1 = await session2.MapAsync<Outer2, Outer1>(outer2);
        await DatabaseContext.SaveChangesAsync();
        var result2 = await DatabaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();

        // assert
        Assert.Equal(1, result2.IntProp);
        Assert.NotNull(result2.Inner1);
        Assert.Equal(2, result2.Inner1!.LongProp);
        Assert.NotNull(result2.Inner2);
        Assert.Equal("2", result2.Inner2!.StringProp);
    }

    [Fact]
    public async Task UpdateOneToOneEntityWithExisting_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<Outer1, Outer2>();
        var mapper = mapperBuilder.Build();

        var outer1 = new Outer1(1);
        var inner1 = new Inner1_1(1);
        var inner2 = new Inner1_2("1");
        outer1.Inner1 = inner1;
        outer1.Inner2 = inner2;
        DatabaseContext.Set<Inner1_1>().Add(new Inner1_1(2));
        DatabaseContext.Set<Inner1_2>().Add(new Inner1_2("2"));
        DatabaseContext.Set<Outer1>().Add(outer1);
        await DatabaseContext.SaveChangesAsync();

        // act
        var entity = await DatabaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        var session1 = mapper.CreateMappingSession();
        var outer2 = session1.Map<Outer1, Outer2>(entity);
        outer2.Inner1 = session1.Map<Inner1_1, Inner2_1>(await DatabaseContext.Set<Inner1_1>().FirstAsync(i => i.LongProp == 2));
        outer2.Inner2 = session1.Map<Inner1_2, Inner2_2>(await DatabaseContext.Set<Inner1_2>().FirstAsync(i => i.StringProp == "2"));
        var session2 = mapper.CreateMappingToDatabaseSession(DatabaseContext);
        var result1 = await session2.MapAsync<Outer2, Outer1>(outer2);
        await DatabaseContext.SaveChangesAsync();
        var result2 = await DatabaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();

        // assert
        Assert.Equal(1, await DatabaseContext.Set<Inner1_1>().CountAsync());
        Assert.Equal(1, await DatabaseContext.Set<Inner1_2>().CountAsync());
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
        var mapperBuilder = factory.Make(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .WithConfiguration<Inner1_1>(new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.Timestamp), true))
            .WithConfiguration<Inner1_2>(new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.Timestamp), true))
            .RegisterTwoWay<Outer1, Outer2>();
        var mapper = mapperBuilder.Build();

        var outer1 = new Outer1(1);
        var inner1 = new Inner1_1(1);
        var inner2 = new Inner1_2("1");
        outer1.Inner1 = inner1;
        outer1.Inner2 = inner2;
        DatabaseContext.Set<Inner1_1>().Add(new Inner1_1(2));
        DatabaseContext.Set<Inner1_2>().Add(new Inner1_2("2"));
        DatabaseContext.Set<Outer1>().Add(outer1);
        await DatabaseContext.SaveChangesAsync();

        // act
        var entity = await DatabaseContext.Set<Outer1>().AsNoTracking().Include(o => o.Inner1).Include(o => o.Inner2).FirstAsync();
        var session1 = mapper.CreateMappingSession();
        var outer2 = session1.Map<Outer1, Outer2>(entity);
        outer2.Inner1 = session1.Map<Inner1_1, Inner2_1>(await DatabaseContext.Set<Inner1_1>().FirstAsync(i => i.LongProp == 2));
        outer2.Inner2 = session1.Map<Inner1_2, Inner2_2>(await DatabaseContext.Set<Inner1_2>().FirstAsync(i => i.StringProp == "2"));
        var session2 = mapper.CreateMappingToDatabaseSession(DatabaseContext);
        var result1 = await session2.MapAsync<Outer2, Outer1>(outer2);
        await DatabaseContext.SaveChangesAsync();

        // assert
        Assert.Equal(2, await DatabaseContext.Set<Inner1_1>().CountAsync());
        Assert.Equal(2, await DatabaseContext.Set<Inner1_2>().CountAsync());
    }
}
