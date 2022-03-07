namespace Oasis.EfMapper.Test;

using Microsoft.EntityFrameworkCore;

internal class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions options) : base(options)
    {
    }

    protected DatabaseContext()
    {
    }
}
