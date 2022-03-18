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
        var mapperBuilder = factory.Make(GetType().Name);
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
}
