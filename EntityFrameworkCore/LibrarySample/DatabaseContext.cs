namespace LibrarySample;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

internal class DatabaseContext : DbContext
{
    private static readonly EntityState[] _states = new[] { EntityState.Added, EntityState.Modified };

    public DatabaseContext(DbContextOptions options)
        : base(options)
    {
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateConcurrencyTokens();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateConcurrencyTokens();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>().ToTable(nameof(Tag));
        modelBuilder.Entity<Tag>().HasKey(t => t.Id);
        modelBuilder.Entity<Tag>().Property(t => t.Id).HasColumnName(nameof(Tag.Id)).ValueGeneratedOnAdd();
        modelBuilder.Entity<Tag>().HasIndex(t => t.Name).IsUnique();

        modelBuilder.Entity<Contact>().ToTable(nameof(Contact));
        modelBuilder.Entity<Contact>().HasKey(c => c.Id);
        modelBuilder.Entity<Contact>().Property(c => c.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<Contact>().Property(c => c.ConcurrencyToken).IsRequired().IsRowVersion();

        modelBuilder.Entity<Borrower>().ToTable(nameof(Borrower));
        modelBuilder.Entity<Borrower>().HasKey(b => b.IdentityNumber);
        modelBuilder.Entity<Borrower>().Property(b => b.ConcurrencyToken).IsRequired().IsRowVersion();

        modelBuilder.Entity<Book>().ToTable(nameof(Book));
        modelBuilder.Entity<Book>().HasKey(b => b.Id);
        modelBuilder.Entity<Book>().Property(b => b.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<Book>().Property(b => b.ConcurrencyToken).IsRequired().IsRowVersion();

        modelBuilder.Entity<Copy>().ToTable(nameof(Copy));
        modelBuilder.Entity<Copy>().HasKey(c => c.Number);
        modelBuilder.Entity<Copy>().Property(c => c.Number);
        modelBuilder.Entity<Copy>().Property(c => c.ConcurrencyToken).IsRequired().IsRowVersion();

        modelBuilder.Entity<Tag>().HasMany(t => t.Books).WithMany(b => b.Tags);
        modelBuilder.Entity<Book>().HasMany(b => b.Copies).WithOne().HasForeignKey(c => c.BookId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Borrower>().HasOne(b => b.Contact).WithOne().HasForeignKey<Contact>(c => c.Borrower).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Borrower>().HasOne(b => b.Reserved).WithOne().HasForeignKey<Copy>(c => c.Reserver).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Borrower>().HasMany(b => b.Borrowed).WithOne().HasForeignKey(c => c.Borrower).OnDelete(DeleteBehavior.Restrict);

        // for sqlite
        var concurrencyTokenProperties = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(byte[])
                && p.ValueGenerated == ValueGenerated.OnAddOrUpdate
                && p.IsConcurrencyToken);

        foreach (var property in concurrencyTokenProperties)
        {
            /// sqlite doesn't provide IsRowVersion feature, so this is done by overriding SaveChanges and SaveChangesAsync.
            /// ValueGenerated.OnAddOrUpdate will block changes done to this property in the methods listed above
            property.ValueGenerated = ValueGenerated.Never;
            property.SetValueConverter(new SqliteConcurrencyTokenConverter());
            property.SetValueComparer(new ValueComparer<byte[]>(
                (a1, a2) => a1 != null && a2 != null && a1.SequenceEqual(a2),
                a => a.Aggregate(0, (v, b) => HashCode.Combine(v, b.GetHashCode())),
                a => a));
        }
    }

    // sqlite doesn't support row version feature, have to do this
    private void UpdateConcurrencyTokens()
    {
        var toBeUpdatedEntities = ChangeTracker.Entries().Where(e => _states.Contains(e.State) && e.Metadata.ClrType.GetInterface(nameof(IEntityBaseWithConcurrencyToken)) != null);
        if (toBeUpdatedEntities.Any())
        {
            var bytes = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            foreach (var entity in toBeUpdatedEntities)
            {
                var property = entity.Property(nameof(IEntityBaseWithConcurrencyToken.ConcurrencyToken));
                property.CurrentValue = Encoding.UTF8.GetBytes(bytes);
            }
        }
    }

    private class SqliteConcurrencyTokenConverter : ValueConverter<byte[], string>
    {
        public SqliteConcurrencyTokenConverter()
            : base(v => ToDb(v), v => FromDb(v))
        {
        }

        private static byte[] FromDb(string v) => v.Select(c => (byte)c).ToArray();

        private static string ToDb(byte[] v) => new (v.Select(b => (char)b).ToArray());
    }
}

