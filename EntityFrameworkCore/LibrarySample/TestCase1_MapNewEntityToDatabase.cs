namespace LibrarySample;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using System.Threading.Tasks;
using Xunit;

public sealed class TestCase1_MapNewEntityToDatabase : TestBase
{
    [Fact]
    public async Task Test1_MapNewTagToDatabase()
    {
        // initialize mapper
        var factory = MakeDefaultMapperBuilder()
            .Register<NewTagDTO, Tag>()
            .Build();

        // create new tag
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var mapper = factory.MakeToDatabaseMapper(databaseContext);
            const string TagName = "English";
            var tagDto = new NewTagDTO { Name = TagName };
            _ = await mapper.MapAsync<NewTagDTO, Tag>(tagDto, null);
            _ = await databaseContext.SaveChangesAsync();
            var tag = await databaseContext.Set<Tag>().FirstAsync();
            Assert.Equal(TagName, tag.Name);
        });
    }
}
