namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using System;

internal class DatabaseContext : DbContext
{
    public static readonly byte[] EmptyTimeStamp = Array.Empty<byte>();
    public DatabaseContext(DbContextOptions options) : base(options)
    {
    }

    protected DatabaseContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: refine this part
        modelBuilder.Entity<ScalarClass1>().ToTable("ScalarClass1");
    }
}
