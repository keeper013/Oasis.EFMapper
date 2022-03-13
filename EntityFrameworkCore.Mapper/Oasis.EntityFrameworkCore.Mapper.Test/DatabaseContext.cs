namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;

internal class DatabaseContext : DbContext
{
    public static readonly byte[] DefaultTimeStamp = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };

    public static readonly byte[] ChangedTimeStamp1 = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 };

    public DatabaseContext(DbContextOptions options)
        : base(options)
    {
    }

    protected DatabaseContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScalarEntity1>().ToTable("ScalarEntity1");
        modelBuilder.Entity<RecursiveEntity1>().ToTable("RecursiveEntity1");
        modelBuilder.Entity<CollectionEntity1>().ToTable("CollectionEntity1");
        modelBuilder.Entity<DerivedEntity1>().ToTable("DerivedEntity1");
        modelBuilder.Entity<DerivedEntity1_1>().ToTable("DerivedEntity1_1");
    }
}
