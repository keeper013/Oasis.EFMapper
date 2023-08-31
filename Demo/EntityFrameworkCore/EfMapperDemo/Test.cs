namespace EfMapperDemo;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Common;
using Xunit;

public sealed class Test : IDisposable
{
    private readonly DbContextOptions _options;
    private readonly DbConnection _connection;

    public Test()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(_connection)
            .Options;
    }

    [Fact]
    public void Test1()
    {

    }

    public void Dispose() => _connection.Dispose();

    private async Task ExecuteWithNewDatabaseContext(Func<DbContext, Task> action)
    {
        using var databaseContext = new DatabaseContext(_options);
        databaseContext.Database.EnsureCreated();
        await action(databaseContext);
    }
}