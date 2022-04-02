namespace Oasis.EntityFramework.Mapper.Test;

using Oasis.EntityFramework.Mapper.Test.KeyPropertyType;
using Oasis.EntityFramework.Mapper.Test.OneToMany;
using Oasis.EntityFramework.Mapper.Test.OneToOne;
using Oasis.EntityFramework.Mapper.Test.Scalar;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Linq;
using System.Reflection;

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
        modelBuilder.Entity<ScalarEntity1Item>().ToTable(nameof(ScalarEntity1Item));
        modelBuilder.Entity<DerivedEntity1>().ToTable(nameof(DerivedEntity1));
        modelBuilder.Entity<DerivedEntity1_1>().ToTable(nameof(DerivedEntity1_1));
        modelBuilder.Entity<SubScalarEntity1>().ToTable(nameof(SubScalarEntity1));
        modelBuilder.Entity<ListEntity2>().ToTable(nameof(ListEntity2));
        modelBuilder.Entity<SubEntity2>().ToTable(nameof(SubEntity2));
        modelBuilder.Entity<Inner1_1>().ToTable(nameof(Inner1_1));
        modelBuilder.Entity<Outer1>().ToTable(nameof(Outer1));
        modelBuilder.Entity<ByteSourceEntity>().ToTable(nameof(ByteSourceEntity));
        modelBuilder.Entity<NByteSourceEntity>().ToTable(nameof(NByteSourceEntity));
        modelBuilder.Entity<ShortSourceEntity>().ToTable(nameof(ShortSourceEntity));
        modelBuilder.Entity<NShortSourceEntity>().ToTable(nameof(NShortSourceEntity));
        modelBuilder.Entity<IntSourceEntity>().ToTable(nameof(IntSourceEntity));
        modelBuilder.Entity<NIntSourceEntity>().ToTable(nameof(NIntSourceEntity));
        modelBuilder.Entity<LongSourceEntity>().ToTable(nameof(LongSourceEntity));
        modelBuilder.Entity<NLongSourceEntity>().ToTable(nameof(NLongSourceEntity));
        modelBuilder.Entity<RecursiveEntity1>().ToTable(nameof(RecursiveEntity1));
        modelBuilder.Entity<SubScalarEntity1>().HasOptional(s => s.ListIEntity).WithMany(l => l!.Scs).HasForeignKey(s => s.ListIEntityId).WillCascadeOnDelete(false);
        modelBuilder.Entity<SubScalarEntity1>().HasOptional(s => s.ListEntity).WithMany(l => l!.Scs).HasForeignKey(s => s.ListEntityId).WillCascadeOnDelete(false);
        modelBuilder.Entity<SubScalarEntity1>().HasOptional(s => s.CollectionEntity).WithMany(c => c!.Scs).HasForeignKey(s => s.CollectionEntityId).WillCascadeOnDelete(false);
        modelBuilder.Entity<SubEntity2>().HasOptional(s => s.ListEntity).WithMany(l => l!.SubEntities).HasForeignKey(s => s.ListEntityId).WillCascadeOnDelete(false);
        modelBuilder.Entity<ScalarEntity1Item>().HasRequired(s => s.DerivedEntity1).WithMany(d => d!.Scs).HasForeignKey(s => s.DerivedEntity1Id).WillCascadeOnDelete(false);
        modelBuilder.Entity<Outer1>().HasOptional(o => o.Inner1).WithOptionalPrincipal(i => i!.Outer!).WillCascadeOnDelete(true);
        modelBuilder.Entity<Outer1>().HasOptional(o => o.Inner2).WithOptionalPrincipal(i => i!.Outer!).WillCascadeOnDelete(true);
        modelBuilder.Entity<RecursiveEntity1>().HasOptional(r => r.Child).WithOptionalPrincipal(r => r!.Parent!).WillCascadeOnDelete(false);
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
