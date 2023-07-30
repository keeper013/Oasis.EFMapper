using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using System.Threading.Tasks;
using System;

namespace LibrarySample;

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

    protected EntityConfiguration DefaultConfiguration = new EntityConfiguration(nameof(IEntityBaseWithId.Id), nameof(IEntityBaseWithConcurrencyToken.ConcurrencyToken));

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
