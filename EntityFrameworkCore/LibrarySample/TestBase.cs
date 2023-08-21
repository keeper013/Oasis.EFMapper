namespace LibrarySample;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using System.Threading.Tasks;
using System;
using Oasis.EntityFrameworkCore.Mapper;

public abstract class TestBase
{
    protected readonly DbContextOptions _options;

    protected TestBase()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(connection)
            .Options;
    }

    protected DbContext CreateDatabaseContext()
    {
        var databaseContext = new DatabaseContext(_options);
        databaseContext.Database.EnsureCreated();

        return databaseContext;
    }

    protected async Task ExecuteWithNewDatabaseContext(Func<DbContext, Task> action)
    {
        using (var databaseContext = CreateDatabaseContext())
        {
            await action(databaseContext);
        }
    }

    protected static IMapperBuilder MakeDefaultMapperBuilder(string[]? excludedProperties = null)
    {
        return new MapperBuilderFactory()
            .Configure()
                .SetKeyPropertyNames(nameof(IEntityBaseWithId.Id), nameof(IEntityBaseWithConcurrencyToken.ConcurrencyToken))
                .ExcludedPropertiesByName(excludedProperties)
                .Finish()
            .MakeMapperBuilder();
    }
}
