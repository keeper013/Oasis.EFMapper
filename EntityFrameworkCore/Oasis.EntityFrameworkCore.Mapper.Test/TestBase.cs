namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

public abstract class TestBase
{
    private readonly DbContextOptions _options;

    protected TestBase()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(connection)
            .Options;
    }

    protected static IMapperBuilder MakeDefaultMapperBuilder(IMapperBuilderFactory factory)
    {
        return factory.MakeMapperBuilder(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken));
    }

    protected static IMapperBuilder MakeDefaultMapperBuilder(IMapperBuilderFactory factory, bool keepEntityOnMappingRemoved)
    {
        return factory.MakeMapperBuilder(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), null, keepEntityOnMappingRemoved);
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
}
