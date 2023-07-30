namespace Oasis.EntityFramework.Mapper.Test.KeepUnmatched;

using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

public sealed class KeepUnmatchedTests : TestBase
{
    [Theory]
    [TestCase(true, 2)]
    [TestCase(false, 1)]
    public async Task KeepUnmatchedTest(bool keepUnmatched, int count)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapper = factory.MakeMapperBuilder(DefaultConfiguration)
            .RegisterTwoWay<UnmatchedPrincipal1, UnmatchedPrincipal2>()
            .Build();

        // act
        var principal = new UnmatchedPrincipal2 { DependantList = new List<UnmatchedDependant2> { new UnmatchedDependant2 { IntProp = 1 } } };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            await mapper.MapAsync<UnmatchedPrincipal2, UnmatchedPrincipal1>(principal, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.AreEqual(1, await databaseContext.Set<UnmatchedPrincipal1>().CountAsync());
            Assert.AreEqual(1, await databaseContext.Set<UnmatchedDependant1>().CountAsync());
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
            Assert.AreEqual(1, await databaseContext.Set<UnmatchedPrincipal1>().CountAsync());
        });

        // assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<UnmatchedPrincipal1>().Include(p => p.DependantList).FirstAsync();
            Assert.AreEqual(count, principal.DependantList.Count);
            Assert.AreEqual(1, principal.DependantList.Where(d => d.IntProp == 2).Count());
        });
    }
}
