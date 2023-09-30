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
        var mapper = MakeDefaultMapperBuilder().Register<ManyToManyParent1, ManyToManyParent2>().Build().MakeToMemoryMapper();
        var child1 = new ManyToManyChild1 { Name = "child1" };
        var parent1 = new ManyToManyParent1 { Name = "parent1" };
        parent1.Children.Add(child1);
        var parent2 = new ManyToManyParent1 { Name = "parent2" };
        parent2.Children.Add(child1);

        // Act & Assert
        mapper.StartSession();
        var result1 = mapper.Map<ManyToManyParent1, ManyToManyParent2>(parent1);
        var result2 = mapper.Map<ManyToManyParent1, ManyToManyParent2>(parent2);
        mapper.StopSession();
        Assert.Equal(result1.Children[0].GetHashCode(), result2.Children[0].GetHashCode());
    }

    [Fact]
    public async Task MapListProperties_ToDatabase_SameInstance_ShouldMapToSameInstance()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<ManyToManyParent1, ManyToManyParent2>().Build().MakeToDatabaseMapper();
        var child1 = new ManyToManyChild1 { Name = "child1" };
        var parent1 = new ManyToManyParent1 { Name = "parent1" };
        parent1.Children.Add(child1);
        var parent2 = new ManyToManyParent1 { Name = "parent2" };
        parent2.Children.Add(child1);

        // Act & Assert
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.StartSession();
            mapper.DatabaseContext = databaseContext;
            var result1 = await mapper.MapAsync<ManyToManyParent1, ManyToManyParent2>(parent1, null);
            var result2 = await mapper.MapAsync<ManyToManyParent1, ManyToManyParent2>(parent2, null);
            mapper.StopSession();
            Assert.Equal(result1.Children[0].GetHashCode(), result2.Children[0].GetHashCode());
            await databaseContext.SaveChangesAsync();
            var subs = await databaseContext.Set<ManyToManyChild2>().ToListAsync();
            Assert.Single(subs);
        });
    }
}
