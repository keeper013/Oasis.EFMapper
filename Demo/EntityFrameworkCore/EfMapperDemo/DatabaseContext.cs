﻿namespace EfMapperDemo;

using Microsoft.EntityFrameworkCore;

internal class DatabaseContext : DbContext
{
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