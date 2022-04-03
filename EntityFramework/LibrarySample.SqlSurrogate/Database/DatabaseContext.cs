namespace Oasis.EntityFramework.Mapper.Test.SqlSurrogate;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFramework.Mapper.Sample;

public class DatabaseContext : DbContext
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
        modelBuilder.Entity<BorrowRecord>().Property(r => r.BookId).HasColumnName("Book_Id");
        modelBuilder.Entity<BorrowRecord>().HasOne(r => r.Borrower).WithMany(b => b!.BorrowRecords).HasForeignKey(r => r.BorrowerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BorrowRecord>().HasOne(r => r.Book).WithOne(b => b!.BorrowRecord!).HasForeignKey<BorrowRecord>(r => r.BookId).OnDelete(DeleteBehavior.Cascade);
    }
}
