namespace Oasis.EntityFrameworkCore.Mapper.Test.KeepUnmatched;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public sealed class KeepUnmatchedTests : TestBase
{
    [Fact]
    public void KeepUnmatched_MatchExcluded_ShouldFail()
    {
        Assert.Throws<KeepUnmatchedPropertyExcludedException>(() =>
        {
            MakeDefaultMapperBuilder()
                .Configure<UnmatchedPrincipal2, UnmatchedPrincipal1>()
                .KeepUnmatched(nameof(UnmatchedPrincipal2.DependentList))
                .ExcludePropertiesByName(nameof(UnmatchedPrincipal2.DependentList))
                .Finish();
        });
    }

    [Fact]
    public void KeepUnmatched_TypeExcluded_ShouldFail()
    {
        Assert.Throws<KeepUnmatchedPropertyExcludedException>(() =>
        {
            MakeDefaultMapperBuilder()
                .Configure<UnmatchedPrincipal1>()
                .KeepUnmatched(nameof(UnmatchedPrincipal1.DependentList))
                .ExcludePropertiesByName(nameof(UnmatchedPrincipal1.DependentList))
                .Finish();
        });
    }

    [Fact]
    public void KeepUnmatched_CustomMatch_ShouldFail()
    {
        Assert.Throws<CustomMappingPropertyKeepUnmatchedException>(() =>
        {
            MakeDefaultMapperBuilder()
                .Configure<UnmatchedPrincipal2, UnmatchedPrincipal1>()
                .MapProperty(p1 => p1.DependentList, p2 => new List<UnmatchedDependent1>())
                .KeepUnmatched(nameof(UnmatchedPrincipal2.DependentList))
                .Finish();
        });
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public async Task KeepUnmatched_Mapping_Test(bool keepUnmatched, int count)
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder();
        if (keepUnmatched)
        {
            mapperBuilder = mapperBuilder.Configure<UnmatchedPrincipal2, UnmatchedPrincipal1>().KeepUnmatched(nameof(UnmatchedPrincipal2.DependentList)).Finish();
        }

        var mapper = mapperBuilder.RegisterTwoWay<UnmatchedPrincipal1, UnmatchedPrincipal2>().Build().MakeMapper();

        await DoTest(mapper, count);
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public async Task KeepUnmatched_Type_Test(bool keepUnmatched, int count)
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder();
        if (keepUnmatched)
        {
            mapperBuilder = mapperBuilder.Configure<UnmatchedPrincipal1>().KeepUnmatched(nameof(UnmatchedPrincipal2.DependentList)).Finish();
        }

        var mapper = mapperBuilder.RegisterTwoWay<UnmatchedPrincipal1, UnmatchedPrincipal2>().Build().MakeMapper();

        await DoTest(mapper, count);
    }

    private async Task DoTest(IMapper mapper, int count)
    {
        // act
        var principal = new UnmatchedPrincipal2 { DependentList = new List<UnmatchedDependent2> { new UnmatchedDependent2 { IntProp = 1 } } };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            await mapper.MapAsync<UnmatchedPrincipal2, UnmatchedPrincipal1>(principal, null);
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
            mapper.DatabaseContext = databaseContext;
            mappedPrincipal.DependentList.Clear();
            mappedPrincipal.DependentList.Add(new UnmatchedDependent2 { IntProp = 2 });
            await mapper.MapAsync<UnmatchedPrincipal2, UnmatchedPrincipal1>(mappedPrincipal, p => p.Include(p => p.DependentList));
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
