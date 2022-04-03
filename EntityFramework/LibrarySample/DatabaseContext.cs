namespace Oasis.EntityFramework.Mapper.Sample;

using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SQLite;
using System.Data.SQLite.EF6;

internal class DatabaseContext : DbContext
{
    public DatabaseContext(DbConnection connection)
        : base(connection, false)
    {
        DbConfiguration.SetConfiguration(new SQLiteConfiguration());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Borrower>().ToTable(nameof(Borrower));
        modelBuilder.Entity<Book>().ToTable(nameof(Book));
        modelBuilder.Entity<BorrowRecord>().ToTable(nameof(BorrowRecord));
        modelBuilder.Entity<BorrowRecord>().HasRequired(r => r.Borrower).WithMany(b => b!.BorrowRecords).HasForeignKey(r => r.BorrowerId).WillCascadeOnDelete(true);
        modelBuilder.Entity<Book>().HasOptional(b => b.BorrowRecord).WithOptionalPrincipal(r => r!.Book!).WillCascadeOnDelete(true);
    }
}

public class SQLiteConfiguration : DbConfiguration
{
    public SQLiteConfiguration()
    {
        SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
        SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
        var providerServices = (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices));
        SetProviderServices("System.Data.SQLite", providerServices);
        SetProviderServices("System.Data.SQLite.EF6", providerServices);
    }
}