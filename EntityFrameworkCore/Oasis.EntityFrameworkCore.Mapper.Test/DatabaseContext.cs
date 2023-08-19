namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oasis.EntityFrameworkCore.Mapper.Test.DependentProperty;
using Oasis.EntityFrameworkCore.Mapper.Test.KeepUnmatched;
using Oasis.EntityFrameworkCore.Mapper.Test.KeyPropertyType;
using Oasis.EntityFrameworkCore.Mapper.Test.ManyToMany;
using Oasis.EntityFrameworkCore.Mapper.Test.OneToMany;
using Oasis.EntityFrameworkCore.Mapper.Test.OneToOne;
using Oasis.EntityFrameworkCore.Mapper.Test.Scalar;
using Oasis.EntityFrameworkCore.Mapper.Test.ToDatabase;
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
        modelBuilder.Entity<ScalarEntity1>().ToTable(nameof(ScalarEntity1));
        modelBuilder.Entity<CollectionEntity1>().ToTable(nameof(CollectionEntity1));
        modelBuilder.Entity<DerivedEntity1>().ToTable(nameof(DerivedEntity1));
        modelBuilder.Entity<ScalarEntity1Item>().ToTable(nameof(ScalarEntity1Item));
        modelBuilder.Entity<DerivedEntity1_1>().ToTable(nameof(DerivedEntity1_1));
        modelBuilder.Entity<SubScalarEntity1>().ToTable(nameof(SubScalarEntity1));
        modelBuilder.Entity<ListEntity2>().ToTable(nameof(ListEntity2));
        modelBuilder.Entity<SubEntity2>().ToTable(nameof(SubEntity2));
        modelBuilder.Entity<DependentOptional1_1>().ToTable(nameof(DependentOptional1_1));
        modelBuilder.Entity<DependentOptional1_2>().ToTable(nameof(DependentOptional1_2));
        modelBuilder.Entity<PrincipalOptional1>().ToTable(nameof(PrincipalOptional1));
        modelBuilder.Entity<DependentRequired1_1>().ToTable(nameof(DependentRequired1_1));
        modelBuilder.Entity<DependentRequired1_2>().ToTable(nameof(DependentRequired1_2));
        modelBuilder.Entity<PrincipalRequired1>().ToTable(nameof(PrincipalRequired1));
        modelBuilder.Entity<SessionTestingList1_1>().ToTable(nameof(SessionTestingList1_1));
        modelBuilder.Entity<SessionTestingList1_2>().ToTable(nameof(SessionTestingList1_2));
        modelBuilder.Entity<ManyToManyParent2>().ToTable(nameof(ManyToManyParent2));
        modelBuilder.Entity<ManyToManyChild2>().ToTable(nameof(ManyToManyChild2));
        modelBuilder.Entity<ScalarItem1>().ToTable(nameof(ScalarItem1));
        modelBuilder.Entity<SomeSourceEntity<byte>>().ToTable("ByteEntity");
        modelBuilder.Entity<SomeSourceEntity<byte?>>().ToTable("NByteEntity");
        modelBuilder.Entity<SomeSourceEntity<short>>().ToTable("ShortEntity");
        modelBuilder.Entity<SomeSourceEntity<short?>>().ToTable("NShortEntity");
        modelBuilder.Entity<SomeSourceEntity<ushort>>().ToTable("UShortEntity");
        modelBuilder.Entity<SomeSourceEntity<ushort?>>().ToTable("NUShortEntity");
        modelBuilder.Entity<SomeSourceEntity<int>>().ToTable("IntEntity");
        modelBuilder.Entity<SomeSourceEntity<int?>>().ToTable("NIntEntity");
        modelBuilder.Entity<SomeSourceEntity<uint>>().ToTable("UIntEntity");
        modelBuilder.Entity<SomeSourceEntity<uint?>>().ToTable("NUIntEntity");
        modelBuilder.Entity<SomeSourceEntity<long>>().ToTable("LongEntity");
        modelBuilder.Entity<SomeSourceEntity<long?>>().ToTable("NLongEntity");
        modelBuilder.Entity<SomeSourceEntity<ulong>>().ToTable("ULongEntity");
        modelBuilder.Entity<SomeSourceEntity<ulong?>>().ToTable("NULongEntity");
        modelBuilder.Entity<SomeSourceEntity<string>>().ToTable("StringEntity");
        modelBuilder.Entity<SomeSourceEntity<byte[]>>().ToTable("ByteArrayEntity");
        modelBuilder.Entity<SomeSourceEntity<Guid>>().ToTable("GuidEntity");
        modelBuilder.Entity<SomeSourceEntity<Guid?>>().ToTable("NGuidEntity");
        modelBuilder.Entity<RecursiveEntity1>().ToTable(nameof(RecursiveEntity1));
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.ListIEntity).WithMany(l => l.Scs).HasForeignKey(s => s.ListIEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.ListEntity).WithMany(l => l.Scs).HasForeignKey(s => s.ListEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.CollectionEntity).WithMany(c => c.Scs).HasForeignKey(s => s.CollectionEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubEntity2>().HasOne(s => s.ListEntity).WithMany(l => l.SubEntities).HasForeignKey(s => s.ListEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScalarEntity1Item>().HasOne(s => s.DerivedEntity1).WithMany(d => d!.Scs).HasForeignKey(s => s.DerivedEntity1Id).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DependentOptional1_1>().HasOne(i => i.Outer).WithOne(o => o.Inner1).HasForeignKey<DependentOptional1_1>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<DependentOptional1_2>().HasOne(i => i.Outer).WithOne(o => o.Inner2).HasForeignKey<DependentOptional1_2>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<DependentRequired1_1>().HasOne(i => i.Outer).WithOne(o => o.Inner1).HasForeignKey<DependentRequired1_1>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<DependentRequired1_2>().HasOne(i => i.Outer).WithOne(o => o.Inner2).HasForeignKey<DependentRequired1_2>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<RecursiveEntity1>().HasOne(r => r.Parent).WithOne(r => r.Child).HasForeignKey<RecursiveEntity1>(r => r.ParentId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ScalarItem1>().HasOne(s => s.List1).WithMany(l => l.Items).HasForeignKey(s => s.List1Id).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScalarItem1>().HasOne(s => s.List2).WithMany(l => l.Items).HasForeignKey(s => s.List2Id).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ToDatabaseEntity1>().ToTable(nameof(ToDatabaseEntity1));
        modelBuilder.Entity<DependentPropertyPrincipal1>().ToTable(nameof(DependentPropertyPrincipal1));
        modelBuilder.Entity<DependentPropertyDependent1>().ToTable(nameof(DependentPropertyDependent1));
        modelBuilder.Entity<DependentPropertyPrincipal1>().HasOne(p => p.OptionalDependent).WithOne().HasForeignKey<DependentPropertyDependent1>(o => o.PrincipalId).IsRequired(false);
        modelBuilder.Entity<DependentPropertyPrincipal1>().HasMany(p => p.DependentList).WithOne().HasForeignKey(d => d.PrincipalId).IsRequired(false);
        modelBuilder.Entity<UnmatchedPrincipal1>().ToTable(nameof(UnmatchedPrincipal1));
        modelBuilder.Entity<UnmatchedDependent1>().ToTable(nameof(UnmatchedDependent1));
        modelBuilder.Entity<UnmatchedPrincipal1>().HasMany(p => p.DependentList).WithOne().HasForeignKey(d => d.PrincipalId).IsRequired(false);
        modelBuilder.Entity<ManyToManyParent2>().HasMany(p => p.Children).WithMany(c => c.Parents);

        // for sqlite
        var concurrencyTokenProperties = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(byte[])
                && p.ValueGenerated == ValueGenerated.OnAddOrUpdate
                && p.IsConcurrencyToken);

        foreach (var property in concurrencyTokenProperties)
        {
            // sqlite doesn't provide IsRowVersion feature, so this is done by overriding SaveChanges and SaveChangesAsync.
            // ValueGenerated.OnAddOrUpdate will block changes done to this property in the methods listed above
            property.ValueGenerated = ValueGenerated.Never;
            property.SetValueConverter(new SqliteConcurrencyTokenConverter());
            property.SetValueComparer(new ValueComparer<byte[]>(
                (a1, a2) => (a1 == null || a2 == null) ? false : a1.SequenceEqual(a2),
                a => a.Aggregate(0, (v, b) => HashCode.Combine(v, b.GetHashCode())),
                a => a));
        }
    }

    // sqlite doesn't support row version feature, have to do this
    private void UpdateConcurrencyTokens()
    {
        var toBeUpdatedEntities = ChangeTracker.Entries().Where(e => _states.Contains(e.State) && e.Metadata.ClrType.GetProperty(nameof(EntityBase.ConcurrencyToken)) != null);
        if (toBeUpdatedEntities.Any())
        {
            var bytes = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            foreach (var entity in toBeUpdatedEntities)
            {
                var property = entity.Property(nameof(EntityBase.ConcurrencyToken));
                property.CurrentValue = Encoding.UTF8.GetBytes(bytes);
            }
        }
    }

    private class SqliteConcurrencyTokenConverter : ValueConverter<byte[], string>
    {
        public SqliteConcurrencyTokenConverter()
            : base(
                v => ToDb(v),
                v => FromDb(v))
        {
        }

        private static byte[] FromDb(string v) => v.Select(c => (byte)c).ToArray();

        private static string ToDb(byte[] v) => new (v.Select(b => (char)b).ToArray());
    }
}
