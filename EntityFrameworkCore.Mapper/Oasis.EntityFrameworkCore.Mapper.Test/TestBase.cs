namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

public abstract class TestBase : IDisposable
{
    private bool _disposed;

    protected TestBase()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(connection)
            .Options;
        DatabaseContext = new DatabaseContext(options);
        DatabaseContext.Database.EnsureCreated();
    }

    protected DbContext DatabaseContext { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // free managed resources
            DatabaseContext.Database.EnsureDeleted();
            DatabaseContext.Dispose();
        }

        // free native resources if there are any.
        _disposed = true;
    }
}
