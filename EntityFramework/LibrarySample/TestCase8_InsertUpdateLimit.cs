namespace LibrarySample;

using Oasis.EntityFramework.Mapper;
using Oasis.EntityFramework.Mapper.Exceptions;
using Oasis.EntityFramework.Mapper.Sample;
using System.Data.Entity;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public sealed class TestCase8_InsertUpdateLimit : TestBase
{
    [Test]
    public async Task Test1_NewEntityInsertedWithEmptyId()
    {
        // initialize mapper
        var factory = MakeDefaultMapperBuilder()
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Register<UpdateBookDTO, Book>()
            .Build();

        var updateBookDto = new UpdateBookDTO { Name = "Test Book 1" };
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var mapper = factory.MakeToDatabaseMapper(databaseContext);
            _ = await mapper.MapAsync<UpdateBookDTO, Book>(updateBookDto, null);
            _ = await databaseContext.SaveChangesAsync();
            Assert.AreEqual(1, await databaseContext.Set<Book>().CountAsync());
        });
    }

    [Test]
    public void Test2_ExcetionThrownWhenUsageLimited()
    {
        // initialize mapper
        var factory = MakeDefaultMapperBuilder()
            .WithScalarConverter<string, long>(s => long.Parse(s))
            .Configure<UpdateBookDTO, Book>()
                .SetMapToDatabaseType(MapToDatabaseType.Update)
                .Finish()
            .Build();

        var updateBookDto = new UpdateBookDTO { Name = "Test Book 1" };
        Assert.ThrowsAsync<UpdateToDatabaseWithoutIdException>(async () =>
        {
            await ExecuteWithNewDatabaseContext(async databaseContext =>
            {
                var mapper = factory.MakeToDatabaseMapper(databaseContext);
                _ = await mapper.MapAsync<UpdateBookDTO, Book>(updateBookDto, null);
            });
        });
    }
}
