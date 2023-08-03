namespace Oasis.EntityFramework.Mapper.Test;

using NUnit.Framework;
using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

public abstract class TestBase
{
    private DbConnection? _connection;

    [SetUp]
    public void Setup()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();
        var sql = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}/script.sql");
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    protected static IMapperBuilder MakeDefaultMapperBuilder(string[]? excludedProperties = null, bool? keepEntityOnMappingRemoved = null)
    {
        return new MapperBuilderFactory()
            .Configure()
                .SetKeyPropertyNames(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken))
                .ExcludedPropertiesByName(excludedProperties)
                .SetKeepEntityOnMappingRemoved(keepEntityOnMappingRemoved)
                .Finish()
            .MakeMapperBuilder();
    }

    protected DbContext CreateDatabaseContext()
    {
        var databaseContext = new DatabaseContext(_connection!);

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
