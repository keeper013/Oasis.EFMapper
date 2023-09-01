namespace SqlSurrogate;

using Microsoft.EntityFrameworkCore;
using EfMapperDemo;

internal class DatabaseContext : DbContext
{
    private static readonly EntityState[] _states = new[] { EntityState.Added, EntityState.Modified };

    public DatabaseContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().ToTable(nameof(Employee));
        modelBuilder.Entity<Employee>().HasKey(e => e.Name);

        modelBuilder.Entity<Project>().ToTable(nameof(Project));
        modelBuilder.Entity<Project>().HasKey(p => p.Name);

        modelBuilder.Entity<Project>().HasMany(p => p.Employees).WithOne().HasForeignKey(e => e.ProjectName).OnDelete(DeleteBehavior.SetNull);
    }
}