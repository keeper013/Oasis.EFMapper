namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

public abstract class TestBase : IDisposable
{
    private readonly DbContextOptions _options;
    private readonly SqliteConnection _connection;

    protected TestBase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose() => _connection.Close();

    protected static IMapperBuilder MakeDefaultMapperBuilder(string[]? excludedProperties = null)
    {
        return new MapperBuilderFactory()
            .Configure()
                .SetKeyPropertyNames(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken))
                .ExcludedPropertiesByName(excludedProperties)
                .Finish()
            .MakeMapperBuilder();
    }

    protected async Task ExecuteWithNewDatabaseContext(Func<DbContext, Task> action)
    {
        using var databaseContext = new DatabaseContext(_options);
        databaseContext.Database.EnsureCreated();
        await action(databaseContext);
    }
}
