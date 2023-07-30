namespace LibrarySample;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using Oasis.EntityFrameworkCore.Mapper;
using System.Threading.Tasks;
using Xunit;

public sealed class TestCase1_MapNewEntityToDatabase : TestBase
{
    [Fact]
    public async Task Test1_MapNewTagToDatabase()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder();
        var mapper = mapperBuilder
            .Register<NewTagDTO, Tag>()
            .Register<Tag, TagDTO>()
            .Build();

        // create new tag
        const string TagName = "English";
        Tag tag = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var tagDto = new NewTagDTO { Name = TagName };
            _ = await mapper.MapAsync<NewTagDTO, Tag>(tagDto, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            tag = await databaseContext.Set<Tag>().FirstAsync();
            Assert.Equal(TagName, tag.Name);
        });

        // map from tag to dto
        var tagDto = mapper.Map<Tag, TagDTO>(tag);
        Assert.NotEqual(default, tagDto.Id);
        Assert.Equal(TagName, tagDto.Name);
    }
}
