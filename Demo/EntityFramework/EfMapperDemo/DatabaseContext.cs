namespace EfMapperDemo;

using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SQLite;
using System.Data.SQLite.EF6;

internal class DatabaseContext : DbContext
{
    private static readonly EntityState[] _states = new[] { EntityState.Added, EntityState.Modified };

    public DatabaseContext(DbConnection connection)
        : base(connection, false)
    {
        DbConfiguration.SetConfiguration(new SQLiteConfiguration());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().ToTable(nameof(Employee));
        modelBuilder.Entity<Employee>().HasKey(e => e.Name);

        modelBuilder.Entity<Project>().ToTable(nameof(Project));
        modelBuilder.Entity<Project>().HasKey(p => p.Name);

        modelBuilder.Entity<Project>().HasMany(p => p.Employees).WithOptional().HasForeignKey(e => e.ProjectName).WillCascadeOnDelete(false);
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