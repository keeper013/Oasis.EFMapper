namespace LibrarySample;

using Oasis.EntityFramework.Mapper.Sample;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Data.Entity;

[TestFixture]
public sealed class TestCase1_MapNewEntityToDatabase : TestBase
{
    [Test]
    public async Task Test1_MapNewTagToDatabase()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .Register<NewTagDTO, Tag>()
            .Build();

        // create new tag
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            const string TagName = "English";
            var tagDto = new NewTagDTO { Name = TagName };
            _ = await mapper.MapAsync<NewTagDTO, Tag>(tagDto, null, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            var tag = await databaseContext.Set<Tag>().FirstAsync();
            Assert.AreEqual(TagName, tag.Name);
        });
    }
}
