﻿namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using System;

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
        // TODO: refine this part
        modelBuilder.Entity<ScalarClass1>().ToTable("ScalarClass1");
    }
}