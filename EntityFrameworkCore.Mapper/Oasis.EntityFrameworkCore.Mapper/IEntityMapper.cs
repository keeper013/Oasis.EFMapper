namespace Oasis.EntityFrameworkCore.Mapper;

using Microsoft.EntityFrameworkCore;

public interface IEntityMapper
{
    void Map<TSource, TTarget>(TSource source, TTarget target, DbContext dbContext)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase;

    Task Map<TSource, TTarget>(TSource source, Func<IQueryable<TTarget>, IQueryable<TTarget>> includer, DbContext dbContext)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new();
}
