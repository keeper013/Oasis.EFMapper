namespace Oasis.EntityFramework.Mapper.Test.SqlSurrogate;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFramework.Mapper.Test.KeyPropertyType;
using Oasis.EntityFramework.Mapper.Test.OneToMany;
using Oasis.EntityFramework.Mapper.Test.OneToOne;
using Oasis.EntityFramework.Mapper.Test.Scalar;

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
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.ListIEntity).WithMany(l => l.Scs).HasForeignKey(s => s.ListIEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.ListEntity).WithMany(l => l.Scs).HasForeignKey(s => s.ListEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubScalarEntity1>().HasOne(s => s.CollectionEntity).WithMany(c => c.Scs).HasForeignKey(s => s.CollectionEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SubEntity2>().HasOne(s => s.ListEntity).WithMany(l => l.SubEntities).HasForeignKey(s => s.ListEntityId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Inner1_1>().Property(i => i.OuterId).HasColumnName("Outer_Id");
        modelBuilder.Entity<Inner1_2>().Property(i => i.OuterId).HasColumnName("Outer_Id");
        modelBuilder.Entity<RecursiveEntity1>().Property(r => r.ParentId).HasColumnName("Parent_Id");
        modelBuilder.Entity<Inner1_1>().HasOne(i => i.Outer).WithOne(o => o.Inner1).HasForeignKey<Inner1_1>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Inner1_2>().HasOne(i => i.Outer).WithOne(o => o.Inner2).HasForeignKey<Inner1_2>(i => i.OuterId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<RecursiveEntity1>().HasOne(r => r.Parent).WithOne(r => r.Child).HasForeignKey<RecursiveEntity1>(r => r.ParentId).OnDelete(DeleteBehavior.SetNull);
    }
}
