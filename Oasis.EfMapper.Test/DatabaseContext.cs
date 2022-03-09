namespace Oasis.EfMapper.Test;

using Microsoft.EntityFrameworkCore;
using System;

internal class DatabaseContext : DbContext
{
    public static readonly byte[] EmptyTimeStamp = Array.Empty<byte>();
    public DatabaseContext(DbContextOptions options) : base(options)
    {
    }

    protected DatabaseContext()
    {
    }
}
