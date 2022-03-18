﻿namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oasis.EntityFrameworkCore.Mapper.Test.OneToMany;
using Oasis.EntityFrameworkCore.Mapper.Test.OneToOne;
using Oasis.EntityFrameworkCore.Mapper.Test.Scalar;
using System;
using System.Linq;

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
        modelBuilder.Entity<ScalarEntity1>().ToTable(nameof(ScalarEntity1));
        modelBuilder.Entity<CollectionEntity1>().ToTable(nameof(CollectionEntity1));
        modelBuilder.Entity<DerivedEntity1>().ToTable(nameof(DerivedEntity1));
        modelBuilder.Entity<DerivedEntity1_1>().ToTable(nameof(DerivedEntity1_1));
        modelBuilder.Entity<SubScalarEntity1>().ToTable(nameof(SubScalarEntity1));
        modelBuilder.Entity<Inner1_1>().ToTable(nameof(Inner1_1));
        modelBuilder.Entity<Outer1>().ToTable(nameof(Outer1));
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.ListIEntity).WithMany(l => l.Scs).HasForeignKey(s => s.ListIEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.ListEntity).WithMany(l => l.Scs).HasForeignKey(s => s.ListEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.CollectionEntity).WithMany(c => c.Scs).HasForeignKey(s => s.CollectionEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Outer1>().HasOne(o => o.Inner1).WithOne(i => i.Outer).HasForeignKey<Inner1_1>(i => i.OuterId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Inner1_1>().HasOne(i => i.Outer).WithOne(o => o.Inner1).HasForeignKey<Outer1>(o => o.Inner1Id).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Outer1>().HasOne(o => o.Inner2).WithOne(i => i.Outer).HasForeignKey<Inner1_2>(i => i.OuterId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Inner1_2>().HasOne(i => i.Outer).WithOne(o => o.Inner2).HasForeignKey<Outer1>(o => o.Inner2Id).OnDelete(DeleteBehavior.SetNull);

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
                    (a1, a2) => (a1 == default || a2 == default) ? false : a1.SequenceEqual(a2),
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

        private static string ToDb(byte[] v) => new string(v.Select(b => (char)b).ToArray());
    }
}
