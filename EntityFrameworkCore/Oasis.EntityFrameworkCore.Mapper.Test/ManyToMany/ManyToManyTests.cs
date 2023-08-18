namespace Oasis.EntityFrameworkCore.Mapper.Test.ManyToMany;

using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

public class ManyToManyTests : TestBase
{
    [Fact]
    public void MapListProperties_ToMemory_SameInstance_ShouldMapToSameInstance()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<ManyToManyParent1, ManyToManyParent2>().Build();
        var child1 = new ManyToManyChild1 { Name = "child1" };
        var parent1 = new ManyToManyParent1 { Name = "parent1" };
        parent1.Children.Add(child1);
        var parent2 = new ManyToManyParent1 { Name = "parent2" };
        parent2.Children.Add(child1);

        // Act & Assert
        var session = mapper.CreateMappingSession();
        var result1 = session.Map<ManyToManyParent1, ManyToManyParent2>(parent1);
        var result2 = session.Map<ManyToManyParent1, ManyToManyParent2>(parent2);
        Assert.Equal(result1.Children[0].GetHashCode(), result2.Children[0].GetHashCode());
    }

    [Fact]
    public async Task MapListProperties_ToDatabase_SameInstance_ShouldMapToSameInstance()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<ManyToManyParent1, ManyToManyParent2>().Build();
        var child1 = new ManyToManyChild1 { Name = "child1" };
        var parent1 = new ManyToManyParent1 { Name = "parent1" };
        parent1.Children.Add(child1);
        var parent2 = new ManyToManyParent1 { Name = "parent2" };
        parent2.Children.Add(child1);

        // Act & Assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            var result1 = await session.MapAsync<ManyToManyParent1, ManyToManyParent2>(parent1, null);
            var result2 = await session.MapAsync<ManyToManyParent1, ManyToManyParent2>(parent2, null);
            Assert.Equal(result1.Children[0].GetHashCode(), result2.Children[0].GetHashCode());
            await databaseContext.SaveChangesAsync();
            var subs = await databaseContext.Set<ManyToManyChild2>().ToListAsync();
            Assert.Single(subs);
        });
    }
}
