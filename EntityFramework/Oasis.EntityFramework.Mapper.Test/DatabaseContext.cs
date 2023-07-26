namespace Oasis.EntityFramework.Mapper.Test;

using Oasis.EntityFramework.Mapper.Test.KeepEntityOnMappingRemoved;
using Oasis.EntityFramework.Mapper.Test.KeepUnmatched;
using Oasis.EntityFramework.Mapper.Test.KeyPropertyType;
using Oasis.EntityFramework.Mapper.Test.OneToMany;
using Oasis.EntityFramework.Mapper.Test.OneToOne;
using Oasis.EntityFramework.Mapper.Test.Scalar;
using Oasis.EntityFramework.Mapper.Test.ToDatabase;
using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Linq;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbConnection connection)
        : base(connection, false)
    {
        DbConfiguration.SetConfiguration(new SQLiteConfiguration());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
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
        modelBuilder.Entity<SubScalarEntity1>().HasOptional(s => s.ListIEntity).WithMany(l => l!.Scs).HasForeignKey(s => s.ListIEntityId).WillCascadeOnDelete(false);
        modelBuilder.Entity<SubScalarEntity1>().HasOptional(s => s.ListEntity).WithMany(l => l!.Scs).HasForeignKey(s => s.ListEntityId).WillCascadeOnDelete(false);
        modelBuilder.Entity<SubScalarEntity1>().HasOptional(s => s.CollectionEntity).WithMany(c => c!.Scs).HasForeignKey(s => s.CollectionEntityId).WillCascadeOnDelete(false);
        modelBuilder.Entity<SubEntity2>().HasOptional(s => s.ListEntity).WithMany(l => l!.SubEntities).HasForeignKey(s => s.ListEntityId).WillCascadeOnDelete(false);
        modelBuilder.Entity<ScalarEntity1Item>().HasRequired(s => s.DerivedEntity1).WithMany(d => d!.Scs).HasForeignKey(s => s.DerivedEntity1Id).WillCascadeOnDelete(false);
        modelBuilder.Entity<PrincipalOptional1>().HasOptional(o => o.Inner1).WithOptionalPrincipal(i => i!.Outer!).WillCascadeOnDelete(true);
        modelBuilder.Entity<PrincipalOptional1>().HasOptional(o => o.Inner2).WithOptionalPrincipal(i => i!.Outer!).WillCascadeOnDelete(true);
        modelBuilder.Entity<PrincipalRequired1>().HasRequired(o => o.Inner1).WithRequiredPrincipal(i => i.Outer).WillCascadeOnDelete(true);
        modelBuilder.Entity<PrincipalRequired1>().HasRequired(o => o.Inner2).WithRequiredPrincipal(i => i.Outer).WillCascadeOnDelete(true);
        modelBuilder.Entity<RecursiveEntity1>().HasOptional(r => r.Child).WithOptionalPrincipal(r => r!.Parent!).WillCascadeOnDelete(false);
        modelBuilder.Entity<ScalarItem1>().HasRequired(s => s.List1).WithMany(l => l!.Items).HasForeignKey(s => s.List1Id).WillCascadeOnDelete(true);
        modelBuilder.Entity<ScalarItem1>().HasRequired(s => s.List2).WithMany(l => l!.Items).HasForeignKey(s => s.List2Id).WillCascadeOnDelete(true);
        modelBuilder.Entity<ToDatabaseEntity1>().ToTable(nameof(ToDatabaseEntity1));
        modelBuilder.Entity<MappingRemovedPrincipal1>().ToTable(nameof(MappingRemovedPrincipal1));
        modelBuilder.Entity<MappingRemovedDependant1>().ToTable(nameof(MappingRemovedDependant1));
        modelBuilder.Entity<MappingRemovedPrincipal1>().HasOptional(p => p.OptionalDependant).WithOptionalPrincipal();
        modelBuilder.Entity<MappingRemovedPrincipal1>().HasMany(p => p.DependantList).WithOptional().HasForeignKey(d => d.PrincipalId);
        modelBuilder.Entity<UnmatchedPrincipal1>().ToTable(nameof(UnmatchedPrincipal1));
        modelBuilder.Entity<UnmatchedDependant1>().ToTable(nameof(UnmatchedDependant1));
        modelBuilder.Entity<UnmatchedPrincipal1>().HasMany(p => p.DependantList).WithOptional().HasForeignKey(d => d.PrincipalId);
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
