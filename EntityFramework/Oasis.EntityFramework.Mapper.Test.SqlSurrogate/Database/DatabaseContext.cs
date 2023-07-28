namespace Oasis.EntityFramework.Mapper.Test.SqlSurrogate;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFramework.Mapper.Test.KeepEntityOnMappingRemoved;
using Oasis.EntityFramework.Mapper.Test.KeepUnmatched;
using Oasis.EntityFramework.Mapper.Test.KeyPropertyType;
using Oasis.EntityFramework.Mapper.Test.OneToMany;
using Oasis.EntityFramework.Mapper.Test.OneToOne;
using Oasis.EntityFramework.Mapper.Test.Scalar;
using Oasis.EntityFramework.Mapper.Test.ToDatabase;

public class DatabaseContext : DbContext
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
        modelBuilder.Entity<DerivedEntity1_1>().ToTable(nameof(DerivedEntity1_1));
        modelBuilder.Entity<SubScalarEntity1>().ToTable(nameof(SubScalarEntity1));
        modelBuilder.Entity<ListEntity2>().ToTable(nameof(ListEntity2));
        modelBuilder.Entity<SubEntity2>().ToTable(nameof(SubEntity2));
        modelBuilder.Entity<DependentOptional1_1>().ToTable(nameof(DependentOptional1_1));
        modelBuilder.Entity<DependentOptional1_2>().ToTable(nameof(DependentOptional1_2));
        modelBuilder.Entity<PrincipalOptional1>().ToTable(nameof(PrincipalOptional1));
        modelBuilder.Entity<SessionTestingList1_1>().ToTable(nameof(SessionTestingList1_1));
        modelBuilder.Entity<SessionTestingList1_2>().ToTable(nameof(SessionTestingList1_2));
        modelBuilder.Entity<ScalarItem1>().ToTable(nameof(ScalarItem1));
        modelBuilder.Entity<ByteSourceEntity>().ToTable(nameof(ByteSourceEntity));
        modelBuilder.Entity<NByteSourceEntity>().ToTable(nameof(NByteSourceEntity));
        modelBuilder.Entity<ShortSourceEntity>().ToTable(nameof(ShortSourceEntity));
        modelBuilder.Entity<NShortSourceEntity>().ToTable(nameof(NShortSourceEntity));
        modelBuilder.Entity<IntSourceEntity>().ToTable(nameof(IntSourceEntity));
        modelBuilder.Entity<NIntSourceEntity>().ToTable(nameof(NIntSourceEntity));
        modelBuilder.Entity<LongSourceEntity>().ToTable(nameof(LongSourceEntity));
        modelBuilder.Entity<NLongSourceEntity>().ToTable(nameof(NLongSourceEntity));
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.ListIEntity).WithMany(l => l.Scs).HasForeignKey(s => s.ListIEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.ListEntity).WithMany(l => l.Scs).HasForeignKey(s => s.ListEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.CollectionEntity).WithMany(c => c.Scs).HasForeignKey(s => s.CollectionEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubEntity2>().HasOne(s => s.ListEntity).WithMany(l => l.SubEntities).HasForeignKey(s => s.ListEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DependentOptional1_1>().Property(i => i.OuterId).HasColumnName("Outer_Id");
        modelBuilder.Entity<DependentOptional1_2>().Property(i => i.OuterId).HasColumnName("Outer_Id");
        modelBuilder.Entity<RecursiveEntity1>().Property(r => r.ParentId).HasColumnName("Parent_Id");
        modelBuilder.Entity<DependentOptional1_1>().HasOne(i => i.Outer).WithOne(o => o.Inner1).HasForeignKey<DependentOptional1_1>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<DependentOptional1_2>().HasOne(i => i.Outer).WithOne(o => o.Inner2).HasForeignKey<DependentOptional1_2>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<RecursiveEntity1>().HasOne(r => r.Parent).WithOne(r => r.Child).HasForeignKey<RecursiveEntity1>(r => r.ParentId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ScalarItem1>().HasOne(s => s.List1).WithMany(l => l.Items).HasForeignKey(s => s.List1Id).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ScalarItem1>().HasOne(s => s.List2).WithMany(l => l.Items).HasForeignKey(s => s.List2Id).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ToDatabaseEntity1>().ToTable(nameof(ToDatabaseEntity1));
        modelBuilder.Entity<MappingRemovedPrincipal1>().ToTable(nameof(MappingRemovedPrincipal1));
        modelBuilder.Entity<MappingRemovedDependant1>().ToTable(nameof(MappingRemovedDependant1));
        modelBuilder.Entity<MappingRemovedDependant1>().Property(m => m.PrincipalIdForEntity).HasColumnName("PrincipalForEntity_Id");
        modelBuilder.Entity<MappingRemovedPrincipal1>().HasOne(p => p.OptionalDependant).WithOne().HasForeignKey<MappingRemovedDependant1>(o => o.PrincipalIdForEntity).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        modelBuilder.Entity<MappingRemovedPrincipal1>().HasMany(p => p.DependantList).WithOne().HasForeignKey(d => d.PrincipalIdForList).IsRequired(false);
        modelBuilder.Entity<UnmatchedPrincipal1>().ToTable(nameof(UnmatchedPrincipal1));
        modelBuilder.Entity<UnmatchedDependant1>().ToTable(nameof(UnmatchedDependant1));
        modelBuilder.Entity<UnmatchedPrincipal1>().HasMany(p => p.DependantList).WithOne().HasForeignKey(d => d.PrincipalId).IsRequired(false);
    }
}
