namespace Oasis.EntityFrameworkCore.Mapper.Sample;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;

internal class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Borrower>().ToTable(nameof(Borrower));
        modelBuilder.Entity<Book>().ToTable(nameof(Book));
        modelBuilder.Entity<BorrowRecord>().ToTable(nameof(BorrowRecord));
        modelBuilder.Entity<BorrowRecord>().HasOne(r => r.Borrower).WithMany(b => b.BorrowRecords).HasForeignKey(r => r.BorrowerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BorrowRecord>().HasOne(r => r.Book).WithOne(b => b.BorrowRecord).HasForeignKey<BorrowRecord>(r => r.BookId).OnDelete(DeleteBehavior.Cascade);

        if (Database.IsSqlite())
        {
            var timestampProperties = modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(byte[])
                    && p.ValueGenerated == ValueGenerated.OnAddOrUpdate
                    && p.IsConcurrencyToken);

            foreach (var property in timestampProperties)
            {
                property.SetValueConverter(new SqliteTimestampConverter());
                property.SetValueComparer(new ValueComparer<byte[]>(
                    (a1, a2) => a1 != default && a2 != default && a1.SequenceEqual(a2),
                    a => a.Aggregate(0, (v, b) => HashCode.Combine(v, b.GetHashCode())),
                    a => a));
                property.SetDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }
    }

    private class SqliteTimestampConverter : ValueConverter<byte[], string>
    {
        public SqliteTimestampConverter()
            : base(
                v => ToDb(v),
                v => FromDb(v))
        {
        }

        private static byte[] FromDb(string v) => v.Select(c => (byte)c).ToArray();

        private static string ToDb(byte[] v) => new (v.Select(b => (char)b).ToArray());
    }
}

