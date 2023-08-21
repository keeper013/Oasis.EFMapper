namespace LibrarySample;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Threading.Tasks;
using System.Threading;

internal class DatabaseContext : DbContext
{
    public DatabaseContext(DbConnection connection)
        : base(connection, false)
    {
        DbConfiguration.SetConfiguration(new SQLiteConfiguration());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>().ToTable(nameof(Tag));
        modelBuilder.Entity<Tag>().HasKey(t => t.Id);
        modelBuilder.Entity<Tag>().Property(t => t.Id).HasColumnName(nameof(Tag.Id)).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        modelBuilder.Entity<Tag>().HasIndex(t => t.Name).IsUnique();

        modelBuilder.Entity<Contact>().ToTable(nameof(Contact));
        modelBuilder.Entity<Contact>().HasKey(c => c.Id);
        modelBuilder.Entity<Contact>().Property(c => c.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        modelBuilder.Entity<Contact>().Property(c => c.ConcurrencyToken).IsRequired();

        modelBuilder.Entity<Borrower>().ToTable(nameof(Borrower));
        modelBuilder.Entity<Borrower>().HasKey(b => b.IdentityNumber);
        modelBuilder.Entity<Borrower>().Property(b => b.ConcurrencyToken).IsRequired();

        modelBuilder.Entity<Book>().ToTable(nameof(Book));
        modelBuilder.Entity<Book>().HasKey(b => b.Id);
        modelBuilder.Entity<Book>().Property(b => b.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        modelBuilder.Entity<Book>().Property(b => b.ConcurrencyToken).IsRequired();

        modelBuilder.Entity<Copy>().ToTable(nameof(Copy));
        modelBuilder.Entity<Copy>().HasKey(c => c.Number);
        modelBuilder.Entity<Copy>().Property(c => c.Number).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        modelBuilder.Entity<Copy>().Property(c => c.ConcurrencyToken).IsRequired();

        modelBuilder.Entity<Tag>().HasMany(t => t.Books).WithMany(b => b.Tags).Map(m =>
        {
            m.ToTable("BookTag");
            m.MapLeftKey("BooksId");
            m.MapRightKey("TagsId");
        }); ;
        modelBuilder.Entity<Book>().HasMany(b => b.Copies).WithRequired().HasForeignKey(c => c.BookId).WillCascadeOnDelete(true);
        modelBuilder.Entity<Borrower>().HasRequired(b => b.Contact).WithRequiredPrincipal(c => c.Borrower).WillCascadeOnDelete(true);
        modelBuilder.Entity<Borrower>().HasOptional(b => b.Reserved).WithOptionalPrincipal(r => r.Reserver!).WillCascadeOnDelete(false);
        modelBuilder.Entity<Borrower>().HasMany(b => b.Borrowed).WithOptional().HasForeignKey(c => c.Borrower).WillCascadeOnDelete(false);

        modelBuilder.Properties().Where(p => string.Equals(p.Name, nameof(IEntityBaseWithConcurrencyToken.ConcurrencyToken))).Configure(p => p.IsConcurrencyToken().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None));
    }

    public override int SaveChanges()
    {
        GenerateRowVersion();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        GenerateRowVersion();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void GenerateRowVersion()
    {
        var objectContext = ((IObjectContextAdapter)this).ObjectContext;
        foreach (var entry in objectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified))
        {
            if (entry.Entity is IEntityBaseWithConcurrencyToken v1 && v1 != null)
            {
                v1.ConcurrencyToken = v1.ConcurrencyToken + 1;
            }
        }
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