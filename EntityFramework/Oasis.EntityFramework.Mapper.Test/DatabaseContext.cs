namespace Oasis.EntityFramework.Mapper.Test;

using Oasis.EntityFramework.Mapper.Test.KeepUnmatched;
using Oasis.EntityFramework.Mapper.Test.KeyPropertyType;
using Oasis.EntityFramework.Mapper.Test.ManyToMany;
using Oasis.EntityFramework.Mapper.Test.OneToMany;
using Oasis.EntityFramework.Mapper.Test.OneToOne;
using Oasis.EntityFramework.Mapper.Test.Scalar;
using Oasis.EntityFramework.Mapper.Test.ToDatabase;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        modelBuilder.Entity<ManyToManyParent2>().ToTable(nameof(ManyToManyParent2));
        modelBuilder.Entity<ManyToManyChild2>().ToTable(nameof(ManyToManyChild2));
        modelBuilder.Entity<ScalarItem1>().ToTable(nameof(ScalarItem1));
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
        modelBuilder.Entity<PrincipalOptional1>().HasOptional(o => o.Inner1).WithOptionalPrincipal(i => i!.Outer!).WillCascadeOnDelete(true);
        modelBuilder.Entity<PrincipalOptional1>().HasOptional(o => o.Inner2).WithOptionalPrincipal(i => i!.Outer!).WillCascadeOnDelete(true);
        modelBuilder.Entity<PrincipalRequired1>().HasRequired(o => o.Inner1).WithRequiredPrincipal(i => i.Outer).WillCascadeOnDelete(true);
        modelBuilder.Entity<PrincipalRequired1>().HasRequired(o => o.Inner2).WithRequiredPrincipal(i => i.Outer).WillCascadeOnDelete(true);
        modelBuilder.Entity<RecursiveEntity1>().HasOptional(r => r.Child).WithOptionalPrincipal(r => r!.Parent!).WillCascadeOnDelete(false);
        modelBuilder.Entity<ScalarItem1>().HasRequired(s => s.List1).WithMany(l => l!.Items).HasForeignKey(s => s.List1Id).WillCascadeOnDelete(true);
        modelBuilder.Entity<ScalarItem1>().HasRequired(s => s.List2).WithMany(l => l!.Items).HasForeignKey(s => s.List2Id).WillCascadeOnDelete(true);
        modelBuilder.Entity<ToDatabaseEntity1>().ToTable(nameof(ToDatabaseEntity1));
        modelBuilder.Entity<UnmatchedPrincipal1>().ToTable(nameof(UnmatchedPrincipal1));
        modelBuilder.Entity<UnmatchedDependent1>().ToTable(nameof(UnmatchedDependent1));
        modelBuilder.Entity<UnmatchedPrincipal1>().HasMany(p => p.DependentList).WithOptional().HasForeignKey(d => d.PrincipalId);
        modelBuilder.Entity<ManyToManyParent2>().HasMany(p => p.Children).WithMany(c => c.Parents).Map(m =>
        {
            m.ToTable("ManyToManyChild2ManyToManyParent2");
            m.MapLeftKey("ParentsId");
            m.MapRightKey("ChildrenId");
        });

        modelBuilder.Properties().Where(p => p.PropertyType == typeof(byte[])).Configure(p => p.IsConcurrencyToken().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None));
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
            if (entry.Entity is IEntityWithConcurrencyToken v1 && v1 != null)
            {
                v1.ConcurrencyToken = UTF8Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString());
            }
            else if (entry.Entity is ReversedEntityBase v2 && v2 != null)
            {
                v2.Id = UTF8Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString());
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
