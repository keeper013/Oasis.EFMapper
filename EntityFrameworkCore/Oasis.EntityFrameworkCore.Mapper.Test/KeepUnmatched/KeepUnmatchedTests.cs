namespace Oasis.EntityFrameworkCore.Mapper.Test.KeepUnmatched;

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public sealed class KeepUnmatchedTests : TestBase
{
    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public async Task KeepUnmatchedTest(bool keepUnmatched, int count)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapper = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration)
            .RegisterTwoWay<UnmatchedPrincipal1, UnmatchedPrincipal2>()
            .Build();

        // act
        var principal = new UnmatchedPrincipal2 { DependantList = new List<UnmatchedDependant2> { new UnmatchedDependant2 { IntProp = 1 } } };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            await mapper.MapAsync<UnmatchedPrincipal2, UnmatchedPrincipal1>(principal, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<UnmatchedPrincipal1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<UnmatchedDependant1>().CountAsync());
        });

        UnmatchedPrincipal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<UnmatchedPrincipal1>().Include(p => p.DependantList).FirstAsync();
            mappedPrincipal = mapper.Map<UnmatchedPrincipal1, UnmatchedPrincipal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.DependantList.Clear();
            mappedPrincipal.DependantList.Add(new UnmatchedDependant2 { IntProp = 2 });
            await mapper.MapAsync<UnmatchedPrincipal2, UnmatchedPrincipal1>(mappedPrincipal, databaseContext, p => p.Include(p => p.DependantList), MapToDatabaseType.Update, keepUnmatched);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<UnmatchedPrincipal1>().CountAsync());
        });

        // assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<UnmatchedPrincipal1>().Include(p => p.DependantList).FirstAsync();
            Assert.Equal(count, principal.DependantList.Count);
            Assert.Single(principal.DependantList.Where(d => d.IntProp == 2));
        });
    }
}
