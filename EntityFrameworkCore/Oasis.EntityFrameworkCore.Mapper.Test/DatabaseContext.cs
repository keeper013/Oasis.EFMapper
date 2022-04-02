namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oasis.EntityFrameworkCore.Mapper.Test.KeyPropertyType;
using Oasis.EntityFrameworkCore.Mapper.Test.OneToMany;
using Oasis.EntityFrameworkCore.Mapper.Test.OneToOne;
using Oasis.EntityFrameworkCore.Mapper.Test.Scalar;
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
        modelBuilder.Entity<ScalarEntity1>().ToTable(nameof(ScalarEntity1));
        modelBuilder.Entity<CollectionEntity1>().ToTable(nameof(CollectionEntity1));
        modelBuilder.Entity<DerivedEntity1>().ToTable(nameof(DerivedEntity1));
        modelBuilder.Entity<ScalarEntity1Item>().ToTable(nameof(ScalarEntity1Item));
        modelBuilder.Entity<DerivedEntity1_1>().ToTable(nameof(DerivedEntity1_1));
        modelBuilder.Entity<SubScalarEntity1>().ToTable(nameof(SubScalarEntity1));
        modelBuilder.Entity<ListEntity2>().ToTable(nameof(ListEntity2));
        modelBuilder.Entity<SubEntity2>().ToTable(nameof(SubEntity2));
        modelBuilder.Entity<Inner1_1>().ToTable(nameof(Inner1_1));
        modelBuilder.Entity<Outer1>().ToTable(nameof(Outer1));
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
        modelBuilder.Entity<Inner1_1>().HasOne(i => i.Outer).WithOne(o => o.Inner1).HasForeignKey<Inner1_1>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Inner1_2>().HasOne(i => i.Outer).WithOne(o => o.Inner2).HasForeignKey<Inner1_2>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<RecursiveEntity1>().HasOne(r => r.Parent).WithOne(r => r.Child).HasForeignKey<RecursiveEntity1>(r => r.ParentId).OnDelete(DeleteBehavior.SetNull);

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
