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
        var mapper = MakeDefaultMapperBuilder()
            .RegisterTwoWay<UnmatchedPrincipal1, UnmatchedPrincipal2>()
            .Build();

        // act
        var principal = new UnmatchedPrincipal2 { DependentList = new List<UnmatchedDependent2> { new UnmatchedDependent2 { IntProp = 1 } } };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            await mapper.MapAsync<UnmatchedPrincipal2, UnmatchedPrincipal1>(principal, null, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<UnmatchedPrincipal1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<UnmatchedDependent1>().CountAsync());
        });

        UnmatchedPrincipal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<UnmatchedPrincipal1>().Include(p => p.DependentList).FirstAsync();
            mappedPrincipal = mapper.Map<UnmatchedPrincipal1, UnmatchedPrincipal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.DependentList.Clear();
            mappedPrincipal.DependentList.Add(new UnmatchedDependent2 { IntProp = 2 });
            await mapper.MapAsync<UnmatchedPrincipal2, UnmatchedPrincipal1>(mappedPrincipal, p => p.Include(p => p.DependentList), databaseContext, keepUnmatched);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<UnmatchedPrincipal1>().CountAsync());
        });

        // assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<UnmatchedPrincipal1>().Include(p => p.DependentList).FirstAsync();
            Assert.Equal(count, principal.DependentList.Count);
            Assert.Single(principal.DependentList.Where(d => d.IntProp == 2));
        });
    }
}
