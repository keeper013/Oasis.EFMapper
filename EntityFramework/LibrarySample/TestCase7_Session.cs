﻿namespace LibrarySample;

using Oasis.EntityFramework.Mapper.Sample;
using System.Data.Entity;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Data.Entity.Infrastructure;

[TestFixture]
public sealed class TestCase7_Session : TestBase
{
    [Test]
    public void Test1_MapWithoutSession_EntityDuplicated()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .Register<NewBookWithNewTagDTO, Book>()
            .Build();

        Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await ExecuteWithNewDatabaseContext(async databaseContext =>
            {
                var tag = new NewTagDTO { Name = "Tag1" };
                var book1 = new NewBookWithNewTagDTO { Name = "Book1" };
                book1.Tags.Add(tag);
                var book2 = new NewBookWithNewTagDTO { Name = "Book2" };
                book2.Tags.Add(tag);
                _ = await mapper.MapAsync<NewBookWithNewTagDTO, Book>(book1, null, databaseContext);
                _ = await mapper.MapAsync<NewBookWithNewTagDTO, Book>(book2, null, databaseContext);
                _ = await databaseContext.SaveChangesAsync();
            });
        });
    }

    [Test]
    public async Task Test2_MapWithSession_EntityNotDuplicated()
    {
        // initialize mapper
        var mapper = MakeDefaultMapperBuilder()
            .Register<NewBookWithNewTagDTO, Book>()
            .Build();

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var tag = new NewTagDTO { Name = "Tag1" };
            var book1 = new NewBookWithNewTagDTO { Name = "Book1" };
            book1.Tags.Add(tag);
            var book2 = new NewBookWithNewTagDTO { Name = "Book2" };
            book2.Tags.Add(tag);
            var session = mapper.CreateMappingToDatabaseSession(databaseContext);
            _ = await session.MapAsync<NewBookWithNewTagDTO, Book>(book1, null);
            _ = await session.MapAsync<NewBookWithNewTagDTO, Book>(book2, null);
            _ = await databaseContext.SaveChangesAsync();
            Assert.AreEqual(1, await databaseContext.Set<Tag>().CountAsync());
        });
    }
}